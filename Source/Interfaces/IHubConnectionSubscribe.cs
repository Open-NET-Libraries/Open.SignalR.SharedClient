namespace Open.SignalR.SharedClient;

/// <summary>
/// Allows for subscribing to a hub's invokations.
/// </summary>
public interface IHubConnectionSubscribe
{
	/// <inheritdoc cref="HubConnection.On(string, Type[], Func{object?[], object, Task{object?}}, object)"/>
	IDisposable On(
		string methodName, Type[] parameterTypes,
		Func<object?[], object, Task<object?>> handler, object state);

	/// <inheritdoc cref="HubConnection.On(string, Type[], Func{object?[], object, Task}, object)"/>
	IDisposable On(
		string methodName, Type[] parameterTypes,
		Func<object?[], object, Task> handler, object state);

	/// <inheritdoc cref="HubConnection.Remove(string)"/>
	void Remove(string methodName);
}
