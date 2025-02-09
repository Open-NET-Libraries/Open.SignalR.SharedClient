using System.Threading.Tasks;

namespace Open.SignalR.SharedClient;

/// <inheritdoc />
internal class HubConnectionAdapter : IHubConnectionAdapter
{
	public HubConnectionAdapter([StringSyntax(StringSyntaxAttribute.Uri)] string hub)
	{
		var conn = _hubConnection = new HubConnectionBuilder()
			.WithUrl(hub)
			.WithAutomaticReconnect()
			.Build();

		conn.Closed += OnClosed;
		conn.Reconnecting += OnReconnecting;
		conn.Reconnected += OnReconnected;
	}
	private readonly Lock _sync = new();

	private Task? _startAsync;
	private Task? _reconnectingAsync;

	private HubConnection? _hubConnection;
	public HubConnection Connection
		=> _hubConnection ?? throw new ObjectDisposedException(nameof(HubConnectionAdapter));

	private int _scopeCount;
	private Task? _scopeCheck;

	/// <inheritdoc />
	public void StartScope()
	{
		lock(_sync)
		{
			_scopeCount++;
			_scopeCheck = null;
		}
	}

	/// <inheritdoc />
	public void EndScope()
	{
		lock (_sync)
		{
			if (--_scopeCount != 0)
				return;

			Task newScopeCheck = _scopeCheck = Task.Delay(5000);
			newScopeCheck.ContinueWith(OnScopeZero, TaskContinuationOptions.ExecuteSynchronously);
		}
	}

	private void OnScopeZero(Task scopeCheck)
	{
		if (_scopeCheck != scopeCheck || _scopeCount != 0)
			return;

		lock (_sync)
		{
			if (_scopeCount != 0)
				return;

			_scopeCheck = null;
			_startAsync = null;
			_reconnectingAsync = null;

			_hubConnection?.StopAsync();
		}
	}

	/// <inheritdoc />
	public event Func<Task>? Connected;

	/// <inheritdoc />
	public Task? AddConnectedListener(Func<Task> listener)
	{
		Task? startTask;
		lock (_sync)
		{
			Connected += listener;
			startTask = _startAsync;
		}

		startTask?.ContinueWith(
			_ => listener(),
			TaskContinuationOptions.OnlyOnRanToCompletion
			| TaskContinuationOptions.ExecuteSynchronously);

		return startTask;
	}

	public ValueTask DisposeAsync()
	{
		var connection = _hubConnection;
		if (connection is null) return default;
		connection = Interlocked.CompareExchange(ref _hubConnection, null, connection);
		if(connection is null) return default;

		lock(_sync)
		{
			Connected = null;
			_scopeCheck = null;
			_startAsync = null;
			var reconnectingAsync = _reconnectingAsync;
			_reconnectingAsync = null;
			reconnectingAsync?.Start();
		}

		return connection.DisposeAsync();
	}

	private Task OnClosed(Exception? _)
	{
		// Disposed?
		if (_hubConnection is null)
			return Task.CompletedTask;

		lock (_sync)
			_startAsync = null;

		return Task.CompletedTask;
	}

	private Task OnReconnecting(Exception? _)
	{
		// Disposed?
		if (_hubConnection is null)
			return Task.CompletedTask;

		lock (_sync)
		{
			// It's possible that OnReconnected could have been called we arrive here.
			if (_startAsync is not null)
				return Task.CompletedTask;

			// If we got here, we have not yet set the reconnecting task.
			// Create an unstarted task that if picked up will eventually start when finally connected.
			_startAsync = _reconnectingAsync = new Task(static () => { });
		}

		return Task.CompletedTask;
	}

	private Task OnReconnected(string? _)
	{
		// Disposed?
		if (_hubConnection is null)
			return Task.CompletedTask;

		Task? reconnectingAsync;
		lock (_sync)
		{
			reconnectingAsync = _reconnectingAsync;
			_reconnectingAsync = null;
			_startAsync = Task.CompletedTask;
		}

		reconnectingAsync?.Start();
		return Connected?.Invoke()
			?? Task.CompletedTask;
	}

