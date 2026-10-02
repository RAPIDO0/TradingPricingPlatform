using System;
using System.Collections.Generic;
using System.Text;
using TradingPricing.Core.Market;

namespace TradingPricing.MarketData
{
    public sealed class OrderBookContext
    {
        public Symbol Symbol { get;  }
        public OrderBook OrderBook { get; }
        public OrderProcessor Processor { get;  }
        public Task ProcessingTask { get; }

        public OrderBookContext(Symbol symbol)
        {
            Symbol = symbol;

            OrderBook = new OrderBook(symbol);

            Processor = new OrderProcessor(OrderBook);

            ProcessingTask = Processor.RunAsync();
        }

        public void Complete()
        {
            Processor.Complete();
        }
    }
}
