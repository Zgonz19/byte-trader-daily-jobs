using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.Tables
{
    public class SymbolIndex
    {
        public int SymbolId { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string IsAssetAvailable { get; set; }
    }
}
