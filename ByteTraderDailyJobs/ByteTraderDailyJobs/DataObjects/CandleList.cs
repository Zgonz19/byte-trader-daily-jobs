using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.DataObjects
{
    public class CandleList
    {
        public List<candles> candles { get; set; }
        public bool empty { get; set; }
        public string symbol { get; set; }
    }
    public class candles
    {
        public Decimal close { get; set; }
        public long datetime { get; set; }
        public Decimal high { get; set; }
        public Decimal low { get; set; }
        public Decimal open { get; set; }
        public int volume { get; set; }
    }


}
