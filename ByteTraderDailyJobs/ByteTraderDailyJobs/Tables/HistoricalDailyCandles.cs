using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.Tables
{
    public class HistoricalDailyCandles
    {
        public int SymbolId { get; set; }
        public string DateString { get; set; }
        public string Symbol { get; set; }
        public string MarketDate { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public decimal AdjustedClose { get; set; }
        public DateTime DateTime { get; set; }

    }
}
