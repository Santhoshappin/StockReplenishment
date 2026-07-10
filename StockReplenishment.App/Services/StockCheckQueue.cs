using System.Threading.Channels;

namespace StockReplenishment.App.Services;

public interface IStockCheckQueue
{
    ValueTask EnqueueAsync(Guid requestId);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}

public class StockCheckQueue : IStockCheckQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid requestId) => _channel.Writer.WriteAsync(requestId);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
