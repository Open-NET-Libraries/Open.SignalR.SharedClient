namespace Open.SignalR.SharedClient;

/// <summary>
/// Manages persistent <see cref="HubConnection"/> instances
/// and produces <see cref="IScopedHubConnection"/> instances for hub paths.
/// </summary>
public sealed class ScopedHubConnectionProvider : IScopedHubConnectionProvider, IAsyncDisposable
{
	private ConcurrentDictionary<string, HubConnectionAdapter>? _registry = new();

	/// <summary>
	/// Disposes of this and all managed <see cref="HubConnection"/> instances.
	/// </summary>
	public ValueTask DisposeAsync()
	{
		var conn = _registry;
		if(conn is null) return default;
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
}

/// <summary>
/// Extension methods for <see cref="IScopedHubConnectionProvider"/>.
/// </summary>
public static partial class ScopedHubConnectionProviderExtensions
{
	/// <summary>
	/// Adds an <see cref="IScopedHubConnectionProvider"/> singleton to the service collection.
	/// </summary>
	public static IServiceCollection AddScopedHubConnectionProvider(this IServiceCollection services)
	{
		services.AddSingleton<IScopedHubConnectionProvider, ScopedHubConnectionProvider>();
		return services;
	}
}
