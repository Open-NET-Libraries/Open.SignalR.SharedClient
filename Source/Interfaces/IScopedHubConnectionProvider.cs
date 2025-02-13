namespace Open.SignalR.SharedClient;

/// <summary>
/// Provides <see cref="IScopedHubConnection"/> instances for hub paths.
/// </summary>
public interface IScopedHubConnectionProvider
{
	/// <summary>
	/// Gets a <see cref="IScopedHubConnection"/> instance for the specified hub path.
	/// </summary>
	IScopedHubConnection GetConnectionFor([StringSyntax(StringSyntaxAttribute.Uri)] string hubUrl);

	/// <inheritdoc cref="GetConnectionFor(string)"/>
	IScopedHubConnection GetConnectionFor(Uri hubUri);

	/// <inheritdoc cref="GetConnectionFor(string)"/>
	internal IScopedHubConnection GetConnectionFor(UriOrUrlString hubUri, Func<Uri, IHubConnectionBuilder> factory);
}

/// <summary>
/// Provides <see cref="IScopedHubConnection"/> instances for named hubs.
/// </summary>
public interface INamedScopedHubConnectionFactory
{
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
