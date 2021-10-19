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
            await DistributePriceChangeReport();
        }


        //ADTV 10, 20, and 30 day avg, when daily volumne less than 400,000, considered thinly traded. lets aim for 1 million filter
        //method to find dollar volume when needed (price of stock * daily volume) minimum 20 to 15 million



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
