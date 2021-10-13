using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.DataObjects
{
    public class Quotes
    {
        public string assetType { get; set; }
        public string assetMainType { get; set; }
        public string cusip { get; set; }
        public string symbol { get; set; }
        public string description { get; set; }
        public Decimal bidPrice { get; set; }
        public Decimal bidSize { get; set; }
        public string bidId { get; set; }
        public Decimal askPrice { get; set; }
        public Decimal askSize { get; set; }
        public string askId { get; set; }
        public Decimal lastPrice { get; set; }
        public Decimal lastSize { get; set; }
        public string lastId { get; set; }
        public string openPrice { get; set; }
        public string highPrice { get; set; }
        public string lowPrice { get; set; }
        public string bidTick { get; set; }
        public string closePrice { get; set; }
        public Decimal netChange { get; set; }
        public string totalVolume { get; set; }
        public long quoteTimeInLong { get; set; }
        public long tradeTimeInLong { get; set; }
        public Decimal mark { get; set; }
        public string exchange { get; set; }
        public string exchangeName { get; set; }
        public bool marginable { get; set; }
        public bool shortable { get; set; }
        public Decimal volatility { get; set; }
        public Decimal digits { get; set; }
        [JsonProperty(PropertyName = "52WkHigh")]
        public Decimal WkHigh { get; set; }
        [JsonProperty(PropertyName = "52WkLow")]
        public Decimal WkLow { get; set; }
        public Decimal nAV { get; set; }
        public Decimal peRatio { get; set; }
        public Decimal divAmount { get; set; }
        public Decimal divYield { get; set; }
        public string divDate { get; set; }
        public string securityStatus { get; set; }
        public Decimal regularMarketLastPrice { get; set; }
        public Decimal regularMarketLastSize { get; set; }
        public Decimal regularMarketNetChange { get; set; }
        public long regularMarketTradeTimeInLong { get; set; }
        public Decimal netPercentChangeInDouble { get; set; }
        public Decimal markChangeInDouble { get; set; }
        public Decimal markPercentChangeInDouble { get; set; }
        public Decimal regularMarketPercentChangeInDouble { get; set; }
        public bool delayed { get; set; }
        public bool realtimeEntitled { get; set; }
    }
}
