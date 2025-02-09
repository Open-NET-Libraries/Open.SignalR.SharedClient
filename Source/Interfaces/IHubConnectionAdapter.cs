namespace Open.SignalR.SharedClient;

/// <summary>
/// A synchronized adapter for a SignalR hub connection.
/// </summary>
internal interface IHubConnectionAdapter
	: IHubConnectionStarter, IHubConnectionActions, IHubConnectionSubscribe, IAsyncDisposable
{
	/// <summary>
	/// Occurs when a connection is established (connected or reconnected).
	/// </summary>
	event Func<Task> Connected;

	/// <summary>
	/// Syncronizes adding the listener and returning the connection started task if one already exists.
	/// </summary>
	Task? AddConnectedListener(Func<Task> listener);
}
