namespace Open.SignalR.SharedClient;

/// <summary>
/// Provides methods for interacting with a hub.
/// </summary>
/// <remarks>Require an active connection to the hub.</remarks>
public interface IHubConnectionActions
{
	/// <inheritdoc cref="HubConnection.SendCoreAsync(string, object?[], CancellationToken)"/>
	Task SendCoreAsync(
		string methodName, object?[] args,
		CancellationToken cancellationToken = default);

	/// <inheritdoc cref="HubConnection.InvokeCoreAsync(string, Type, object?[], CancellationToken)"/>
	Task<object?> InvokeCoreAsync(
		string methodName, Type returnType, object?[] args,
		CancellationToken cancellationToken = default);

	/// <inheritdoc cref="HubConnection.StreamAsChannelCoreAsync(string, Type, object?[], CancellationToken)"/>
	Task<ChannelReader<object?>> StreamAsChannelCoreAsync(
		string methodName, Type returnType, object?[] args,
		CancellationToken cancellationToken = default);

	/// <inheritdoc cref="HubConnection.StreamAsyncCore{TResult}(string, object?[], CancellationToken)"/>
	IAsyncEnumerable<TResult> StreamAsyncCore<TResult>(
		string methodName, object?[] args,
		CancellationToken cancellationToken = default);
}
