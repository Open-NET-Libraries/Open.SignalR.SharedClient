# Open.SignalR.SharedClient

A HubConnection adapter and provider for easily sharing SignalR connections.

## Interfaces

### `IScopedHubConnection`

Provides all the same functionality as a `HubConnection`, 
but does not interfere with other `IScopedHubConnection` instances.

Disposing the scoped connection cleans up all the handlers
but leaves the connection intact for others to use.

### `IScopedHubConnectionProvider`

## Usage

#### DI Setup

```csharp
services.AddSingleton<IScopedHubConnectionProvider, ScopedHubConnectionProvider>();
```

#### Example


```csharp
public class MyService : IDisposable
{
	private readonly IScopedHubConnection _hub;
	public MyService(IScopedHubConnectionProvider connectionProvider)
	{
		_hub = connectionProvider.GetConnectionFor("http://localhost:5000/hub");

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