namespace Open.SignalR.SharedClient.Tests;
public class ScopedHubConnectionProviderTests
{
	[Test]
	public async Task Basic()
	{
		var p = new ScopedHubConnectionProvider();
		const string url = "http://localhost:5000/hub";
		var hub1 = p.GetConnectionFor(url);
		var hub2 = p.GetConnectionFor(url);

		await Assert.That(hub1).IsNotEqualTo(hub2);
	}
}