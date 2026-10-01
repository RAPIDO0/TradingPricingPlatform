using TradingPricing.Core.Market;

namespace TradingPricing.MarketData;

public sealed class Trade
{
    public long Id { get; }
    public Symbol Symbol { get; }
    public Price Price { get; }
    public Quantity Quantity { get; }

    public long BuyOrderId { get; }
    public long SellOrderId { get; }

    public DateTimeOffset Timestamp { get; }

    public Trade(
        long id,
        Symbol symbol,
        Price price,
        Quantity quantity,
        long buyOrderId,
        long sellOrderId,
        DateTimeOffset timestamp)
    {
        Id = id;
        Symbol = symbol;
        Price = price;
        Quantity = quantity;
        BuyOrderId = buyOrderId;
        SellOrderId = sellOrderId;
        Timestamp = timestamp;
    }
}