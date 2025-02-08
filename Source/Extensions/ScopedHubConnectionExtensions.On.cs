namespace Open.SignalR.SharedClient;

public static partial class ScopedHubConnectionExtensions
{
	private static IDisposable On(this IHubConnectionSubscribe hubConnection, string methodName, Type[] parameterTypes, Action<object?[]> handler)
	{
		Debug.Assert(hubConnection is not null);

		return hubConnection.On(methodName, parameterTypes, static (parameters, state) =>
		{
			var currentHandler = (Action<object?[]>)state;
			currentHandler(parameters);
			return Task.CompletedTask;
		}, handler);
	}

	/// <inheritdoc cref="HubConnectionExtensions.On(HubConnection, string, Action)"/>
	public static IDisposable On(this IHubConnectionSubscribe hubConnection, string methodName, Action handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName, Type.EmptyTypes, _ => handler());
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1}(HubConnection, string, Action{T1})"/>
	public static IDisposable On<T1>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[ typeof(T1) ],
			args => handler((T1)args[0]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2}(HubConnection, string, Action{T1, T2})"/>
	public static IDisposable On<T1, T2>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[ typeof(T1), typeof(T2) ],
			args => handler((T1)args[0]!, (T2)args[1]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3}(HubConnection, string, Action{T1, T2, T3})"/>
	public static IDisposable On<T1, T2, T3>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2, T3> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[ typeof(T1), typeof(T2), typeof(T3) ],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4}(HubConnection, string, Action{T1, T2, T3, T4})"/>
	public static IDisposable On<T1, T2, T3, T4>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2, T3, T4> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[ typeof(T1), typeof(T2), typeof(T3), typeof(T4) ],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5}(HubConnection, string, Action{T1, T2, T3, T4, T5})"/>
	public static IDisposable On<T1, T2, T3, T4, T5>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2, T3, T4, T5> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5, T6}(HubConnection, string, Action{T1, T2, T3, T4, T5, T6})"/>
	public static IDisposable On<T1, T2, T3, T4, T5, T6>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5, T6, T7}(HubConnection, string, Action{T1, T2, T3, T4, T5, T6, T7})"/>
	public static IDisposable On<T1, T2, T3, T4, T5, T6, T7>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5, T6, T7, T8}(HubConnection, string, Action{T1, T2, T3, T4, T5, T6, T7, T8})"/>
	public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8>(this IHubConnectionSubscribe hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!, (T8)args[7]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{TResult}(HubConnection, string, Type[], Func{object?[], Task{TResult}})" />
	public static IDisposable On(this IHubConnectionSubscribe hubConnection, string methodName, Type[] parameterTypes, Func<object?[], Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName, parameterTypes, (parameters, state) =>
		{
			var currentHandler = (Func<object?[], Task>)state;
			return currentHandler(parameters);
		}, handler);
	}

	/// <inheritdoc cref="HubConnectionExtensions.On(HubConnection, string, Func{Task})"/>
	public static IDisposable On(this IHubConnectionSubscribe hubConnection, string methodName, Func<Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName, Type.EmptyTypes, _ => handler());
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, TResult}(HubConnection, string, Func{T1, TResult})"/>
	public static IDisposable On<T1>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1)],
			args => handler((T1)args[0]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2}(HubConnection, string, Func{T1, T2, Task})"/>
	public static IDisposable On<T1, T2>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2)],
			args => handler((T1)args[0]!, (T2)args[1]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3}(HubConnection, string, Func{T1, T2, T3, Task})"/>
	public static IDisposable On<T1, T2, T3>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, T3, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4}(HubConnection, string, Func{T1, T2, T3, T4, Task})"/>
	public static IDisposable On<T1, T2, T3, T4>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, T3, T4, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5}(HubConnection, string, Func{T1, T2, T3, T4, T5, Task})"/>
	public static IDisposable On<T1, T2, T3, T4, T5>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, T3, T4, T5, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5, T6}(HubConnection, string, Func{T1, T2, T3, T4, T5, T6, Task})"/>
	public static IDisposable On<T1, T2, T3, T4, T5, T6>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5, T6, T7}(HubConnection, string, Func{T1, T2, T3, T4, T5, T6, T7, Task})"/>
	public static IDisposable On<T1, T2, T3, T4, T5, T6, T7>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!));
	}

	/// <inheritdoc cref="HubConnectionExtensions.On{T1, T2, T3, T4, T5, T6, T7, T8}(HubConnection, string, Func{T1, T2, T3, T4, T5, T6, T7, T8, Task})"/>
	public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8>(this IHubConnectionSubscribe hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> handler)
	{
		ArgumentNullException.ThrowIfNull(hubConnection);
		return hubConnection.On(methodName,
			[typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)],
			args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!, (T8)args[7]!));
	}
}
