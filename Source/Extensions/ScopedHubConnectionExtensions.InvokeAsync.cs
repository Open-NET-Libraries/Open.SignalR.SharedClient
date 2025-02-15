﻿
namespace Open.SignalR.SharedClient;

public static partial class ScopedHubConnectionExtensions
{
	/// <inheritdoc cref="HubConnectionExtensions.InvokeCoreAsync(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task InvokeAsync(this IHubConnectionActions hubConnection, string methodName, object?[] args, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.InvokeCoreAsync(methodName, typeof(object), args, cancellationToken);
	}

	/// <inheritdoc cref="HubConnectionExtensions.InvokeCoreAsync(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task InvokeAsync(this IHubConnectionActions hubConnection, string methodName, params object?[] args)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.InvokeCoreAsync(methodName, typeof(object), args, default);
	}

	/// <inheritdoc cref="HubConnectionExtensions.InvokeCoreAsync(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task InvokeAsync(this IHubConnectionActions hubConnection, string methodName, CancellationToken cancellationToken, params object?[] args)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.InvokeCoreAsync(methodName, typeof(object), args, cancellationToken);
	}

	/// <inheritdoc cref="HubConnectionExtensions.InvokeCoreAsync(HubConnection, string, object?[], CancellationToken)"/>/>
	public static async Task<TResult> InvokeAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, object?[] args, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return (TResult)(await hubConnection.InvokeCoreAsync(methodName, typeof(TResult), args, cancellationToken).ConfigureAwait(false))!;
	}

	/// <inheritdoc cref="HubConnectionExtensions.InvokeCoreAsync(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task<TResult> InvokeAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, params object?[] args)
		=> hubConnection.InvokeAsync<TResult>(methodName, args, default);

	/// <inheritdoc cref="HubConnectionExtensions.InvokeCoreAsync(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task<TResult> InvokeAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, CancellationToken cancellationToken, params object?[] args)
		=> hubConnection.InvokeAsync<TResult>(methodName, args, cancellationToken);
}
