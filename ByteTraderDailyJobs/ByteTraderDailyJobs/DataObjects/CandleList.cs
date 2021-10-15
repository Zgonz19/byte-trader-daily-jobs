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
        public string close { get; set; }
        public DateTime datetime { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string open { get; set; }
        public string volume { get; set; }
    }


}
