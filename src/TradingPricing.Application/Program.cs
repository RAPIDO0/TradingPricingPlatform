using TradingPricing.Core.Market;
using TradingPricing.MarketData;

Console.WriteLine("Hello, World!");
var aapl = new Symbol("AAPL");

var orderBook = new OrderBook(aapl);

orderBook.TradeExecuted += trade =>
{
    Console.WriteLine(
        $"TRADE: {trade.Quantity.Value} AAPL @ {trade.Price.Value}"
    );
};

orderBook.OrderCancelled += order =>
{
    Console.WriteLine(
        $"ORDER CANCELLED: {order.Id}"
    );
};

var sellOrder = new Order(
    id: 1,
    symbol: aapl,
    price: new Price(100m),
    quantity: new Quantity(50),
    side: OrderSide.Sell
);

var buyOrder = new Order(
    id: 2,
    symbol: aapl,
    price: new Price(101m),
    quantity: new Quantity(50),
    side: OrderSide.Buy
);

orderBook.SubmitOrder(sellOrder);
orderBook.SubmitOrder(buyOrder);