	public Task EnsureStarted(CancellationToken cancellationToken)
	{
		// Will throw if disposed.
		var connection = Connection;

		var startAsync = _startAsync;
		if (startAsync is not null)
			return startAsync;

		lock (_sync)
		{
			startAsync = _startAsync;
			if (startAsync is not null)
				return startAsync;

			_startAsync = startAsync = Connection.State switch
			{
				HubConnectionState.Connected => Task.CompletedTask,
				HubConnectionState.Disconnected => connection.StartAsync(cancellationToken),

				// The following should almost never occur, but will help recover from attempting a start while reconnecting.
				_ => Task.Run(async () =>
				{
					var state = HubConnectionState.Disconnected;

					for (int i = 0; i < 50; i++)
					{
						// Maybe TaskCanceledException?
						ObjectDisposedException.ThrowIf(_hubConnection is null, nameof(HubConnectionTracker));

						state = connection.State;
						if (state is HubConnectionState.Connected)
							return;

						cancellationToken.ThrowIfCancellationRequested();

						if (state is HubConnectionState.Disconnected)
						{
							await Connection.StartAsync(cancellationToken);
							return;
						}

						await Task.Delay(200, cancellationToken);
					}

					lock (connection)
					{
						if (_startAsync == startAsync)
							_startAsync = null;
					}

					throw new TimeoutException(
						$"Could not complete the connection in a timely manner: Latest state is {state}");
				}, cancellationToken),
			};
		}

		startAsync.ContinueWith(
			_ => Connected?.Invoke() ?? Task.CompletedTask,
			TaskContinuationOptions.OnlyOnRanToCompletion
		  | TaskContinuationOptions.ExecuteSynchronously);

		// If any errors or cancellations occur, clear the start task.
		startAsync.ContinueWith(t =>
		{
			if (_startAsync != t) return;
			lock (connection)
			{
				if (_startAsync != t) return;
				_startAsync = null;
			}
		}, TaskContinuationOptions.NotOnRanToCompletion
		 | TaskContinuationOptions.ExecuteSynchronously);

		return startAsync;
	}

	#region RPC Methods
	/// <inheritdoc />
	public Task SendCoreAsync(
		string methodName, object?[] args,
		CancellationToken cancellationToken = default)
		// No cancellation managment needed here. Fire and forget.
		=> EnsureStarted(cancellationToken)
			.ContinueWith(_ => Connection.SendCoreAsync(methodName, args, cancellationToken),
				TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously)
			.Unwrap();

	/// <inheritdoc />
	public Task<object?> InvokeCoreAsync(
		string methodName, Type returnType, object?[] args,
		CancellationToken cancellationToken = default)
		=> EnsureStarted(cancellationToken)
			.ContinueWith(_ => Connection.InvokeCoreAsync(methodName, returnType, args, cancellationToken),
				TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously)
			.Unwrap();
	#endregion

	#region Potentially Long-Running Cancellable Methods
	/// <inheritdoc />
	public Task<ChannelReader<object?>> StreamAsChannelCoreAsync(
		string methodName, Type returnType, object?[] args,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
		ArgumentNullException.ThrowIfNull(returnType);
		ArgumentNullException.ThrowIfNull(args);
		Contract.EndContractBlock();

		return EnsureStarted(cancellationToken)
			.ContinueWith(
				_ => Connection.StreamAsChannelCoreAsync(methodName, returnType, args, cancellationToken),
				cancellationToken,
				TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Current)
			.Unwrap();
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<TResult> StreamAsyncCore<TResult>(
		string methodName, object?[] args,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
		ArgumentNullException.ThrowIfNull(args);
		Contract.EndContractBlock();

		await EnsureStarted(cancellationToken)
			.ConfigureAwait(false);

		await foreach (var e in Connection
			.StreamAsyncCore<TResult>(methodName, args, cancellationToken)
			.ConfigureAwait(false))
		{
			yield return e;
		}
	}
	#endregion

	#region Subscribable Methods
	/// <inheritdoc />
	public IDisposable On(
		string methodName, Type[] parameterTypes,
		Func<object?[], object, Task<object?>> handler, object state)
		=> Connection.On(methodName, parameterTypes, handler, state);

	/// <inheritdoc />
	public IDisposable On(string methodName, Type[] parameterTypes, Func<object?[], object, Task> handler, object state)
		=> Connection.On(methodName, parameterTypes, handler, state);

	/// <inheritdoc />
	public void Remove(string methodName)
		=> Connection.Remove(methodName);
	#endregion
}
