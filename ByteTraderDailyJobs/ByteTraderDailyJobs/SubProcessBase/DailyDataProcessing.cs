using ByteTraderDailyJobs.Connections;
using ByteTraderDailyJobs.SubProcessBase.DailyDataProcess;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
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
            await ProcessVolatility();
            await DistributePriceChangeReport();
        }


        //ADTV 10, 20, and 30 day avg, when daily volumne less than 400,000, considered thinly traded. lets aim for 1 million filter
        //method to find dollar volume when needed (price of stock * daily volume) minimum 20 to 15 million
        //volatility indicator based on fixed %

        public async Task ProcessVolatility()
        {
            var TargetChange = 15;
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
                    if (test2 >= endDate.AddDays(-1))
                    {
                        var listOfWeeks = new List<List<Tables.HistoricalDailyCandles>>();
                        var list = test.GetRange(0, 20);
                        listOfWeeks.Add(list.GetRange(0, 5));
                        listOfWeeks.Add(list.GetRange(4, 5));
                        listOfWeeks.Add(list.GetRange(9, 5));
                        listOfWeeks.Add(list.GetRange(14, 5));
                        var priceDictionary = new Dictionary<DateTime, decimal>();
                        foreach (var week in listOfWeeks)
                        {
                            var highest = week.OrderByDescending(e => e.High).ToList();
                            var lowest = week.OrderBy(e => e.Low).ToList();
                            //highLowCandles.Add(highest[0]);
                            //highLowCandles.Add(lowest[0]);

                            priceDictionary.Add(highest[0].DateTime, highest[0].High);
                            priceDictionary.Add(lowest[0].DateTime, lowest[0].Low);
                        }
                        //var ordered = highLowCandles.OrderBy(e => e.DateTime).ToList();
                        var orderedDict = priceDictionary.OrderBy(e => e.Key).ToList();
                        bool skipfirst = false;
                        var volatilityOutput = new List<string>();
                        foreach(var item in orderedDict)
                        {
                            if(skipfirst == false)
                            {
                                skipfirst = true;
                            }
                            else
                            {
                                var previousItem = orderedDict[orderedDict.IndexOf(item) - 1];
                                var percentChange = 100 * ((item.Value - previousItem.Value) / previousItem.Value);
                                if(!(percentChange == (decimal)0))
                                {
                                    bool positive = percentChange > (decimal)0;
                                    bool negative = percentChange < (decimal)0;

                                    if (positive)
                                    {
                                        if (percentChange >= (decimal)TargetChange)
                                        {
                                            volatilityOutput.Add("P");
                                        }
                                    }
                                    else if (negative)
                                    {
                                        if (percentChange <= (decimal)TargetChange * -1)
                                        {
                                            volatilityOutput.Add("N");
                                        }
                                    }
                                }
                                else
                                {
                                    //No Change
                                }
                            }
                        }
                        var countN = volatilityOutput.Where(e => e == "N").Count();
                        var countP = volatilityOutput.Where(e => e == "P").Count();
                        string output = String.Join("-", volatilityOutput.ToArray());

                        await Repo.InsertWeeklyVolume(stock.SymbolId, Repo.GenerateDateString(endDate), TargetChange, countP, countN, output);
                    }
                }
                catch (Exception exc)
                {
                    Logger.Info(exc.ToString());
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
                    if(test2 >= endDate.AddDays(-1))
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
                    Logger.Info(exc.ToString());
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
                    var ListToProcess = await Repo.LoadPercentChangeList(stock.SymbolId);
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
