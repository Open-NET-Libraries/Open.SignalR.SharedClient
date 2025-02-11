namespace Open.SignalR.SharedClient;

/// <summary>
/// Provides <see cref="IScopedHubConnection"/> instances for hub paths.
/// </summary>
public interface IScopedHubConnectionProvider
{
	/// <summary>
	/// Gets a <see cref="IScopedHubConnection"/> instance for the specified hub path.
	/// </summary>
	IScopedHubConnection GetConnectionFor(string hubUrl);

	/// <summary>
	/// Gets a <see cref="IScopedHubConnection"/> instance by the configured hub name.
	/// </summary>
	/// <remarks>Allows for more complex hub configurations when setting up DI services.</remarks>
	IScopedHubConnection GetGonnectionByName(string configuredHubName);
}

public static partial class ScopedHubConnectionProviderExtensions
{
	/// <inheritdoc cref="IScopedHubConnectionProvider.GetConnectionFor(string)"/>
	public static IScopedHubConnection GetConnectionFor(this IScopedHubConnectionProvider provider, Uri hubUrl)
		=> provider.GetConnectionFor(hubUrl.ToString());
}
