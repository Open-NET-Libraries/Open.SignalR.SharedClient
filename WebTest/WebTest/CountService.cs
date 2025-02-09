namespace WebTest;

public class CountService
{
	private readonly Channel<int> _channel;

	private int _count;

	public CountService(IHubContext<CounterHub> hubContext)
	{
		_channel = Channel.CreateBounded<int>(new BoundedChannelOptions(100) { SingleReader = true });
		_ = _channel.Reader
			.TaskReadAllAsync(value => hubContext.Clients.All.SendAsync("OnNext", value))
			.AsTask();
	}

	public int Count => _count;

	private async ValueTask<int> Queue(int value, CancellationToken cancellationToken)
	{
		await _channel.Writer.WriteAsync(value, cancellationToken);
		return value;
	}

	public ValueTask<int> Increment(CancellationToken cancellationToken = default)
	{
		int value = Interlocked.Increment(ref _count);
		return Queue(value, cancellationToken);
	}

	public ValueTask<int> Decrement(CancellationToken cancellationToken = default)
	{
		int value = Interlocked.Decrement(ref _count);
		return Queue(value, cancellationToken);
	}
}
