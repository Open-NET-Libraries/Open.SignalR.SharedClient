namespace Open.SignalR.SharedClient;

/// <summary>
/// Provides <see cref="IScopedHubConnection"/> instances for hub paths.
/// </summary>
public interface IScopedHubConnectionProvider
{
	/// <summary>
	/// Gets a <see cref="IScopedHubConnection"/> instance for the specified hub path.
	/// </summary>
	IScopedHubConnection GetConnectionFor(string hub);
}
