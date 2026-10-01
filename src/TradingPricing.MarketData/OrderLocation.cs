using System;
using System.Collections.Generic;
using System.Text;
using TradingPricing.Core.Market;

namespace TradingPricing.MarketData
{
    internal sealed class OrderLocation
    {
        public Price Price { get; }
        public OrderSide Side { get; }
        public LinkedListNode<Order> Node { get; }

        public OrderLocation(
            Price price,
            OrderSide side,
            LinkedListNode<Order> node)
        {
            Price = price;
            Side = side;
            Node = node;
        }
    }
}
