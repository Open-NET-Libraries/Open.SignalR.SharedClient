namespace WebTest.Hubs;

// Note: Hubs accept RPC calls from clients.
// IHubConext<THub> is how you can send messages to clients.
// CountService is using IHubContext<CounterHub> to queue and send messages to clients.

public class CounterHub(CountService counter) : Hub
{
	public ValueTask<int> GetLatest()
		=> new(counter.Count);

	public ValueTask<int> Increment()
		=> counter.Increment();

	public ValueTask<int> Decrement()
		=> counter.Decrement();
}
