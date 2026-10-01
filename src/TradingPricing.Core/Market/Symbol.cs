using System;
using System.Collections.Generic;
using System.Text;

namespace TradingPricing.Core.Market
{
    class Test
    {
        public static void Main(string[] args)
        {
            // Example usage of the Symbol record struct
            Symbol symbol = new Symbol("AAPL");
            Console.WriteLine($"Symbol: {symbol.symbol}");
        }
    }
    public readonly record struct Symbol(string symbol)
    {
    }
}
