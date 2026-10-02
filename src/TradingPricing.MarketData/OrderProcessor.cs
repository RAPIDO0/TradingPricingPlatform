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
                SingleReader = true,
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

    public async Task RunAsync(
    CancellationToken cancellationToken = default)
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