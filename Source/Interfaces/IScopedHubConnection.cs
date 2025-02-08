namespace Open.SignalR.SharedClient;

/// <summary>
/// Represents all the <see cref="HubConnection"/> actions other than starting, stopping and disposal.
/// </summary>
public interface IScopedHubConnection : IHubConnectionSubscribe, IHubConnectionActions, IDisposable
{
	/// <summary>
	/// Invokes the <paramref name="handler"/> when the connection is available or re-established.
	/// </summary>
	/// <remarks>Ensures a connnection has made before invoking the action.</remarks>
	IDisposable OnConnected(Func<Task> handler);
}

public static partial class ScopedHubConnectionExtensions
{
	/// <inheritdoc cref="IScopedHubConnection.OnConnected(Func{Task})"/>
	public static IDisposable OnConnected(this IScopedHubConnection hubAdapter, Action handler)
		=> hubAdapter.OnConnected(() =>
		{
			handler();
			return Task.CompletedTask;
		});

	/// <inheritdoc cref="IScopedHubConnection.OnConnected(Func{Task})"/>
	public static IDisposable OnConnected(this IScopedHubConnection hubAdapter, Func<IScopedHubConnection, Task> action)
		=> hubAdapter.OnConnected(() => action(hubAdapter));

	/// <inheritdoc cref="IScopedHubConnection.OnConnected(Func{Task})"/>
	public static IDisposable OnConnected(this IScopedHubConnection hubAdapter, Action<IScopedHubConnection> action)
		=> hubAdapter.OnConnected(() => action(hubAdapter));

	/// <summary>
	/// Invokes the <paramref name="handler"/> when the connection is available.
	/// Only invokes the action once and then disposes the created listener.
	/// </summary>
	/// <inheritdoc cref="IScopedHubConnection.OnConnected(Func{Task})"/>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary", Justification = "Not needed.")]
	public static IDisposable OnceConnected(this IScopedHubConnection hubAdapter, Func<Task> handler)
	{
		Task? task = null;
		Lock sync = new();
		IDisposable? d = null;
		d = hubAdapter.OnConnected(() =>
		{
			Debug.Assert(d is not null);
			if (task is not null) return task;
			try
			{
				lock (sync)
				{
					if (task is not null) return task;
					task = handler();
				}

				return task;
			}
			finally
			{
				d.Dispose();
			}
		});

		return d;
	}

	/// <inheritdoc cref="OnceConnected(IScopedHubConnection, Func{Task})"/>
	public static IDisposable OnceConnected(this IScopedHubConnection hubAdapter, Action handler)
		=> hubAdapter.OnceConnected(() =>
		{
			handler();
			return Task.CompletedTask;
		});

	/// <inheritdoc cref="OnceConnected(IScopedHubConnection, Func{Task})"/>
	public static IDisposable OnceConnected(this IScopedHubConnection hubAdapter, Func<IScopedHubConnection, Task> action)
		=> hubAdapter.OnceConnected(() => action(hubAdapter));

	/// <inheritdoc cref="OnceConnected(IScopedHubConnection, Func{Task})"/>
	public static IDisposable OnceConnected(this IScopedHubConnection hubAdapter, Action<IScopedHubConnection> action)
		=> hubAdapter.OnceConnected(() => action(hubAdapter));
}
