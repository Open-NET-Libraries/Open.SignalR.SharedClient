using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebTest.Client;

public static class AppServicesConfiguration
{
	/// <summary>
	/// Provided to keep the client and server DI in sync.
	/// </summary>
	public static IServiceCollection AddAppHubConnections(this IServiceCollection services)
		=> services
		.AddScopedHubConnectionProvider()
		.AddNamedScopedHubConnectionMapping(serviceProvider => hubName => hubName switch
		{
			"hub1" => serviceProvider.ToAbsoluteUri("/hub/hub1"),
			"counter" => serviceProvider.ToAbsoluteUri("/hub/counter"),
			_ => null, // Returning null will signal that the name was not found and throw appropritately.
		});

	/// <summary>
	/// Shortcut for <see cref="NavigationManager"/>.
	/// </summary>
	public static Uri ToAbsoluteUri(this IServiceProvider sp, string path)
		=> sp.GetRequiredService<NavigationManager>().ToAbsoluteUri(path);

	public static IServiceCollection AddCustomAppHubConnectionsExample(this IServiceCollection services)
		=> services
		.AddScopedHubConnectionProvider()
		.AddNamedScopedHubConnectionFactory(serviceProvider => hubName => hubName switch
		{
			"hub1" => (serviceProvider.ToAbsoluteUri("/hub/hub1"),
				uri => new HubConnectionBuilder().WithAutomaticReconnect().WithUrl(uri)
			),

			"counter" => (serviceProvider.ToAbsoluteUri("/hub/counter"),
				uri => new HubConnectionBuilder().WithStatefulReconnect().WithUrl(uri)
			),

			_ => (null, null)
		});
}

