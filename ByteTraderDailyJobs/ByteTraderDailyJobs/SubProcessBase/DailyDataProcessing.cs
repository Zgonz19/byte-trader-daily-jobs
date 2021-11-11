using ByteTraderDailyJobs.Connections;
using ByteTraderDailyJobs.SubProcessBase.DailyDataProcess;
using ByteTraderDailyJobs.Tables;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class DailyDataProcessing : ProcessBaseConfig
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

        public ByteTraderRepository Repo = new ByteTraderRepository();
        public EmailEngine EmailEngine { get; set; }
        public DailyDataProcessing()
        {
            EmailEngine = new EmailEngine();
        }
        public override async void ExecuteProcess()
        {
            await InitializePercentChange();
            await ProcessDailyChange();
            await AverageDailyTradeVolume();
            await ProcessWeeklyVolatility();
            await DistributeDailyReport();
            //await DistributePriceChangeReport();
        }


        //ADTV 10, 20, and 30 day avg, when daily volumne less than 400,000, considered thinly traded. lets aim for 1 million filter
        //method to find dollar volume when needed (price of stock * daily volume) minimum 20 to 15 million
        //volatility indicator based on fixed %

        public async Task ProcessWeeklyVolatility()
        {
            var stockList = await Repo.QueryAvailableSymbols();
            var endDate = DateTime.Now;
            var beginDate = endDate.AddDays(-50);
            foreach (var stock in stockList)
            {
                try
                {
                    var candleList = await Repo.QueryCandlesByDate(stock.SymbolId, beginDate, endDate);
                    var test = candleList.OrderByDescending(e => e.DateTime).ToList();
                    var test2 = test.Select(e => e.DateTime).ToList().Max();
                    if (test2 >= endDate.Date.AddDays(-1) && test.Count >= 20)
                    {
                        var listOfWeeks = new List<List<Tables.HistoricalDailyCandles>>();
                        var list = test.GetRange(0, 20);
                        listOfWeeks.Add(list.GetRange(0, 5));
                        listOfWeeks.Add(list.GetRange(5, 5));
                        listOfWeeks.Add(list.GetRange(10, 5));
                        listOfWeeks.Add(list.GetRange(15, 5));
                        var priceDictionary = new List<KeyValuePair<DateTime, decimal>>();
                         //var priceDictionary = new Dictionary<DateTime, decimal>();
                        foreach (var week in listOfWeeks)
                        {
                            var highest = week.OrderByDescending(e => e.High).ToList();
                            var lowest = week.OrderBy(e => e.Low).ToList();
                            priceDictionary.Add(new KeyValuePair<DateTime, decimal>(highest[0].DateTime, highest[0].High));
                            priceDictionary.Add(new KeyValuePair<DateTime, decimal>(lowest[0].DateTime, lowest[0].Low));
                        }
                        var maxhigh = list.Max(e => e.High);
                        var Minlow = list.Min(e => e.Low);
                        var maxclose = list.Max(e => e.Close);
                        var minclose = list.Min(e => e.Close);

                        var highToLowChange = 100 * ((maxhigh - Minlow) / Minlow);
                        var changeOnClose = 100 * ((maxclose - minclose) / minclose);

                        var filteredCandles = priceDictionary;

                        var maxObj = filteredCandles.OrderByDescending(e => e.Value).First();
                        var minObj = filteredCandles.OrderBy(e => e.Value).First();
                        var orderedHighAndLowDict = new List<KeyValuePair<DateTime, decimal>>();
                        orderedHighAndLowDict.Add(maxObj);
                        orderedHighAndLowDict.Add(minObj);
                        orderedHighAndLowDict = orderedHighAndLowDict.OrderBy(e => e.Key).ToList();
                        var orderedHighAndLow = 100 * ((orderedHighAndLowDict[1].Value - orderedHighAndLowDict[0].Value) / orderedHighAndLowDict[0].Value);


                        var orderedDict = priceDictionary.OrderBy(e => e.Key).ToList();
                        var ChangeDirection = new List<string>();
                        var OrderedChange = new List<decimal>();
                        var Absolutechange = new List<decimal>();
                        var UnorderedChange = new List<decimal>();

                        var orderbyAsc = orderedDict.OrderBy(e => e.Value).ToList();
                        var orderbyDesc = orderedDict.OrderByDescending(e => e.Value).ToList();

                        for (int i = 0;  i <= orderedDict.Count - 1; i++)
                        {
                            var percentChange = 100 * ((orderbyAsc[i].Value - orderbyDesc[i].Value) / orderbyDesc[i].Value);
                            UnorderedChange.Add(Decimal.Round(percentChange, 5));
                        }

                        bool skipfirst = false;
                        var currentIndex = 0;
                        var previousIndex = 0;
                        foreach (var item in orderedDict)
                        {
                            if(skipfirst == false)
                            {
                                skipfirst = true;
                            }
                            else
                            {
                                var previousItem = orderedDict[previousIndex];
                                var percentChange = 100 * ((item.Value - previousItem.Value) / previousItem.Value);
                                OrderedChange.Add(Decimal.Round(percentChange, 5));
                                Absolutechange.Add(item.Value - previousItem.Value);

                                if (!(Decimal.Round(percentChange, 4) == (decimal)0))
                                {
                                    bool positive = percentChange > (decimal)0;
                                    bool negative = percentChange < (decimal)0;

                                    if (positive)
                                    {
                                        ChangeDirection.Add("P");
                                        //if (percentChange >= (decimal)TargetChange)
                                        //{
                                        //    volatilityOutput.Add("P");
                                        //}
                                    }
                                    else if (negative)
                                    {
                                        ChangeDirection.Add("N");
                                        //if (percentChange <= (decimal)TargetChange * -1)
                                        //{
                                        //    volatilityOutput.Add("N");
                                        //}
                                    }
                                }
                                else
                                {
                                    ChangeDirection.Add("0");
                                }
                            }
                            previousIndex = currentIndex;
                            currentIndex++;
                        }
                        var countN = ChangeDirection.Where(e => e == "N").Count();
                        var countP = ChangeDirection.Where(e => e == "P").Count();
                        var changeDirection = String.Join("-", ChangeDirection.ToArray());
                        var orderedChange = String.Join("|", OrderedChange.ToArray());
                        var absolutechange = String.Join("|", Absolutechange.ToArray());
                        var unorderedChange = String.Join("|", UnorderedChange.ToArray());

                        await Repo.InsertWeeklyVolatility(stock.SymbolId, Repo.GenerateDateString(endDate), countP, countN, highToLowChange, changeOnClose, changeDirection, orderedChange, absolutechange, unorderedChange, orderedHighAndLow);
                    }
                }
                catch (Exception exc)
                {
                    Logger.Info(stock.Symbol +": " + exc.ToString());
                }
            }
        }

        public async Task AverageDailyTradeVolume()
        {
            var stockList = await Repo.QueryAvailableSymbols();
            var endDate = DateTime.Now;
            var beginDate = endDate.AddDays(-50);
            foreach (var stock in stockList)
            {
                try
                {                   
                    var candleList = await Repo.QueryCandlesByDate(stock.SymbolId, beginDate, endDate);
                    var test = candleList.OrderByDescending(e => e.DateTime).ToList();
                    var test2 = test.Select(e => e.DateTime).ToList().Max();
                    double? avg10;
                    double? avg20;
                    double? avg30;
                    if(test2 >= endDate.Date.AddDays(-1))
                    {
                        if (test.Count >= 30)
                        {
                            avg10 = test.GetRange(0, 10).ToList().Select(e => e.Volume).ToList().Average();
                            avg20 = test.GetRange(0, 20).ToList().Select(e => e.Volume).ToList().Average();
                            avg30 = test.GetRange(0, 30).ToList().Select(e => e.Volume).ToList().Average();
                            await Repo.InsertAverageVolume(stock.SymbolId, Repo.GenerateDateString(endDate), avg10, avg20, avg30);
                        }
                        else if (test.Count >= 20)
                        {
                            avg10 = test.GetRange(0, 10).ToList().Select(e => e.Volume).ToList().Average();
                            avg20 = test.GetRange(0, 20).ToList().Select(e => e.Volume).ToList().Average();
                            await Repo.InsertAverageVolume(stock.SymbolId, Repo.GenerateDateString(endDate), avg10, avg20, null);
                        }
                        else if (test.Count >= 10)
                        {
                            avg10 = test.GetRange(0, 10).ToList().Select(e => e.Volume).ToList().Average();
                            await Repo.InsertAverageVolume(stock.SymbolId, Repo.GenerateDateString(endDate), avg10, null, null);
                        }
                    }
                }
                catch (Exception exc)
                {
                    Logger.Info(stock.Symbol + ": " + exc.ToString());
                }
            }

        }



        public async Task InitializePercentChange()
        {
            var stockList = await Repo.QueryAvailableSymbols();
            var symbolsInPC = await Repo.SymbolsInPercentChangeTable();
            foreach (var stock in stockList)
            {
                if (!symbolsInPC.Contains(stock.SymbolId))
                {
                    try
                    {
                        var endDate = DateTime.Now;
                        var beginDate = endDate.AddDays(-40);
                        var candleList = await Repo.QueryCandlesByDate(stock.SymbolId, beginDate, endDate);
                        var percentChange = new PercentChange(candleList);
                        percentChange.CalculateChange();
                        await Repo.BulkPercentChangeInsert(percentChange.CalculatedData);
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }

                }
            }
        }

        public void WriteToCsvFile(DataTable dataTable, string filePath)
        {
            StringBuilder fileContent = new StringBuilder();

            foreach (var col in dataTable.Columns)
            {
                fileContent.Append(col.ToString() + ",");
            }

            fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);

            foreach (DataRow dr in dataTable.Rows)
            {
                foreach (var column in dr.ItemArray)
                {
                    fileContent.Append("" + column.ToString() + ",");
                }

                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
            }

            System.IO.File.WriteAllText(filePath, fileContent.ToString());
        }
        public async Task DistributeDailyReport()
        {
            try
            {
                Logger.Info("Generating Daily Report...");
                var reportDate = DateTime.Now.Date;
                var stockList = await Repo.QueryAvailableSymbols();
                var dailyReport = await GenerateDailyReport(stockList, reportDate);
                var reportPath = @"C:\Users\Gonzalo\Dropbox\Github_08_2021\byte-trader-daily-jobs\DailyReport\" + $"ByteTrader_DailyReport_{Repo.GenerateDateString(reportDate)}.csv";
                WriteToCsvFile(dailyReport, reportPath);
                var subscribedUsers = await Repo.GetAppUsers();
                var emails = subscribedUsers.Select(e => e.Email).ToList();
                foreach (var email in emails)
                {
                    await EmailEngine.SendEmailAttachment(email, "", $"ByteTrader Report {reportDate.ToString("yyyy-MM-dd")}", reportPath);
                }
                Logger.Info("Daily Report Distribution Complete...");
            }
            catch(Exception exc)
            {
                Logger.Info(exc.ToString());
            }

        }

        public async Task<DataTable> GenerateDailyReport(List<SymbolIndex> symbols, DateTime reportDate)
        {
            var dateString = Repo.GenerateDateString(reportDate);
            var fundamentals = await Repo.QueryFundamentalByDate(reportDate);
            var table = new DataTable();
            table.Columns.Add("SymbolId", typeof(int));
            table.Columns.Add("Symbol", typeof(string));
            table.Columns.Add("CountP", typeof(decimal));
            table.Columns.Add("CountN", typeof(decimal));
            table.Columns.Add("DailyVolumeChange", typeof(decimal));           
            table.Columns.Add("DailyPercentChange", typeof(decimal));
            table.Columns.Add("OrderedHighAndLow", typeof(decimal));
            table.Columns.Add("HighToLowChange", typeof(decimal));
            table.Columns.Add("ChangeOnClose", typeof(decimal));
            table.Columns.Add("ChangeDirection", typeof(string));
            table.Columns.Add("OrderedChange", typeof(string));
            table.Columns.Add("AbsoluteChange", typeof(string));
            table.Columns.Add("UnorderedChange", typeof(string));
            table.Columns.Add("VolumeChangeSeries", typeof(string));       
            table.Columns.Add("PercentChangeSeries", typeof(string));
            table.Columns.Add("ChangeSeriesSum", typeof(decimal));
            table.Columns.Add("AvgVolume10", typeof(decimal));
            table.Columns.Add("AvgVolume20", typeof(decimal));
            table.Columns.Add("AvgVolume30", typeof(decimal));
            table.Columns.Add("DailyOpen", typeof(decimal));
            table.Columns.Add("DailyHigh", typeof(decimal));
            table.Columns.Add("DailyLow", typeof(decimal));
            table.Columns.Add("DailyClose", typeof(decimal));
            table.Columns.Add("DailyVolume", typeof(long));
            table.Columns.Add("ReturnOnInvestment", typeof(decimal));
            table.Columns.Add("QuickRatio", typeof(decimal));
            table.Columns.Add("CurrentRatio", typeof(decimal));
            table.Columns.Add("InterestCoverage", typeof(decimal));
            table.Columns.Add("TotalDebtToCapital", typeof(decimal));
            table.Columns.Add("LtDebtToEquity", typeof(decimal));
            table.Columns.Add("TotalDebtToEquity", typeof(decimal));
            table.Columns.Add("EpsTTM", typeof(decimal));
            table.Columns.Add("EpsChangePercentTTM", typeof(decimal));
            table.Columns.Add("EpsChangeYear", typeof(decimal));
            table.Columns.Add("EpsChange", typeof(decimal));
            table.Columns.Add("RevChangeYear", typeof(decimal));
            table.Columns.Add("RevChangeTTM", typeof(decimal));
            table.Columns.Add("RevChangeIn", typeof(decimal));
            table.Columns.Add("SharesOutstanding", typeof(decimal));
            table.Columns.Add("MarketCapFloat", typeof(decimal));
            table.Columns.Add("MarketCap", typeof(decimal));
            table.Columns.Add("BookValuePerShare", typeof(decimal));
            table.Columns.Add("ShortIntToFloat", typeof(decimal));
            table.Columns.Add("ShortIntDayToCover", typeof(decimal));
            table.Columns.Add("DivGrowthRate3Year", typeof(decimal));
            table.Columns.Add("DividendPayAmount", typeof(decimal));
            table.Columns.Add("DividendPayDate", typeof(DateTime));
            table.Columns.Add("Beta", typeof(decimal));
            table.Columns.Add("Vol1DayAvg", typeof(decimal));
            table.Columns.Add("ReturnOnAssets", typeof(decimal));
            table.Columns.Add("ReturnOnEquity", typeof(decimal));
            table.Columns.Add("OperatingMarginMRQ", typeof(decimal));
            table.Columns.Add("OperatingMarginTTM", typeof(decimal));
            table.Columns.Add("High52", typeof(decimal));
            table.Columns.Add("Vol10DayAvg", typeof(decimal));
            table.Columns.Add("DividendAmount", typeof(decimal));
            table.Columns.Add("DividendYield", typeof(decimal));
            table.Columns.Add("DividendDate", typeof(DateTime));
            table.Columns.Add("PeRatio", typeof(decimal));
            table.Columns.Add("Low52", typeof(decimal));
            table.Columns.Add("PbRatio", typeof(decimal));
            table.Columns.Add("PegRatio", typeof(decimal));
            table.Columns.Add("NetProfitMarginMRQ", typeof(decimal));
            table.Columns.Add("NetProfitMarginTTM", typeof(decimal));
            table.Columns.Add("Vol3MonthAvg", typeof(decimal));
            table.Columns.Add("GrossMarginTTM", typeof(decimal));
            table.Columns.Add("PcfRatio", typeof(decimal));
            table.Columns.Add("PrRatio", typeof(decimal));
            table.Columns.Add("GrossMarginMRQ", typeof(decimal));

            foreach(var asset in symbols)
            {
                //String.Join("|", OrderedChange.ToArray());
                var changeData = await Repo.PercentChangeByDate(asset.SymbolId, reportDate.AddDays(-6), reportDate.AddHours(18));
                changeData = changeData.OrderBy(e => e.MarketDate).ToList();
                var changeSeries = changeData.Select(e => e.PercentChange).ToArray();
                var changeSeriesSum = changeData.Sum(e => e.PercentChange);
                var VolumeChangeSeries = changeData.Select(e => e.VolumePercentChange).ToArray();
                var dailyChangeObj = changeData.OrderByDescending(e => e.MarketDate).FirstOrDefault();
                decimal PercentChange = 0;
                decimal VolumePercentChange = 0;
                if (dailyChangeObj != null)
                {
                    PercentChange = dailyChangeObj.PercentChange;
                    VolumePercentChange = dailyChangeObj.VolumePercentChange;

                }
                var volatility = await Repo.QueryWeeklyVolatility(asset.SymbolId, dateString);
                if (volatility == null)
                {
                    volatility = new WeeklyVolatility();
                }
                var volume = await Repo.QueryAverageTradeVolume(asset.SymbolId, dateString);
                if (volume == null)
                {
                    volume = new AverageTradeVolume();
                }
                var candles = await Repo.QueryDailyCandles(asset.SymbolId, dateString);
                if(candles == null)
                {
                    candles = new HistoricalDailyCandles();
                }
                var fundamental = fundamentals.FirstOrDefault(e => e.SymbolId == asset.SymbolId);
                if(fundamental == null)
                {
                    fundamental = new DailyFundamentalData();
                }
                var row = new Object[]
                {
                    asset.SymbolId,
                    asset.Symbol,
                    volatility.CountP,
                    volatility.CountN,
                    VolumePercentChange,
                    PercentChange,
                    volatility.OrderedHighAndLow,
                    volatility.HighToLowChange,
                    volatility.ChangeOnClose,
                    volatility.ChangeDirection,
                    volatility.OrderedChange,
                    volatility.AbsoluteChange,
                    volatility.UnorderedChange,
                    String.Join("|", VolumeChangeSeries),
                    String.Join("|", changeSeries),
                    changeSeriesSum,
                    volume.Avg10,
                    volume.Avg20,
                    volume.Avg30,
                    candles.Open,
                    candles.High,
                    candles.Low,
                    candles.Close,
                    candles.Volume,
                    fundamental.ReturnOnInvestment,
                    fundamental.QuickRatio,
                    fundamental.CurrentRatio,
                    fundamental.InterestCoverage,
                    fundamental.TotalDebtToCapital,
                    fundamental.LtDebtToEquity,
                    fundamental.TotalDebtToEquity,
                    fundamental.EpsTTM,
                    fundamental.EpsChangePercentTTM,
                    fundamental.EpsChangeYear,
                    fundamental.EpsChange,
                    fundamental.RevChangeYear,
                    fundamental.RevChangeTTM,
                    fundamental.RevChangeIn,
                    fundamental.SharesOutstanding,
                    fundamental.MarketCapFloat,
                    fundamental.MarketCap,
                    fundamental.BookValuePerShare,
                    fundamental.ShortIntToFloat,
                    fundamental.ShortIntDayToCover,
                    fundamental.DivGrowthRate3Year,
                    fundamental.DividendPayAmount,
                    fundamental.DividendPayDate,
                    fundamental.Beta,
                    fundamental.Vol1DayAvg,
                    fundamental.ReturnOnAssets,
                    fundamental.ReturnOnEquity,
                    fundamental.OperatingMarginMRQ,
                    fundamental.OperatingMarginTTM,
                    fundamental.High52,
                    fundamental.Vol10DayAvg,
                    fundamental.DividendAmount,
                    fundamental.DividendYield,
                    fundamental.DividendDate,
                    fundamental.PeRatio,
                    fundamental.Low52,
                    fundamental.PbRatio,
                    fundamental.PegRatio,
                    fundamental.NetProfitMarginMRQ,
                    fundamental.NetProfitMarginTTM,
                    fundamental.Vol3MonthAvg,
                    fundamental.GrossMarginTTM,
                    fundamental.PcfRatio,
                    fundamental.PrRatio,
                    fundamental.GrossMarginMRQ
                };
                table.Rows.Add(row);
            }
            return table;
        }
        public string CreateHtmlReport(List<ChangeDataReport> gainers, List<ChangeDataReport> losers)
        {
            var htmlBody = new StringBuilder();
            //gainers
            htmlBody.Append($"<h2>Top Gainers Data Report</h2>");
            htmlBody.Append($"<table>");

            htmlBody.Append($"<thead>");
            htmlBody.Append($"<tr>");
            htmlBody.Append($"<th>Symbol</th>");
            htmlBody.Append($"<th>Percent Change</th>");
            htmlBody.Append($"<th>Company Name</th>");
            htmlBody.Append($"<th>Absolute Change</th>");
            htmlBody.Append($"</tr>");
            htmlBody.Append($"</thead>");

            htmlBody.Append($"<tbody>");
            foreach (var report in gainers)
            {
                htmlBody.Append($"<tr>");
                htmlBody.Append($"<td>{report.Symbol}</td>");
                htmlBody.Append($"<td>{report.PercentChange}</td>");
                htmlBody.Append($"<td>{report.CompanyName}</td>");
                htmlBody.Append($"<td>{report.AbsoluteChange}</td>");
                htmlBody.Append($"</tr>");
            }
            htmlBody.Append($"</tbody>");

            htmlBody.Append($"</table>");

            htmlBody.Append($"<br>");
            htmlBody.Append($"<br>");

            //losers
            htmlBody.Append($"<h2>Top Losers Data Report</h2>");
            htmlBody.Append($"<table>");

            htmlBody.Append($"<thead>");
            htmlBody.Append($"<tr>");
            htmlBody.Append($"<th>Symbol</th>");
            htmlBody.Append($"<th>Percent Change</th>");
            htmlBody.Append($"<th>Company Name</th>");
            htmlBody.Append($"<th>Absolute Change</th>");
            htmlBody.Append($"</tr>");
            htmlBody.Append($"</thead>");

            htmlBody.Append($"<tbody>");
            foreach (var report in losers)
            {
                htmlBody.Append($"<tr>");
                htmlBody.Append($"<td>{report.Symbol}</td>");
                htmlBody.Append($"<td>{report.PercentChange}</td>");
                htmlBody.Append($"<td>{report.CompanyName}</td>");
                htmlBody.Append($"<td>{report.AbsoluteChange}</td>");
                htmlBody.Append($"</tr>");
            }
            htmlBody.Append($"</tbody>");

            htmlBody.Append($"</table>");

            return htmlBody.ToString();
        }

        public async Task DistributePriceChangeReport()
        {
            //var date = new DateTime(2020, 9, 18);
            var date = DateTime.Now.Date;
            var gainers = await Repo.LoadTopDailyGainers(date);
            var losers = await Repo.LoadTopDailyLosers(date);
            var subscribedUsers = await Repo.GetAppUsers();
            //subscribedUsers = subscribedUsers.Where(e => e.EnableDailyReports == "Y").ToList();
            var emails = subscribedUsers.Select(e => e.Email).ToList();
            var subject = $"Data Report For {date.ToShortDateString()}";
            var body = CreateHtmlReport(gainers, losers);
            await EmailEngine.EmailBatchTemplate(emails, subject, body, true);
        }


        public async Task ProcessDailyChange()
        {
            try
            {
                var processDate = DateTime.Now;
                var stockList = await Repo.QueryAvailableSymbols();
                foreach (var stock in stockList)
                {
                    var ListToProcess = Repo.LoadPercentChangeList(stock.SymbolId).Result;
                    var percentChange = new PercentChange(ListToProcess);
                    percentChange.CalculateChange();
                    //var result = percentChange.CalculatedData;
                    await Repo.BulkPercentChangeInsert(percentChange.CalculatedData);
                }
            }
            catch (Exception Exc)
            {
                Logger.Info(Exc.ToString());
            }
        }

    }
}
