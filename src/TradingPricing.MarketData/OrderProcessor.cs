using System.Threading.Channels;

namespace TradingPricing.MarketData;

public sealed class OrderProcessor
{
    private readonly Channel<Order> _channel;
    private readonly OrderBook _orderBook;

    public OrderProcessor(OrderBook orderBook)
    {
        _orderBook = orderBook;

        _channel = Channel.CreateUnbounded<Order>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }
        );
    }

    public ValueTask SubmitAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(
            order,
            cancellationToken
        );
    }

    public async Task RunWorkersAsync(
        int workerCount,
        CancellationToken cancellationToken = default)
    {
        var workers = new Task[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            workers[i] = ConsumeAsync(cancellationToken);
        }

        await Task.WhenAll(workers);
    }

    private async Task ConsumeAsync(
        CancellationToken cancellationToken)
    {
        await foreach (
            Order order in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            _orderBook.SubmitOrder(order);
        }
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }
}