using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebTest.Client;

public static class AppServicesConfiguration
{
	class FakeNavigationManager : NavigationManager
	{
		public FakeNavigationManager()
			=> Initialize("http://localhost/", "http://localhost/");
	}

	/// <summary>
	/// Provided to keep the client and server DI in sync.
	/// </summary>
	public static IServiceCollection AddAppHubConnections(this IServiceCollection services)
		=> services.AddScopedHubConnectionProvider(serviceProvider => {

			// Currently can't find a better way to do this.
			// WASM works, but the same registration needs to exist on the server.
			NavigationManager nav;
			try
			{
				nav = serviceProvider.GetRequiredService<NavigationManager>();
			}
			catch (InvalidOperationException)
			{
				nav = new FakeNavigationManager();
			}

			return [
				// Hub 1 (just for demonstration, doesn't actually exist)
				KeyValuePair.Create("hub1",
					new HubConnectionBuilder()
					.WithUrl(nav.ToAbsoluteUri("/hub/hub1"))),

				// Counter
				KeyValuePair.Create("counter",
					new HubConnectionBuilder()
					.WithAutomaticReconnect()
					.WithUrl(nav.ToAbsoluteUri("/hub/counter")))
			];
		});
}
