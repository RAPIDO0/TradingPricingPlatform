using global::TradingPricing.Core.Market;

namespace TradingPricing.MarketData.Tests
{

    public class OrderProcessorConcurrencyTests
    {
        private static readonly Symbol Aapl = new("AAPL");

        [Fact]
        public async Task MultipleWorkers_ShouldNotDoubleFillSameRestingOrder()
        {
            // Arrange
            var orderBook = new OrderBook(Aapl);
            var processor = new OrderProcessor(orderBook);

            int tradeCount = 0;
            long totalExecutedQuantity = 0;

            orderBook.TradeExecuted += trade =>
            {
                Interlocked.Increment(ref tradeCount);
                Interlocked.Add(
                    ref totalExecutedQuantity,
                    trade.Quantity.Value
                );
            };

            // One resting SELL with only 100 shares available
            var restingSell = new Order(
                id: 1,
                symbol: Aapl,
                price: new Price(100m),
                quantity: new Quantity(100),
                side: OrderSide.Sell
            );

            orderBook.SubmitOrder(restingSell);

            // Start 4 concurrent consumers
            Task workers = processor.RunWorkersAsync(workerCount: 4);

            // Two BUY orders both able to fully consume the SELL
            await processor.SubmitAsync(
                new Order(
                    id: 2,
                    symbol: Aapl,
                    price: new Price(101m),
                    quantity: new Quantity(100),
                    side: OrderSide.Buy
                )
            );

            await processor.SubmitAsync(
                new Order(
                    id: 3,
                    symbol: Aapl,
                    price: new Price(101m),
                    quantity: new Quantity(100),
                    side: OrderSide.Buy
                )
            );

            processor.Complete();

            await workers;

            // Assert
            Assert.Equal(
                100,
                totalExecutedQuantity
            );

            Assert.Equal(
                OrderStatus.Filled,
                restingSell.Status
            );

            Assert.Equal(
                new Quantity(0),
                restingSell.RemainingQuantity
            );

            // One BUY must remain in the book
            Assert.NotNull(orderBook.BestBuy);

            Assert.Equal(
                new Quantity(100),
                orderBook.BestBuy!.RemainingQuantity
            );
        }

        [Fact]
        public async Task MultipleWorkers_WithManyOrders_ShouldPreserveTotalQuantity()
        {
            var orderBook = new OrderBook(Aapl);
            var processor = new OrderProcessor(orderBook);

            long totalExecuted = 0;

            orderBook.TradeExecuted += trade =>
            {
                Interlocked.Add(
                    ref totalExecuted,
                    trade.Quantity.Value
                );
            };

            // 100 SELL orders * 10 = 1000 shares
            for (int i = 0; i < 100; i++)
            {
                orderBook.SubmitOrder(
                    new Order(
                        id: i + 1,
                        symbol: Aapl,
                        price: new Price(100m),
                        quantity: new Quantity(10),
                        side: OrderSide.Sell
                    )
                );
            }

            Task workers = processor.RunWorkersAsync(8);

            // 100 BUY orders * 10 = 1000 shares
            for (int i = 0; i < 100; i++)
            {
                await processor.SubmitAsync(
                    new Order(
                        id: 1000 + i,
                        symbol: Aapl,
                        price: new Price(101m),
                        quantity: new Quantity(10),
                        side: OrderSide.Buy
                    )
                );
            }

            processor.Complete();

            await workers;

            Assert.Equal(1000, totalExecuted);

            Assert.Null(orderBook.BestBuy);
            Assert.Null(orderBook.BestSell);
        }
    }
}