using System;
using System.Collections.Generic;
using System.Text;
using TradingPricing.Core.Market;

namespace TradingPricing.Instruments
{
    public sealed class Equity : IInstrument
	{
        public string Id { get; }
        public Symbol Symbol { get; }

        public Currency Currency { get; set; }

        public Equity(string id, Symbol symbol, Currency currency)
        {
            this.Id = id;
            this.Symbol = symbol;
            this.Currency = currency;
		}
	}
}
