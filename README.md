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

Assumes the consumers will know which URL to ask for.

```csharp
services.AddScopedHubConnectionProvider();
```

Consumers use `.GetConnectionFor(url)` to get a hub connection.


#### With Named Connections

Allows for any hub configuration to be added and retrieved by name.

> Note: Client/Server hybrid apps will need to replicate the DI setup on the server side.

```csharp
services.AddScopedHubConnectionProvider(serviceProvider => {
	var nav = serviceProvider.GetRequiredService<NavigationManager>();
	return [
		// Hub 1
		KeyValuePair.Create("hub1",
			new HubConnectionBuilder()
			.WithAutomaticReconnect()
			.WithUrl(nav.ToAbsoluteUri("/hubs/hub1"))),

		// Hub 2
		KeyValuePair.Create("hub2",
			new HubConnectionBuilder()
			.WithUrl(nav.ToAbsoluteUri("/hubs/hub2")))
	];
});
```

Consumers use `.GetGonnectionByName(hubName)` to get a hub connection.

### Example


```csharp
public class MyService : IDisposable
{
	private readonly IScopedHubConnection _hub;
	public MyService(IScopedHubConnectionProvider connectionProvider)
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