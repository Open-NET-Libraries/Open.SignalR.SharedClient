namespace Open.SignalR.SharedClient;

public static partial class ScopedHubConnectionExtensions
{
	/// <inheritdoc cref="HubConnectionExtensions.StreamAsChannelCoreAsync{TResult}(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, object?[] args, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, args, cancellationToken);
	}

	/// <inheritdoc cref="HubConnectionExtensions.StreamAsChannelCoreAsync{TResult}(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, params object?[] args)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, args);
	}

	/// <inheritdoc cref="HubConnectionExtensions.StreamAsChannelCoreAsync{TResult}(HubConnection, string, object?[], CancellationToken)"/>/>
	public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, CancellationToken cancellationToken, params object?[] args)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, args, cancellationToken);
	}

	/// <inheritdoc cref="HubConnectionExtensions.StreamAsChannelCoreAsync{TResult}(HubConnection, string, object?[], CancellationToken)"/>/>
	public static async Task<ChannelReader<TResult>> StreamAsChannelCoreAsync<TResult>(this IHubConnectionActions hubConnection, string methodName, object?[] args, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);

		var inputChannel = await hubConnection.StreamAsChannelCoreAsync(methodName, typeof(TResult), args, cancellationToken).ConfigureAwait(false);
		var outputChannel = Channel.CreateUnbounded<TResult>();

		// Intentionally avoid passing the CancellationToken to RunChannel. The token is only meant to cancel the intial setup, not the enumeration.
		_ = RunChannel(inputChannel, outputChannel);

		return outputChannel.Reader;
	}

	// Function to provide a way to run async code as fire-and-forget
	// The output channel is how we signal completion to the caller.
	private static async Task RunChannel<TResult>(ChannelReader<object?> inputChannel, Channel<TResult> outputChannel)
	{
		try
		{
			while (await inputChannel.WaitToReadAsync().ConfigureAwait(false))
			{
				while (inputChannel.TryRead(out object? item))
				{
					while (!outputChannel.Writer.TryWrite((TResult)item!))
					{
						if (!await outputChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
						{
							// Failed to write to the output channel because it was closed. Nothing really we can do but abort here.
							return;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			outputChannel.Writer.TryComplete(ex);
		}
		finally
		{
			// This will safely no-op if the catch block above ran.
			outputChannel.Writer.TryComplete();

			// Needed to avoid UnobservedTaskExceptions
			_ = inputChannel.Completion.Exception;
		}
	}
}
