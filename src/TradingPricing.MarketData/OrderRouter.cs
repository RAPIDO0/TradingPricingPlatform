using System.Collections.Concurrent;
using TradingPricing.Core.Market;

namespace TradingPricing.MarketData;

public sealed class OrderRouter
{
    private readonly ConcurrentDictionary<Symbol, OrderBookContext> _contexts;

    public OrderRouter()
    {
        _contexts =
            new ConcurrentDictionary<Symbol, OrderBookContext>();
    }


    public ValueTask SubmitAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {

        OrderBookContext context = _contexts.GetOrAdd(order.Symbol, symbol => new OrderBookContext(symbol));
        return context.Processor.SubmitAsync(order, cancellationToken);
    }

    public bool TryGetContext(Symbol symbol, out OrderBookContext? context)
    {
        return _contexts.TryGetValue(
            symbol,
            out context
        );
    }

    public async Task CompleteAsync()
    {
        foreach (OrderBookContext context in _contexts.Values)
        {
            context.Complete();
        }

        await Task.WhenAll(
            _contexts.Values.Select(
                context => context.ProcessingTask
            )
        );
    }
}