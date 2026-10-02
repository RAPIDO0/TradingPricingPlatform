using System.Collections.Concurrent;
using TradingPricing.Core.Market;

namespace TradingPricing.MarketData;

public sealed class OrderRouter
{
    private readonly ConcurrentDictionary<Symbol, OrderProcessor> _processors;
    private readonly ConcurrentDictionary<Symbol, Task> _processingTasks;

    public OrderRouter()
    {
        _processors = new ConcurrentDictionary<Symbol, OrderProcessor>();
        _processingTasks = new ConcurrentDictionary<Symbol, Task>();
    }

    public ValueTask SubmitAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        OrderProcessor processor = _processors.GetOrAdd(
            order.Symbol,
            CreateProcessor
        );

        return processor.SubmitAsync(
            order,
            cancellationToken
        );
    }

    private OrderProcessor CreateProcessor(Symbol symbol)
    {
        var orderBook = new OrderBook(symbol);

        var processor = new OrderProcessor(orderBook);

        Task processingTask = processor.RunAsync();

        _processingTasks.TryAdd(
            symbol,
            processingTask
        );

        return processor;
    }
}