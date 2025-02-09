namespace Open.SignalR.SharedClient;

/// <summary>
/// Exposes a method to ensure that a hub connection is started.
/// </summary>
public interface IHubConnectionStarter
{
	/// <summary>
	/// Ensures that the hub connection is started.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
	Task EnsureStarted(CancellationToken cancellationToken = default);
}
