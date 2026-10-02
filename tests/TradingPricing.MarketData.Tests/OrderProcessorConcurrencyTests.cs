using global::TradingPricing.Core.Market;

namespace TradingPricing.MarketData.Tests
{

    public class OrderProcessorConcurrencyTests
    {
        [Fact]
        public async Task OrderRouter_ShouldRouteOrdersToCorrectOrderBooks()
        {
            var router = new OrderRouter();

            var aapl = new Symbol("AAPL");
            var msft = new Symbol("MSFT");

            await router.SubmitAsync(
                new Order(
                    1,
                    aapl,
                    new Price(100m),
                    new Quantity(50),
                    OrderSide.Buy
                )
            );

            await router.SubmitAsync(
                new Order(
                    2,
                    msft,
                    new Price(200m),
                    new Quantity(75),
                    OrderSide.Sell
                )
            );

            await router.CompleteAsync();

            Assert.True(
                router.TryGetContext(aapl, out var aaplContext)
            );

            Assert.True(
                router.TryGetContext(msft, out var msftContext)
            );

            Assert.Equal(
                new Price(100m),
                aaplContext!.OrderBook.BestBuy!.Price
            );

            Assert.Equal(
                new Price(200m),
                msftContext!.OrderBook.BestSell!.Price
            );
        }
    }
}