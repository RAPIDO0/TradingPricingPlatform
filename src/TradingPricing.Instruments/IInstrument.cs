using System;
using System.Collections.Generic;
using System.Text;
using TradingPricing.Core.Market;

namespace TradingPricing.Instruments
{
    public interface IInstrument
    {
        string Id { get; }
        Symbol Symbol { get; }
    }
}
