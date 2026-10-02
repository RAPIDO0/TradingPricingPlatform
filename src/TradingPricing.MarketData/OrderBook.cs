using TradingPricing.Core.Market;

namespace TradingPricing.MarketData;

public class OrderBook
{
    private readonly SortedDictionary<Price, LinkedList<Order>> _buyOrders;
    private readonly SortedDictionary<Price, LinkedList<Order>> _sellOrders;

    private readonly Dictionary<long, OrderLocation> _ordersById;
    private readonly HashSet<long> _knownOrderIds;

    private long _nextTradeId = 1;

    public Symbol Symbol { get; }

    public event Action<Trade>? TradeExecuted;
    public event Action<Order>? OrderCancelled;
    public event Action? OrderBookChanged;

    public OrderBook(Symbol symbol)
    {
        Symbol = symbol;

        _buyOrders = new SortedDictionary<Price, LinkedList<Order>>(
            Comparer<Price>.Create(
                (x, y) => y.Value.CompareTo(x.Value)
            )
        );

        _sellOrders = new SortedDictionary<Price, LinkedList<Order>>(
            Comparer<Price>.Create(
                (x, y) => x.Value.CompareTo(y.Value)
            )
        );

        _ordersById = new Dictionary<long, OrderLocation>();
        _knownOrderIds = new HashSet<long>();
    }

    public Order? BestBuy =>
        _buyOrders.Count == 0
            ? null
            : _buyOrders.First().Value.First?.Value;

    public Order? BestSell =>
        _sellOrders.Count == 0
            ? null
            : _sellOrders.First().Value.First?.Value;

    public IReadOnlyList<Trade> SubmitOrder(Order order)
    {
        ValidateOrder(order);

        _knownOrderIds.Add(order.Id);

        var trades = new List<Trade>();

        while (order.RemainingQuantity.Value > 0)
        {
            Order? restingOrder = GetBestOppositeOrder(order.Side);

            if (restingOrder is null)
                break;

            if (!PricesCross(order, restingOrder))
                break;

            long matchedQuantity = Math.Min(
                order.RemainingQuantity.Value,
                restingOrder.RemainingQuantity.Value
            );

            var quantity = new Quantity(matchedQuantity);

            order.ApplyFill(quantity);
            restingOrder.ApplyFill(quantity);

            Trade trade = CreateTrade(
                order,
                restingOrder,
                quantity
            );

            trades.Add(trade);

            TradeExecuted?.Invoke(trade);

            if (restingOrder.Status == OrderStatus.Filled)
            {
                RemoveFilledOrder(restingOrder);
            }
        }

        if (order.RemainingQuantity.Value > 0)
        {
            AddRestingOrder(order);
        }

        return trades;
    }

    public bool CancelOrder(long orderId)
    {
        if (!_ordersById.TryGetValue(orderId, out var location))
            return false;

        var side = location.Side == OrderSide.Buy
            ? _buyOrders
            : _sellOrders;

        if (!side.TryGetValue(location.Price, out var ordersAtPrice))
            return false;

        Order order = location.Node.Value;

        order.Cancel();

        ordersAtPrice.Remove(location.Node);
        _ordersById.Remove(orderId);

        if (ordersAtPrice.Count == 0)
        {
            side.Remove(location.Price);
        }

        OrderCancelled?.Invoke(order);
        OrderBookChanged?.Invoke();

        return true;
    }

    private void AddRestingOrder(Order order)
    {
        var side = order.Side == OrderSide.Buy
            ? _buyOrders
            : _sellOrders;

        if (!side.TryGetValue(order.Price, out var ordersAtPrice))
        {
            ordersAtPrice = new LinkedList<Order>();
            side[order.Price] = ordersAtPrice;
        }

        LinkedListNode<Order> node =
            ordersAtPrice.AddLast(order);

        _ordersById.Add(
            order.Id,
            new OrderLocation(
                order.Price,
                order.Side,
                node
            )
        );

        OrderBookChanged?.Invoke();
    }

    private void RemoveFilledOrder(Order order)
    {
        if (!_ordersById.TryGetValue(order.Id, out var location))
        {
            throw new InvalidOperationException(
                $"Order {order.Id} was not found in the order book."
            );
        }

        var side = location.Side == OrderSide.Buy
            ? _buyOrders
            : _sellOrders;

        LinkedList<Order> ordersAtPrice =
            side[location.Price];

        ordersAtPrice.Remove(location.Node);

        _ordersById.Remove(order.Id);

        if (ordersAtPrice.Count == 0)
        {
            side.Remove(location.Price);
        }

        OrderBookChanged?.Invoke();
    }

    private Order? GetBestOppositeOrder(OrderSide incomingSide)
    {
        return incomingSide == OrderSide.Buy
            ? BestSell
            : BestBuy;
    }

    private static bool PricesCross(
        Order incomingOrder,
        Order restingOrder)
    {
        return incomingOrder.Side == OrderSide.Buy
            ? incomingOrder.Price.Value >= restingOrder.Price.Value
            : incomingOrder.Price.Value <= restingOrder.Price.Value;
    }

    private Trade CreateTrade(
        Order incomingOrder,
        Order restingOrder,
        Quantity quantity)
    {
        long buyOrderId;
        long sellOrderId;

        if (incomingOrder.Side == OrderSide.Buy)
        {
            buyOrderId = incomingOrder.Id;
            sellOrderId = restingOrder.Id;
        }
        else
        {
            buyOrderId = restingOrder.Id;
            sellOrderId = incomingOrder.Id;
        }

        return new Trade(
            id: _nextTradeId++,
            symbol: Symbol,
            price: restingOrder.Price,
            quantity: quantity,
            buyOrderId: buyOrderId,
            sellOrderId: sellOrderId,
            timestamp: DateTimeOffset.UtcNow
        );
    }

    private void ValidateOrder(Order order)
    {
        if (order.Symbol != Symbol)
        {
            throw new InvalidOperationException(
                $"Order symbol {order.Symbol} does not match book symbol {Symbol}."
            );
        }

        if (_knownOrderIds.Contains(order.Id))
        {
            throw new InvalidOperationException(
                $"Order id {order.Id} has already been used."
            );
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "A cancelled order cannot be submitted."
            );
        }

        if (order.Status == OrderStatus.Filled)
        {
            throw new InvalidOperationException(
                "A filled order cannot be submitted."
            );
        }
    }
}