namespace Open.SignalR.SharedClient.Tests;
public class HubSubscriptionManagerTests
{
	private sealed class TestDisposable(Action onDispose) : IDisposable
	{
		public void Dispose() => onDispose();
	}

	private static TestDisposable CreateDisposable(Action onDispose) => new(onDispose);

	[Test]
	public async Task Basic()
	{
		var subs = new HubSubscriptionManager();
		bool disposed = false;

		subs.Subscribe("test", CreateDisposable(() => disposed = true)).Dispose();
		await Assert.That(disposed).IsTrue();
		disposed = false;

		subs.Subscribe("test2", CreateDisposable(() => disposed = true));
		subs.Dispose();
		await Assert.That(disposed).IsTrue();
	}
}
