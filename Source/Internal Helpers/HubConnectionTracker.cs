using System.Collections.Concurrent;

namespace Open.SignalR.SharedClient;

internal sealed class HubConnectionTracker : IDisposable
{
	/// <summary>
	/// Initializes a new instance of <see cref="HubConnectionTracker"/>.
	/// </summary>
	public HubConnectionTracker(HubConnection hubConnection)
	{
		_hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));

		// Wire up events first...
		hubConnection.Closed += OnClosed;
		hubConnection.Reconnecting += OnReconnecting;
		hubConnection.Reconnected += OnReconnected;

		// Events could have fired and changed the state, so check if we're already connected.
		lock (hubConnection)
		{
			if (_startAsync is null
			&& hubConnection.State == HubConnectionState.Connected)
			{
				_startAsync = Task.CompletedTask;
			}
		}
	}

	#region HubConnection & Events Management
	private HubConnection? _hubConnection;

	Task? _startAsync;
	Task? _reconnectingAsync;

	private event Func<Task>? ConnectedCore;

	private readonly ConcurrentDictionary<Func<Task>, Func<Task>> _connectedEventHandlers = new();

	/// <summary>
	/// Occurs when the connection is established or re-established.
	/// </summary>
	public event Func<Task> Connected
	{
		add
		{
			ObjectDisposedException.ThrowIf(_hubConnection is null, nameof(HubConnectionTracker));
			ArgumentNullException.ThrowIfNull(value, nameof(value));

			Task LocalHandler()
			{
				// This ensures the event is fired only once after adding.
				if (!_connectedEventHandlers.TryRemove(value, out var handler))
					return Task.CompletedTask;

				ConnectedCore -= handler;
				if (_hubConnection is null)
					return Task.CompletedTask;

				ConnectedCore += value;
				return value();
			}

			// Double adding? Return.
			if (!_connectedEventHandlers.TryAdd(value, LocalHandler))
				return;

			ConnectedCore += LocalHandler;

			var startTask = _startAsync;
			if (startTask is null)
				return;

			// In flight, or completed, it doesn't matter.
			// The LocalHanlder will manage calling the value only once.
			startTask.ContinueWith(
				_ => LocalHandler(),
				TaskContinuationOptions.OnlyOnRanToCompletion
				| TaskContinuationOptions.ExecuteSynchronously);
		}

		remove
		{
			ArgumentNullException.ThrowIfNull(value, nameof(value));
			if (_connectedEventHandlers.TryRemove(value, out var handler))
				ConnectedCore -= handler;
			ConnectedCore -= value;
		}
	}
	#endregion

	private readonly struct Subscription(Action unsubscribe)
		: IDisposable
	{
		public void Dispose()
			=> unsubscribe();
	}

	public IDisposable OnConnected(Func<Task> handler)
	{
		Connected += handler;
		var sub = new Subscription(() => Connected -= handler);
		_ = EnsureStarted(CancellationToken.None);
		return sub;
	}

	/// <summary>
	/// The underlying <see cref="HubConnection"/>.
	/// </summary>
	public HubConnection Connection => _hubConnection
		?? throw new ObjectDisposedException(nameof(HubConnectionTracker));

	public static implicit operator HubConnection(HubConnectionTracker tracker)
		=> tracker.Connection;

	/// <summary>
	/// Detaches from the <see cref="HubConnection"/> events.
	/// </summary>
	public void Dispose()
	{
		var connection = _hubConnection;
		if (connection is null) return;

		connection = Interlocked.CompareExchange(ref _hubConnection, null, connection);
		if (connection is null) return;

		lock (connection) _startAsync = _reconnectingAsync = null;
		// Clear any listeners.
		ConnectedCore = null;

		connection.Closed -= OnClosed;
		connection.Reconnecting -= OnReconnecting;
		connection.Reconnected -= OnReconnected;
	}

	private Task OnClosed(Exception? _)
	{
		// Disposed?
		var connection = _hubConnection;
		if (connection is null)
			return Task.CompletedTask;

		lock (connection)
			_startAsync = null;

		return Task.CompletedTask;
	}

	private Task OnReconnecting(Exception? _)
	{
		// Disposed?
		var connection = _hubConnection;
		if (connection is null)
			return Task.CompletedTask;

		lock (connection)
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
		var connection = _hubConnection;
		if (connection is null)
			return Task.CompletedTask;

		Task? reconnectingAsync;
		lock (connection)
		{
			reconnectingAsync = _reconnectingAsync;
			_reconnectingAsync = null;
			_startAsync = Task.CompletedTask;
		}

		reconnectingAsync?.Start();
		return ConnectedCore is null
			? Task.CompletedTask
			: ConnectedCore.Invoke();
	}

	/// <summary>
	/// Ensures the connection is started.
	/// </summary>
	public Task EnsureStarted(CancellationToken cancellationToken)
	{
		// Will throw if disposed.
		var connection = Connection;

		var startAsync = _startAsync;
		if (startAsync is not null)
			return startAsync;

		lock (connection)
		{
			startAsync = _startAsync;
			if (startAsync is not null)
				return startAsync;

			_startAsync = startAsync = Connection.State switch
			{
				HubConnectionState.Connected => Task.CompletedTask,
				HubConnectionState.Disconnected => connection.StartAsync(cancellationToken),
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
			_ => ConnectedCore?.Invoke() ?? Task.CompletedTask,
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
}
