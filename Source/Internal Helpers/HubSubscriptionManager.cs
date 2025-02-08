namespace Open.SignalR.SharedClient;

/// <summary>
/// A helper class for managing subscriptions to a hub by method name.
/// </summary>
/// <remarks>Is not thread-safe.</remarks>
internal class HubSubscriptionManager : IDisposable
{
	private Dictionary<string, HashSet<Subscription>>? _subscriptions = [];

	private sealed class Subscription(Action onDispose, IDisposable sub) : IDisposable
	{
		private Action? _onDispose = onDispose;

		private readonly IDisposable _sub = sub;

		internal Action? DisposeCore()
		{
			var d = Interlocked.Exchange(ref _onDispose, null);

			// Step 2) Dispose the subscription.
			_sub.Dispose();

			return d;
		}

		public void Dispose()
			=> DisposeCore()?.Invoke();
	}

	/// <summary>
	/// Produces an <see cref="IDisposable"/> that will remove the subscription when disposed
	/// or will unsubscribe if this instance is disposed.
	/// </summary>
	public IDisposable Subscribe(string methodName, IDisposable sub)
	{
		var subs = _subscriptions;
		ObjectDisposedException.ThrowIf(subs is null, typeof(HubSubscriptionManager));

		HashSet<Subscription>? registry = null;
		Subscription? s = null;

		// Setup the subscription.
		s = new Subscription(() =>
		{
			// Setup the removal action.
			Debug.Assert(registry is not null);
			Debug.Assert(s is not null);

			registry.Remove(s);
			if (registry.Count == 0)
				subs.Remove(methodName);
		}, sub);

		// Add the subscription to the registry.
		if (subs.TryGetValue(methodName, out registry))
		{
			registry.Add(s);
		}
		else
		{
			subs[methodName] = registry = [s];
		}

		// Return the subscription.
		return s;
	}

	/// <summary>
	/// Removes all subscriptions related to the <paramref name="methodName"/>.
	/// </summary>
	public void Unsubscribe(string methodName)
	{
		var reg = _subscriptions;
		// Disposed?
		if (reg is null)
			return;

		if (reg.Remove(methodName, out var subs))
		{
			foreach (var s in subs)
			{
				s.Dispose();
			}
		}
	}

	/// <summary>
	/// Disposes of this instance and all subscriptions.
	/// </summary>
	public void Dispose()
	{
		var reg = _subscriptions;
		if (reg is null) return;
		reg = Interlocked.CompareExchange(ref _subscriptions, null, reg);
		if (reg is null) return;

		// Release the entire registry.
		foreach (var kv in reg)
		{
			foreach (var s in kv.Value)
			{
				s.DisposeCore();
			}

			kv.Value.Clear();
		}
	}
}
