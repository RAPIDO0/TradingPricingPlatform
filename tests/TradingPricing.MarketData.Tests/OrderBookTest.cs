using TradingPricing.Core.Market;
using TradingPricing.MarketData;

namespace TradingPricing.MarketData.Tests;

public class OrderBookTests
{
    private static readonly Symbol Aapl = new("AAPL");

    [Fact]
    public void SubmitOrder_WhenNoMatch_AddsOrderToBook()
    {
        // Arrange
        var orderBook = new OrderBook(Aapl);

        var buyOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Buy
        );

        // Act
        var trades = orderBook.SubmitOrder(buyOrder);

        // Assert
        Assert.Empty(trades);

        Assert.NotNull(orderBook.BestBuy);
        Assert.Equal(1, orderBook.BestBuy!.Id);

        Assert.Null(orderBook.BestSell);

        Assert.Equal(
            new Quantity(50),
            buyOrder.RemainingQuantity
        );

        Assert.Equal(
            OrderStatus.New,
            buyOrder.Status
        );
    }

    [Fact]
    public void SubmitOrder_WhenOrdersFullyMatch_FillsBothOrders()
    {
        // Arrange
        var orderBook = new OrderBook(Aapl);

        var sellOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Sell
        );

        orderBook.SubmitOrder(sellOrder);

        var buyOrder = new Order(
            id: 2,
            symbol: Aapl,
            price: new Price(101m),
            quantity: new Quantity(50),
            side: OrderSide.Buy
        );

        // Act
        var trades = orderBook.SubmitOrder(buyOrder);

        // Assert
        Assert.Single(trades);

        Trade trade = trades[0];

        Assert.Equal(new Price(100m), trade.Price);
        Assert.Equal(new Quantity(50), trade.Quantity);

        Assert.Equal(2, trade.BuyOrderId);
        Assert.Equal(1, trade.SellOrderId);

        Assert.Equal(OrderStatus.Filled, buyOrder.Status);
        Assert.Equal(OrderStatus.Filled, sellOrder.Status);

        Assert.Equal(
            new Quantity(0),
            buyOrder.RemainingQuantity
        );

        Assert.Equal(
            new Quantity(0),
            sellOrder.RemainingQuantity
        );

        Assert.Null(orderBook.BestBuy);
        Assert.Null(orderBook.BestSell);
    }

    [Fact]
    public void SubmitOrder_WhenIncomingOrderIsPartiallyFilled_AddsRemainingQuantityToBook()
    {
        // Arrange
        var orderBook = new OrderBook(Aapl);

        var sellOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(40),
            side: OrderSide.Sell
        );

        orderBook.SubmitOrder(sellOrder);

        var buyOrder = new Order(
            id: 2,
            symbol: Aapl,
            price: new Price(101m),
            quantity: new Quantity(100),
            side: OrderSide.Buy
        );

        // Act
        var trades = orderBook.SubmitOrder(buyOrder);

        // Assert
        Assert.Single(trades);

        Assert.Equal(
            new Quantity(40),
            trades[0].Quantity
        );

        Assert.Equal(
            OrderStatus.Filled,
            sellOrder.Status
        );

        Assert.Equal(
            OrderStatus.PartiallyFilled,
            buyOrder.Status
        );

        Assert.Equal(
            new Quantity(60),
            buyOrder.RemainingQuantity
        );

        Assert.NotNull(orderBook.BestBuy);

        Assert.Equal(
            buyOrder.Id,
            orderBook.BestBuy!.Id
        );

        Assert.Null(orderBook.BestSell);
    }

    [Fact]
    public void SubmitOrder_WhenIncomingOrderCrossesMultiplePriceLevels_CreatesMultipleTrades()
    {
        // Arrange
        var orderBook = new OrderBook(Aapl);

        var sell1 = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Sell
        );

        var sell2 = new Order(
            id: 2,
            symbol: Aapl,
            price: new Price(101m),
            quantity: new Quantity(70),
            side: OrderSide.Sell
        );

        var sell3 = new Order(
            id: 3,
            symbol: Aapl,
            price: new Price(102m),
            quantity: new Quantity(100),
            side: OrderSide.Sell
        );

        orderBook.SubmitOrder(sell1);
        orderBook.SubmitOrder(sell2);
        orderBook.SubmitOrder(sell3);

        var buyOrder = new Order(
            id: 4,
            symbol: Aapl,
            price: new Price(102m),
            quantity: new Quantity(150),
            side: OrderSide.Buy
        );

        // Act
        var trades = orderBook.SubmitOrder(buyOrder);

        // Assert
        Assert.Equal(3, trades.Count);

        Assert.Equal(new Price(100m), trades[0].Price);
        Assert.Equal(new Quantity(50), trades[0].Quantity);

        Assert.Equal(new Price(101m), trades[1].Price);
        Assert.Equal(new Quantity(70), trades[1].Quantity);

        Assert.Equal(new Price(102m), trades[2].Price);
        Assert.Equal(new Quantity(30), trades[2].Quantity);

        Assert.Equal(OrderStatus.Filled, sell1.Status);
        Assert.Equal(OrderStatus.Filled, sell2.Status);

        Assert.Equal(
            OrderStatus.PartiallyFilled,
            sell3.Status
        );

        Assert.Equal(
            new Quantity(70),
            sell3.RemainingQuantity
        );

        Assert.Equal(OrderStatus.Filled, buyOrder.Status);

        Assert.Equal(
            new Quantity(0),
            buyOrder.RemainingQuantity
        );

        Assert.NotNull(orderBook.BestSell);

        Assert.Equal(
            sell3.Id,
            orderBook.BestSell!.Id
        );

        Assert.Null(orderBook.BestBuy);
    }

    [Fact]
    public void SubmitOrder_WhenSamePrice_UsesTimePriority()
    {
        // Arrange
        var orderBook = new OrderBook(Aapl);

        var firstSell = new Order(
            id: 10,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(30),
            side: OrderSide.Sell
        );

        var secondSell = new Order(
            id: 11,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(30),
            side: OrderSide.Sell
        );

        orderBook.SubmitOrder(firstSell);
        orderBook.SubmitOrder(secondSell);

        var buyOrder = new Order(
            id: 12,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(40),
            side: OrderSide.Buy
        );

        // Act
        var trades = orderBook.SubmitOrder(buyOrder);

        // Assert
        Assert.Equal(2, trades.Count);

        Assert.Equal(10, trades[0].SellOrderId);
        Assert.Equal(new Quantity(30), trades[0].Quantity);

        Assert.Equal(11, trades[1].SellOrderId);
        Assert.Equal(new Quantity(10), trades[1].Quantity);

        Assert.Equal(OrderStatus.Filled, firstSell.Status);

        Assert.Equal(
            OrderStatus.PartiallyFilled,
            secondSell.Status
        );

        Assert.Equal(
            new Quantity(20),
            secondSell.RemainingQuantity
        );

        Assert.Equal(
            11,
            orderBook.BestSell!.Id
        );
    }

    [Fact]
    public void SubmitOrder_WhenOrderIdWasAlreadyUsed_ThrowsException()
    {
        // Arrange
        var orderBook = new OrderBook(Aapl);

        var sellOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Sell
        );

        var buyOrder = new Order(
            id: 2,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Buy
        );

        orderBook.SubmitOrder(sellOrder);
        orderBook.SubmitOrder(buyOrder);

        // Les deux ordres sont maintenant Filled
        // et ont disparu du carnet.

        var reusedOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(99m),
            quantity: new Quantity(100),
            side: OrderSide.Buy
        );

        // Act + Assert
        Assert.Throws<InvalidOperationException>(
            () => orderBook.SubmitOrder(reusedOrder)
        );
    }

    [Fact]
    public void SubmitOrder_WhenTradeOccurs_RaisesTradeExecuted()
    {
        var orderBook = new OrderBook(Aapl);

        Trade? receivedTrade = null;

        orderBook.TradeExecuted += trade =>
        {
            receivedTrade = trade;
        };

        var sellOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Sell
        );

        var buyOrder = new Order(
            id: 2,
            symbol: Aapl,
            price: new Price(101m),
            quantity: new Quantity(50),
            side: OrderSide.Buy
        );

        orderBook.SubmitOrder(sellOrder);
        orderBook.SubmitOrder(buyOrder);

        Assert.NotNull(receivedTrade);
        Assert.Equal(new Price(100m), receivedTrade!.Price);
        Assert.Equal(new Quantity(50), receivedTrade.Quantity);
    }

    [Fact]
    public void CancelOrder_WhenOrderExists_RaisesOrderCancelled()
    {
        var orderBook = new OrderBook(Aapl);

        Order? cancelledOrder = null;

        orderBook.OrderCancelled += order =>
        {
            cancelledOrder = order;
        };

        var buyOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Buy
        );

        orderBook.SubmitOrder(buyOrder);

        bool result = orderBook.CancelOrder(1);

        Assert.True(result);
        Assert.NotNull(cancelledOrder);
        Assert.Equal(1, cancelledOrder!.Id);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder.Status);
    }

    [Fact]
    public void SubmitOrder_WhenBookChanges_RaisesOrderBookChanged()
    {
        var orderBook = new OrderBook(Aapl);

        int eventCount = 0;

        orderBook.OrderBookChanged += () =>
        {
            eventCount++;
        };

        var buyOrder = new Order(
            id: 1,
            symbol: Aapl,
            price: new Price(100m),
            quantity: new Quantity(50),
            side: OrderSide.Buy
        );

        orderBook.SubmitOrder(buyOrder);

        Assert.True(eventCount > 0);
    }
}