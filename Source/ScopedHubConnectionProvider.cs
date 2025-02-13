namespace Open.SignalR.SharedClient;

/// <summary>
/// Manages persistent <see cref="HubConnection"/> instances
/// and produces <see cref="IScopedHubConnection"/> instances for hub paths.
/// </summary>
/// <param name="factory">The factory method to create new <see cref="IHubConnectionBuilder"/> instances from their URL.</param>
/// <remarks>
/// This class acts as the singleton repository for all shared <see cref="HubConnection"/> instances.
/// </remarks>
internal sealed class ScopedHubConnectionProvider(Func<Uri, IHubConnectionBuilder> factory)
	: IScopedHubConnectionProvider, IAsyncDisposable
{
	public static IHubConnectionBuilder GetDefaultConfigBuilder(Uri uri)
		=> new HubConnectionBuilder()
			.WithAutomaticReconnect()
			.WithUrl(uri);

	private readonly Func<Uri, IHubConnectionBuilder> _defaultfactory
		= factory ?? throw new ArgumentNullException(nameof(factory));

	private ConcurrentDictionary<string, HubConnectionAdapter>? _registry = new();

	/// <summary>
	/// Initializes a new instance of <see cref="ScopedHubConnectionProvider"/> with a default hub configuration.
	/// </summary>
	public ScopedHubConnectionProvider() : this(GetDefaultConfigBuilder) { }

	/// <summary>
	/// Disposes of this and all managed <see cref="HubConnection"/> instances.
	/// </summary>
	public ValueTask DisposeAsync()
	{
		var conn = _registry;
		if (conn is null) return default;
		conn = Interlocked.CompareExchange(ref _registry, null, conn);
		return conn is null ? default : DisposeAsync(conn.Values);
	}

	private static async ValueTask DisposeAsync(IEnumerable<IAsyncDisposable> asyncDisposables)
	{
		foreach (var c in asyncDisposables)
		{
			try
			{ await c.DisposeAsync(); }
			catch
			{ /* Ignore */ }
		}
	}

	public IScopedHubConnection GetConnectionFor(UriOrUrlString hubUri, Func<Uri, IHubConnectionBuilder> factory)
	{
		var reg = _registry;
		ObjectDisposedException.ThrowIf(reg is null, nameof(ScopedHubConnectionProvider));
		return new ScopedHubConnection(reg.GetOrAdd(hubUri, _ => new HubConnectionAdapter(factory(hubUri))));
	}

	/// <inheritdoc />
	public IScopedHubConnection GetConnectionFor([StringSyntax(StringSyntaxAttribute.Uri)] string hubUrl)
		=> GetConnectionFor(new(hubUrl), _defaultfactory);

	/// <inheritdoc />
	public IScopedHubConnection GetConnectionFor(Uri hubUri)
		=> GetConnectionFor(new(hubUri), _defaultfactory);
}

/// <summary>
/// Produces <see cref="IScopedHubConnection"/> instances for a given hub 'name'.
/// </summary>
/// <param name="provider">The configured provider that registers all the <see cref="IHubConnectionAdapter"/> instances.</param>
/// <param name="nameToFactory">The factory method to create new <see cref="IHubConnectionBuilder"/> instances from their 'name'.</param>
/// <remarks>
/// Initializes a new instance of <see cref="NamedScopedHubConnnectionFactory"/>.
/// Acts as a 'scoped' wrapper for <see cref="ScopedHubConnectionProvider"/> to avoid dependency injection restrictions.
/// </remarks>
internal sealed class NamedScopedHubConnnectionFactory(
	IScopedHubConnectionProvider provider,
	Func<string, (Uri? uri, Func<Uri, IHubConnectionBuilder>? factory)> nameToFactory)
	: INamedScopedHubConnectionFactory
{
	/// <inheritdoc />
	public IScopedHubConnection GetGonnectionByName(string configuredHubName)
	{
		var (uri, factory) = nameToFactory(configuredHubName);
		if (uri is null) throw new ArgumentException($"No URL for requested hub: {configuredHubName}");
		return provider.GetConnectionFor(new(uri), factory ?? ScopedHubConnectionProvider.GetDefaultConfigBuilder);
	}
}

/// <summary>
/// Extension methods for <see cref="IScopedHubConnectionProvider"/>.
/// </summary>
public static partial class ScopedHubConnectionProviderExtensions
{
	/// <summary>
	/// Adds an <see cref="IScopedHubConnectionProvider"/> singleton to the service collection.
	/// </summary>
	public static IServiceCollection AddScopedHubConnectionProvider(
		this IServiceCollection services)
		=> services.AddSingleton<IScopedHubConnectionProvider, ScopedHubConnectionProvider>();

	/// <summary>
	/// Adds an <see cref="IScopedHubConnectionProvider"/> singleton to the service collection
	/// with a custom <see cref="IHubConnectionBuilder"/> factory.
	/// </summary>
	public static IServiceCollection AddScopedHubConnectionProvider(
		this IServiceCollection services, Func<Uri, IHubConnectionBuilder> factory)
		=> services.AddSingleton<IScopedHubConnectionProvider>(_ => new ScopedHubConnectionProvider(factory));

	/// <summary>
	/// Adds (scoped) an <see cref="INamedScopedHubConnectionFactory"/> to the service collection
	/// with a custom <see cref="IHubConnectionBuilder"/> factory.
	/// </summary>
	public static IServiceCollection AddNamedScopedHubConnectionFactory(
		this IServiceCollection services, Func<IServiceProvider, Func<string, (Uri? url, Func<Uri, IHubConnectionBuilder>? factory)>> factory)
		=> services.AddScoped<INamedScopedHubConnectionFactory>(
			sp => new NamedScopedHubConnnectionFactory(
				sp.GetRequiredService<IScopedHubConnectionProvider>(),
				factory(sp)));

	/// <summary>
	/// Adds (scoped) an <see cref="INamedScopedHubConnectionFactory"/> to the service collection
	/// using a <paramref name="nameToUrlMapper"/> function.
	/// </summary>
	/// <remarks>
	/// This easily facilitates the common use case of using a "NavigationMangager" to configure URLs.
	/// </remarks>
	public static IServiceCollection AddNamedScopedHubConnectionMapping(
		this IServiceCollection services, Func<IServiceProvider, Func<string, Uri?>> nameToUrlMapper)
		=> services.AddNamedScopedHubConnectionFactory(
			sp => {
				var nameToUrl = nameToUrlMapper(sp);
				return (string name) => (nameToUrl(name), ScopedHubConnectionProvider.GetDefaultConfigBuilder);
			});
}
