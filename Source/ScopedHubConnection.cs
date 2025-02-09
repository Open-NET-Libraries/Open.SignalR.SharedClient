namespace Open.SignalR.SharedClient;

/// <inheritdoc />
internal sealed class ScopedHubConnection(IHubConnectionAdapter connection) : IScopedHubConnection
{
	private readonly HubConnectionTracker _tracker = new(connection);
	private readonly HubSubscriptionManager _subs = new();

	/// <summary>
	/// Cancels any streams and removes any subscriptions.
	/// </summary>
	public void Dispose()
	{
		#region Disposed Check
		var ctsInstances = _ctsInstances;
		if (ctsInstances is null) return;
		lock (_ctsSync)
		{
			ctsInstances = _ctsInstances;
			if (ctsInstances is null) return;
			_ctsInstances = null;
		}
		#endregion

		#region Cancellation
		// Cancel any running async methods.
		using var cts = _cts;
		foreach (var ctsi in ctsInstances)
		{
			// Cancel each manually first.
			using var c = ctsi;
			c.Cancel();
		}

		// Cancel the main token.
		_cts.Cancel();
		#endregion

		// Cleanup any remaining local subscriptions.
		_subs.Dispose();

		// Dispose the connection tracker.
		_tracker.Dispose();
	}

	#region RPC Methods
	/// <inheritdoc />
	public Task SendCoreAsync(
		string methodName, object?[] args,
		CancellationToken cancellationToken = default)
		// No cancellation managment needed here. Fire and forget.
		=> connection.SendCoreAsync(methodName, args, cancellationToken);

	/// <inheritdoc />
	public Task<object?> InvokeCoreAsync(
		string methodName, Type returnType, object?[] args,
		CancellationToken cancellationToken = default)
		=> connection.InvokeCoreAsync(methodName, returnType, args, cancellationToken);
	#endregion

	#region CancellationToken Management
	private readonly Lock _ctsSync = new();
	private HashSet<CancellationTokenSource>? _ctsInstances = [];
	private readonly CancellationTokenSource _cts = new();

	private CancellationTokenSource? AddCtsInstance(CancellationToken incommingToken)
	{
		var instances = _ctsInstances;
		ObjectDisposedException.ThrowIf(instances is null, this);
		if (!incommingToken.CanBeCanceled) return null;

		lock (_ctsSync)
		{
			instances = _ctsInstances;
			ObjectDisposedException.ThrowIf(instances is null, this);

			var cts = CancellationTokenSource.CreateLinkedTokenSource(incommingToken, _cts.Token);
			instances.Add(cts);
			return cts;
		}
	}

	private void RemoveCtsInstance(CancellationTokenSource? cts)
	{
		if (_ctsInstances is null || cts is null)
			return;

		lock (_ctsSync)
		{
			_ctsInstances?.Remove(cts);
		}
	}
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

		// If the token is cancellable, then use the local method.
		// Otherwise just use the underlying CanellationToken.
		return cancellationToken.CanBeCanceled
			? StreamAsChannelCoreAsync()
			: connection.StreamAsChannelCoreAsync(methodName, returnType, args, _cts.Token);

		async Task<ChannelReader<object?>> StreamAsChannelCoreAsync()
		{
			using var cts = AddCtsInstance(cancellationToken);
			Debug.Assert(cts is not null); // Because we checked above if it can be cancelled.

			ChannelReader<object?>? reader = null;
			try
			{
				reader = await connection
					.StreamAsChannelCoreAsync(methodName, returnType, args, cts.Token)
					.ConfigureAwait(false);
			}
			catch
			{
				RemoveCtsInstance(cts);
				throw;
			}

			// Await completion and remove the cts instance.
			_ = reader.Completion
				.ContinueWith(_ => RemoveCtsInstance(cts), CancellationToken.None);

			return reader;
		}
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<TResult> StreamAsyncCore<TResult>(
		string methodName, object?[] args,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
		ArgumentNullException.ThrowIfNull(args);
		Contract.EndContractBlock();

		using var cts = AddCtsInstance(cancellationToken);
		var token = cts?.Token ?? _cts.Token;
		try
		{
			await foreach (var e in connection
				.StreamAsyncCore<TResult>(methodName, args, token)
				.ConfigureAwait(false))
			{
				yield return e;
			}
		}
		finally
		{
			RemoveCtsInstance(cts);
		}
	}
	#endregion

	#region Subscribable Methods
	/// <inheritdoc />
	public IDisposable On(
		string methodName, Type[] parameterTypes,
		Func<object?[], object, Task<object?>> handler, object state)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(methodName);
		return _subs.Subscribe(methodName, connection.On(methodName, parameterTypes, handler, state));
	}

	/// <inheritdoc />
	public IDisposable On(string methodName, Type[] parameterTypes, Func<object?[], object, Task> handler, object state)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(methodName);
		return _subs.Subscribe(methodName, connection.On(methodName, parameterTypes, handler, state));
	}

	/// <inheritdoc />
	public void Remove(string methodName)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(methodName);
		_subs.Unsubscribe(methodName);
	}
	#endregion

	/// <inheritdoc />
	public IDisposable OnConnected(Func<Task> handler)
		=> _subs.Subscribe(string.Empty, _tracker.OnConnected(() => handler()));

	// ^^^ See IScopedHubConnection.cs for extension methods related to this.
}