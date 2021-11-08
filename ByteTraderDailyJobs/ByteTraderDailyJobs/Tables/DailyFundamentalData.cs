using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.Tables
{
    public class DailyFundamentalData
    {
        public int SymbolId { get; set; }
        public DateTime DateTimeKey { get; set; }
        public string Symbol { get; set; }
        public decimal ReturnOnInvestment { get; set; }
        public decimal QuickRatio { get; set; }
        public decimal CurrentRatio { get; set; }
        public decimal InterestCoverage { get; set; }
        public decimal TotalDebtToCapital { get; set; }
        public decimal LtDebtToEquity { get; set; }
        public decimal TotalDebtToEquity { get; set; }
        public decimal EpsTTM { get; set; }
        public decimal EpsChangePercentTTM { get; set; }
        public decimal EpsChangeYear { get; set; }
        public decimal EpsChange { get; set; }
        public decimal RevChangeYear { get; set; }
        public decimal RevChangeTTM { get; set; }
        public decimal RevChangeIn { get; set; }
        public decimal SharesOutstanding { get; set; }
        public decimal MarketCapFloat { get; set; }
        public decimal MarketCap { get; set; }
        public decimal BookValuePerShare { get; set; }
        public decimal ShortIntToFloat { get; set; }
        public decimal ShortIntDayToCover { get; set; }
        public decimal DivGrowthRate3Year { get; set; }
        public decimal DividendPayAmount { get; set; }
        public DateTime? DividendPayDate { get; set; }
        public decimal Beta { get; set; }
        public decimal Vol1DayAvg { get; set; }
        public decimal ReturnOnAssets { get; set; }
        public decimal ReturnOnEquity { get; set; }
        public decimal OperatingMarginMRQ { get; set; }
        public decimal OperatingMarginTTM { get; set; }
        public decimal High52 { get; set; }
        public decimal Vol10DayAvg { get; set; }
        public decimal DividendAmount { get; set; }
        public decimal DividendYield { get; set; }
        public DateTime? DividendDate { get; set; }
        public decimal PeRatio { get; set; }
        public decimal Low52 { get; set; }
        public decimal PbRatio { get; set; }
        public decimal PegRatio { get; set; }
        public decimal NetProfitMarginMRQ { get; set; }
        public decimal NetProfitMarginTTM { get; set; }
        public decimal Vol3MonthAvg { get; set; }
        public decimal GrossMarginTTM { get; set; }
        public decimal PcfRatio { get; set; }
        public decimal PrRatio { get; set; }
        public decimal GrossMarginMRQ { get; set; }
    }
}
