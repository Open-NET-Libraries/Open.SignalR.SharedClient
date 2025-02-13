# Open.SignalR.SharedClient

[![NuGet](https://img.shields.io/nuget/v/Open.SignalR.SharedClient.svg)](https://www.nuget.org/packages/Open.SignalR.SharedClient/)

A scoped hub connection and provider for easily sharing SignalR connections.

## `IScopedHubConnection`

Provides all the same functionality as a `HubConnection`, 
but does not interfere with other `IScopedHubConnection` instances.

Disposing the scoped connection cleans up all the handlers
but leaves the connection intact for others to use.

## Usage

### DI Setup

#### Default

The following adds a default provider as a singleton to the service collection.   
Assumes the consumers will know which URL to ask for.

```csharp
services.AddScopedHubConnectionProvider();
```

Consumers use `IScopedHubConnectionProvider.GetConnectionFor(url)` to get a hub connection.


#### With Named Connections

Consumers use `INamedScopedHubConnectionFactory.GetGonnectionByName(hubName)` to get a hub connection.  
This is necessary to ensure `NavigationManager` or other scope isolated services/resources are not used in a singleton context.

> Note: Client/Server hybrid apps will need to replicate the DI setup on the server side.

##### With Simple URL Mapping
```csharp
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
}
```

##### Custom DI Config

```csharp
services
		.AddScopedHubConnectionProvider()
		.AddNamedScopedHubConnectionFactory(serviceProvider => hubName => hubName switch
		{
			// With Automatic Reconnnect
			"hub1" => (serviceProvider.ToAbsoluteUri("/hub/hub1"),
				uri => new HubConnectionBuilder().WithAutomaticReconnect().WithUrl(uri)
			),

			// With Stateful Reconnect
			"hub2" => (serviceProvider.ToAbsoluteUri("/hub/hub1"),
				uri => new HubConnectionBuilder().WithStatefulReconnect().WithUrl(uri)
			),

			_ => (null, null)
		});
```



### Example


```csharp
public class MyService : IDisposable
{
	private readonly IScopedHubConnection _hub;
	public MyService(INamedScopedHubConnectionFactory connectionProvider)
	{
		_hub = connectionProvider.GetConnectionByNam("hub1");

		_hub.On("MyMethod1", (string arg1, string arg2) =>
		{
			// Do something when the method is invoked on the hub.
		});

		_hub.On("MyMethod2", (string arg1, string arg2) =>
		{
			// Do something when the method is invoked on the hub.
		});

		// Automatically invokes OnConnectionEstablished when a connnection is available
		// and invokes it on every reconnection.
		_hub.OnConnected(OnConnectionEstablished);
	}

	public void Dispose() => _hub.Dispose();

	static void OnConnectionEstablished(IScopedHubConnection hub)
	{
		hub.SendAsync("SubTo", "MyMethod1");
		hub.SendAsync("SubTo", "MyMethod2");
	}

	public async Task DoSomething()
	{
		// Automatically guarantees a connection and invokes the method.
		await _hub.InvokeAsync("MyMethod", "arg1", "arg2");
	}
}
```