namespace Open.SignalR.SharedClient;

/// <summary>
/// Initializes a new instance of <see cref="HubConnectionTracker"/>.
/// </summary>
internal sealed class HubConnectionTracker(IHubConnectionAdapter hubConnection) : IDisposable
{
	#region HubConnection & Events Management
	private IHubConnectionAdapter? _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
	/// <summary>
	/// The underlying <see cref="HubConnection"/>.
	/// </summary>
	private IHubConnectionAdapter Connection => _hubConnection
		?? throw new ObjectDisposedException(nameof(HubConnectionTracker));

	private readonly ConcurrentDictionary<Func<Task>, Func<Task>> _connectedEventHandlers = new();

	/// <summary>
	/// Occurs when the connection is established or re-established.
	/// </summary>
	private event Func<Task> Connected
	{
		add
		{
			var connection = _hubConnection;
			ObjectDisposedException.ThrowIf(connection is null, nameof(HubConnectionTracker));
			ArgumentNullException.ThrowIfNull(value, nameof(value));

			Task LocalHandler()
			{
				// This ensures the event is fired only once after adding.
				if (!_connectedEventHandlers.TryRemove(value, out var handler))
					return Task.CompletedTask;

				connection.Connected -= handler;
				connection = _hubConnection;
				if (connection is null)
					return Task.CompletedTask;

				if (!_connectedEventHandlers.TryAdd(value, value))
					return Task.CompletedTask;

				connection.Connected += value;
				return value();
			}

			// Double adding? Return.
			if (!_connectedEventHandlers.TryAdd(value, LocalHandler))
				return;

			// Handles calling the handler immediately if already connected.
			connection.Connected += LocalHandler;
		}

		remove
		{
			ArgumentNullException.ThrowIfNull(value, nameof(value));
			var connection = _hubConnection;
			if(_connectedEventHandlers.TryRemove(value, out var handler) && connection is not null)
				connection.Connected -= handler;
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
		_ = Connection.EnsureStarted(CancellationToken.None);
		return sub;
	}

	/// <summary>
	/// Detaches from the <see cref="HubConnection"/> events.
	/// </summary>
	public void Dispose()
	{
		var connection = _hubConnection;
		if (connection is null) return;

		connection = Interlocked.CompareExchange(ref _hubConnection, null, connection);
		if (connection is null) return;

		// Remove connected subscriptions.
		foreach (var handler in _connectedEventHandlers.Values)
			connection.Connected -= handler;
	}
}
