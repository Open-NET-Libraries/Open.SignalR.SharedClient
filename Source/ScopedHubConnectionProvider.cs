using System.Collections.Frozen;

namespace Open.SignalR.SharedClient;

/// <summary>
/// Manages persistent <see cref="HubConnection"/> instances
/// and produces <see cref="IScopedHubConnection"/> instances for hub paths.
/// </summary>
public sealed class ScopedHubConnectionProvider : IScopedHubConnectionProvider, IAsyncDisposable
{
	private ConcurrentDictionary<string, HubConnectionAdapter>? _registry = new();
	private readonly FrozenDictionary<string, HubConnectionAdapter> _namedHubConnections;

	/// <summary>
	/// Initializes a new instance of <see cref="ScopedHubConnectionProvider"/>.
	/// </summary>
	internal ScopedHubConnectionProvider(FrozenDictionary<string, HubConnectionAdapter> namedHubConnections)
		=> _namedHubConnections = namedHubConnections ?? throw new ArgumentNullException(nameof(namedHubConnections));

	/// <inheritdoc cref="ScopedHubConnectionProvider(FrozenDictionary{string, HubConnectionAdapter})"/>
	public ScopedHubConnectionProvider()
		: this(FrozenDictionary<string, HubConnectionAdapter>.Empty)
	{ }

	/// <inheritdoc cref="ScopedHubConnectionProvider(FrozenDictionary{string, HubConnectionAdapter})"/>
	public ScopedHubConnectionProvider(IEnumerable<KeyValuePair<string, IHubConnectionBuilder>> namedHubConnections)
		: this(namedHubConnections.ToFrozenDictionary(kvp => kvp.Key, kvp => new HubConnectionAdapter(kvp.Value)))
	{ }

	/// <inheritdoc cref="ScopedHubConnectionProvider(FrozenDictionary{string, HubConnectionAdapter})"/>
	public ScopedHubConnectionProvider(string name, IHubConnectionBuilder builder)
		: this([KeyValuePair.Create(name, builder)])
	{ }

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

	/// <inheritdoc />
	public IScopedHubConnection GetConnectionFor([StringSyntax(StringSyntaxAttribute.Uri)] string hubUrl)
	{
		ArgumentNullException.ThrowIfNull(hubUrl);
		ArgumentException.ThrowIfNullOrWhiteSpace(hubUrl);
		var reg = _registry;
		ObjectDisposedException.ThrowIf(reg is null, nameof(ScopedHubConnectionProvider));
		return new ScopedHubConnection(reg.GetOrAdd(hubUrl, k => new HubConnectionAdapter(k)));
	}

	/// <inheritdoc />
	public IScopedHubConnection GetGonnectionByName(string configuredHubName)
		=> new ScopedHubConnection(_namedHubConnections[configuredHubName]);
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
	{
		services.AddSingleton<IScopedHubConnectionProvider, ScopedHubConnectionProvider>();
		return services;
	}

	/// <summary>
	/// Adds an <see cref="IScopedHubConnectionProvider"/> singleton to the service collection with the pre-configured hubs.
	/// </summary>
	public static IServiceCollection AddScopedHubConnectionProvider(
		this IServiceCollection services,
		IEnumerable<KeyValuePair<string, IHubConnectionBuilder>> namedHubs)
	{
		services.AddSingleton<IScopedHubConnectionProvider>(_ => new ScopedHubConnectionProvider(namedHubs));
		return services;
	}

	/// <inheritdoc cref="AddScopedHubConnectionProvider(IServiceCollection, IEnumerable{KeyValuePair{string, IHubConnectionBuilder}})"/>
	public static IServiceCollection AddScopedHubConnectionProvider(
		this IServiceCollection services,
		Func<IServiceProvider, IEnumerable<KeyValuePair<string, IHubConnectionBuilder>>> namedHubs)
	{
		services.AddSingleton<IScopedHubConnectionProvider>(sp => new ScopedHubConnectionProvider(namedHubs(sp)));
		return services;
	}

	/// <summary>
	/// Adds an <see cref="IScopedHubConnectionProvider"/> singleton to the service collection with the pre-configured hub.
	/// </summary>
	/// <remarks>Using this method the service container has only one named hub.</remarks>
	public static IServiceCollection AddScopedHubConnectionProvider(
		this IServiceCollection services, string name, Func<IServiceProvider, IHubConnectionBuilder> hub)
	{
		services.AddSingleton<IScopedHubConnectionProvider>(sp => new ScopedHubConnectionProvider(name, hub(sp)));
		return services;
	}
}
