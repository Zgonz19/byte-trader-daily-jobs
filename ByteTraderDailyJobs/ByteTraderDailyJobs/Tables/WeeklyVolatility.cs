using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.Tables
{
    public class WeeklyVolatility
    {
        public int SymbolId { get; set; }
        public string DateString { get; set; }
        public decimal CountP { get; set; }
        public decimal CountN { get; set; }
        public decimal HighToLowChange { get; set; }
        public decimal ChangeOnClose { get; set; }
        public string ChangeDirection { get; set; }
        public string OrderedChange { get; set; }
        public string AbsoluteChange { get; set; }
        public string UnorderedChange { get; set; }
    }
}
