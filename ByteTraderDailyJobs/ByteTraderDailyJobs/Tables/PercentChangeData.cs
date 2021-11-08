using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.Tables
{
    public class PercentChangeData
    {
        public int SymbolId { get; set; }
        public string MarketDateString { get; set; }
        public DateTime MarketDate { get; set; }
        public DateTime PreviousMarketDate { get; set; }
        public decimal PercentChange { get; set; }
        public decimal AbsoluteChange { get; set; }
        public decimal VolumePercentChange { get; set; }
    }
}
