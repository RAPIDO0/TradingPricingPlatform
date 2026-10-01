using TradingPricing.Core.Market;
using TradingPricing.MarketData;

public class Order
{
    public long Id { get; }
    public Symbol Symbol { get; }
    public Price Price { get; private set; }

    public Quantity OriginalQuantity { get; }
    public Quantity RemainingQuantity { get; private set; }

    public OrderSide Side { get; }
    public OrderStatus Status { get; private set; }

    public Order(
        long id,
        Symbol symbol,
        Price price,
        Quantity quantity,
        OrderSide side)
    {
        Id = id;
        Symbol = symbol;
        Price = price;

        OriginalQuantity = quantity;
        RemainingQuantity = quantity;

        Side = side;
        Status = OrderStatus.New;
    }

    public void ApplyFill(Quantity filledQuantity)
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException(
                "A cancelled order cannot be filled."
            );

        if (Status == OrderStatus.Filled)
            throw new InvalidOperationException(
                "A filled order cannot be filled again."
            );

        if (filledQuantity.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(filledQuantity));

        if (filledQuantity.Value > RemainingQuantity.Value)
            throw new InvalidOperationException(
                "Fill quantity cannot exceed remaining quantity."
            );

        RemainingQuantity = new Quantity(
            RemainingQuantity.Value - filledQuantity.Value
        );

        Status = RemainingQuantity.Value == 0
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Filled)
            throw new InvalidOperationException(
                "A filled order cannot be cancelled."
            );

        Status = OrderStatus.Cancelled;
    }
}