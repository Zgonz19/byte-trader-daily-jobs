using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.Tables
{
    public class AverageTradeVolume
    {
        public int SymbolId { get; set; }
        public string DateString { get; set; }
        public decimal Avg10 { get; set; }
        public decimal Avg20 { get; set; }
        public decimal Avg30 { get; set; }
    }
}
