using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using ByteTraderDailyJobs.DataObjects;
using System.Linq;
using Alpaca.Markets;
using ByteTraderDailyJobs.Connections;
using ByteTraderDailyJobs.SubProcessBase.DailyCandleIngestionProcess;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using NLog.Web;
using NLog;
using Abbotware.Interop.TDAmeritrade.Models;
using ByteTraderDailyJobs.Tables;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class DailyCandleIngestion : ProcessBaseConfig
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        public string AlpacaApiKey { get; set; }
        public string AlpacaSecretKey { get; set; }
        public string TDAmeritradeApiKey { get; set; }
        public AmeritradeApiWrapper ApiWrapper = new AmeritradeApiWrapper();

        public ByteTraderRepository Repo = new ByteTraderRepository();
        public DailyCandleIngestion()
        {

        }

        public DailyCandleIngestion(string name)
        {

        }
        public override async void ExecuteProcess()
        {

            //ProcessTestingMethod();
            await AlpacaDataIngestion();
            await AmeritradeFundamentalData();
            //await PriceHistoryDataUpdate();
        }
        public async Task PriceHistoryDataUpdate()
        {
            var runDate = DateTime.Now;
            var repo = new ByteTraderRepository();
            var tickers = await repo.QueryNightlyBars();

            //tickers = tickers.Where(e => e.SymbolId == 999).ToList();

            foreach (var symbol in tickers)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                var urlMaxDate = symbol.MaxDate.AddHours(0);
                var apiUrl = BuildPriceHistoryUrl(urlMaxDate, runDate, symbol.Symbol);
                var apiCall = new BaseApiCall(apiUrl);
                try
                {
                    var retry = 0;
                    do
                    {
                        await apiCall.CallApi();
                        //apiCall.SetResponseObject<CandleList>();

                        if(apiCall.ResponseObject.candles.Count == 0)
                        {
                            retry++;
                            Thread.Sleep(TimeSpan.FromSeconds(20));

                        }
                        else
                        {
                            retry = 4;
                        }                    
                    } while (retry < 3);

                    if (!(apiCall.ResponseObject.candles.Count == 0))
                    {
                        var bars = (List<candles>)apiCall.ResponseObject.candles;
                        var filteredBars = FilterOutput(bars, symbol, runDate);
                        repo.InsertHistoricalCandles(filteredBars, symbol.Symbol, symbol.SymbolId);
                    }
                }
                catch (Exception exc)
                {
                    
                }
            }

            List<candles> FilterOutput(List<candles> bars, NightlyBarsModel barsModel, DateTime runDate)
            {
                //var maxDateUnix = (long?)DateTimeToUnixTimestamp(barsModel.MaxDate);
                var maxDateUnix = runDate;
                var test1 = runDate;
                var runDateUnix = test1;
                var datesList = bars.Select(e => e.datetime).ToList();
                if (datesList.Contains(maxDateUnix))
                {
                    var candle = bars.FirstOrDefault(e => e.datetime == maxDateUnix);
                    bars.Remove(candle);
                    //var datemax = datesList.Max();
                    //var datemin = datesList.Min();
                    //var maxdate = new DateTime((long)datemax);
                    //var mindate = new DateTime((long)datemin);
                    //if (maxdate == barsModel.MaxDate || mindate == barsModel.MaxDate)
                    //{
                    //}
                }
                if (datesList.Contains(runDateUnix))
                {
                    var candle = bars.FirstOrDefault(e => e.datetime == runDateUnix);
                    bars.Remove(candle);
                }
                return bars;
            }
        }
        public List<candles> FilterOutput(List<candles> bars, NightlyBarsModel barsModel, DateTime runDate)
        {
            //var maxDateUnix = (long?)DateTimeToUnixTimestamp(barsModel.MaxDate);
            var maxDateUnix = runDate;
            var test1 = runDate;
            var runDateUnix = test1;
            var datesList = bars.Select(e => e.datetime).ToList();
            if (datesList.Contains(maxDateUnix))
            {
                var candle = bars.FirstOrDefault(e => e.datetime == maxDateUnix);
                bars.Remove(candle);
                //var datemax = datesList.Max();
                //var datemin = datesList.Min();
                //var maxdate = new DateTime((long)datemax);
                //var mindate = new DateTime((long)datemin);
                //if (maxdate == barsModel.MaxDate || mindate == barsModel.MaxDate)
                //{
                //}
            }
            if (datesList.Contains(runDateUnix))
            {
                var candle = bars.FirstOrDefault(e => e.datetime == runDateUnix);
                bars.Remove(candle);
            }
            return bars;
        }

        public override void SetDailyTaskParameters()
        {

        }
        public static IEnumerable<object> GetPropertyValues<T>(T input)
        {
            return input.GetType()
                .GetProperties()
                .Select(p => p.GetValue(input));
        }
        public string BuildQuotesUrl(List<string> symbols)
        {
            var urlBase = $"https://api.tdameritrade.com/v1/marketdata/quotes?apikey={TDAmeritradeApiKey}&symbol=";
            string urlSymbols = "";

            foreach(var symbol in symbols)
            {
                //AMD%2CXELA
                urlSymbols = urlSymbols + (symbol + "%2C");
            }
            return urlBase + urlSymbols;
        }
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
        }
        public string BuildPriceHistoryUrl(DateTime startDate, DateTime endDate, string symbol)
        {
            var Date = DateTimeToUnixTimestamp(startDate);
            var Date2 = DateTimeToUnixTimestamp(endDate);
            var urlBase = $"https://api.tdameritrade.com/v1/marketdata/{symbol}/pricehistory?apikey={TDAmeritradeApiKey}&periodType=month&period=1&frequencyType=daily&frequency=1&endDate={(long)Date2}&startDate={(long)Date}&needExtendedHoursData=false";
            return urlBase;
        }

        public async Task CaptureDailyCandles()
        {
            var repo = new ByteTraderRepository();
            var index = await repo.QueryAvailableSymbols();
            var test = index.Where(e => e.SymbolId < 5).ToList();
            var apiUrl = BuildQuotesUrl(test.Select(e => e.Symbol).ToList());
            var apiCall = new BaseApiCall(apiUrl);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            var response = await apiCall.CallApi();
            var quotes = apiCall.SetResponseObject<Dictionary<string, Quotes>>();


            foreach(var item in (Dictionary<string, Quotes>)quotes)
            {
                try
                {
                    var quote = item.Value;
                    var candleList = new List<candles>();
                    var candle = new candles();

                    candle.close = quote.closePrice;
                    //candle.datetime = quote.quoteTimeInLong;
                    candle.high = quote.highPrice;
                    candle.low = quote.lowPrice;
                    candle.open = quote.openPrice;
                    candle.volume = quote.totalVolume;
                    candleList.Add(candle);
                    var x = index.Where(e => e.Symbol == item.Key).ToList();
                    repo.InsertHistoricalCandles(candleList, item.Key, x[0].SymbolId);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                }
            }



        //if (apiCall.IsResponseValid == true)
        //{
        //    var symbolId = await repo.QuerySymbolId(item.Symbol);
        //    repo.InsertHistoricalCandles(apiCall.ResponseObject.candles, item.Symbol, symbolId);
        //    await repo.SetAssetFlag(item.Symbol, "Y");
        //}
        //else if (apiCall.IsResponseValid == false)
        //{
        //    await repo.SetAssetFlag(item.Symbol, "N");
        //}





        }
        public async void CaptureHistoricalDailyCandles()
        {
            var repo = new ByteTraderRepository();

            var tableRows = await repo.QuerySymbols();

            var startDate = new DateTime(2016, 1, 1);
            var endDate = DateTime.Now;
            //var TestUrl = BuildPriceHistoryUrl(startDate, endDate, "ABGI");
            //var apiCall2 = new BaseApiCall(TestUrl);
            //Thread.Sleep(TimeSpan.FromSeconds(1));
            //var response2 = await apiCall2.CallApi();
            //apiCall2.SetResponseObject<CandleList>();

            foreach (var item in tableRows)
            {
                var apiUrl = BuildPriceHistoryUrl(startDate, endDate, item.Symbol);
                var apiCall = new BaseApiCall(apiUrl);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                var response = await apiCall.CallApi();
                //apiCall.SetResponseObject<CandleList>();

                if (apiCall.IsResponseValid == true)
                {
                    var symbolId = await repo.QuerySymbolId(item.Symbol);
                    repo.InsertHistoricalCandles(apiCall.ResponseObject.candles, item.Symbol, symbolId);
                    await repo.SetAssetFlag(item.Symbol, "Y");
                }
                else if (apiCall.IsResponseValid == false)
                {
                    await repo .SetAssetFlag(item.Symbol, "N");
                }
            }
        }
        public async Task AmeritradeFundamentalData()
        {
            Logger.Info("Starting TDA Fundamental Data Ingestion...");
            DateTime captureDate = DateTime.Now.AddDays(-1).Date;
            var stockList = await Repo.QueryAvailableSymbols();
            var apiKey = await Repo.GetSystemDefault("TDA Api Key");
            var accessToken = await Repo.GetSystemDefault("TDA Access Token");
            var accessTokenObj = JsonConvert.DeserializeObject<AccessToken>(accessToken.AttributeValue);           
            ApiWrapper.InitializeApiWrapper(accessTokenObj, apiKey.AttributeValue);
            var task = Task.Run(() => ApiWrapper.PollRefreshToken());
            foreach (var item in stockList)
            {
                try
                {
                    var data = await ApiWrapper.GetFundamentalData(item.Symbol);
                    if(data != null)
                    {
                        await Repo.InsertDailyFundamentalData(MapFundamentalData(data, item.SymbolId, captureDate));
                        Logger.Info($"Captured Daily Fundamental: {data.Symbol}");
                    }
                }
                catch(Exception exc)
                {
                    Logger.Info(exc.ToString());
                }
                Thread.Sleep(TimeSpan.FromSeconds(0.57));
            }
            ApiWrapper.KeepApiActive = false;
        }

        public DailyFundamentalData MapFundamentalData(Fundamental data, int symbolId, DateTime date)
        {
            var table = new DailyFundamentalData
            {
                SymbolId = symbolId,
                DateTimeKey = date,
                Symbol = data.Symbol,
                ReturnOnInvestment = Convert.ToDecimal(data.ReturnOnInvestment),
                QuickRatio = Convert.ToDecimal(data.QuickRatio),
                CurrentRatio = Convert.ToDecimal(data.CurrentRatio),
                InterestCoverage = Convert.ToDecimal(data.InterestCoverage),
                TotalDebtToCapital = Convert.ToDecimal(data.TotalDebtToCapital),
                LtDebtToEquity = Convert.ToDecimal(data.LtDebtToEquity),
                TotalDebtToEquity = Convert.ToDecimal(data.TotalDebtToEquity),
                EpsTTM = Convert.ToDecimal(data.EpsTTM),
                EpsChangePercentTTM = Convert.ToDecimal(data.EpsChangePercentTTM),
                EpsChangeYear = Convert.ToDecimal(data.EpsChangeYear),
                EpsChange = Convert.ToDecimal(data.EpsChange),
                RevChangeYear = Convert.ToDecimal(data.RevChangeYear),
                RevChangeTTM = Convert.ToDecimal(data.RevChangeTTM),
                RevChangeIn = Convert.ToDecimal(data.RevChangeIn),
                SharesOutstanding = Convert.ToDecimal(data.SharesOutstanding),
                MarketCapFloat = Convert.ToDecimal(data.MarketCapFloat),
                MarketCap = Convert.ToDecimal(data.MarketCap),
                BookValuePerShare = Convert.ToDecimal(data.BookValuePerShare),
                ShortIntToFloat = Convert.ToDecimal(data.ShortIntToFloat),
                ShortIntDayToCover = Convert.ToDecimal(data.ShortIntDayToCover),
                DivGrowthRate3Year = Convert.ToDecimal(data.DivGrowthRate3Year),
                DividendPayAmount = Convert.ToDecimal(data.DividendPayAmount),
                DividendPayDate = data.DividendPayDate,
                Beta = Convert.ToDecimal(data.Beta),
                Vol1DayAvg = Convert.ToDecimal(data.Vol1DayAvg),
                ReturnOnAssets = Convert.ToDecimal(data.ReturnOnAssets),
                ReturnOnEquity = Convert.ToDecimal(data.ReturnOnEquity),
                OperatingMarginMRQ = Convert.ToDecimal(data.OperatingMarginMRQ),
                OperatingMarginTTM = Convert.ToDecimal(data.OperatingMarginTTM),
                High52 = Convert.ToDecimal(data.High52),
                Vol10DayAvg = Convert.ToDecimal(data.Vol10DayAvg),
                DividendAmount = Convert.ToDecimal(data.DividendAmount),
                DividendYield = Convert.ToDecimal(data.DividendYield),
                DividendDate = data.DividendDate,
                PeRatio = Convert.ToDecimal(data.PeRatio),
                Low52 = Convert.ToDecimal(data.Low52),
                PbRatio = Convert.ToDecimal(data.PbRatio),
                PegRatio = Convert.ToDecimal(data.PegRatio),
                NetProfitMarginMRQ = Convert.ToDecimal(data.NetProfitMarginMRQ),
                NetProfitMarginTTM = Convert.ToDecimal(data.NetProfitMarginTTM),
                Vol3MonthAvg = Convert.ToDecimal(data.Vol3MonthAvg),
                GrossMarginTTM = Convert.ToDecimal(data.GrossMarginTTM),
                PcfRatio = Convert.ToDecimal(data.PcfRatio),
                PrRatio = Convert.ToDecimal(data.PrRatio),
                GrossMarginMRQ = Convert.ToDecimal(data.GrossMarginMRQ)
            };
            return table;
        }
        public async Task AlpacaDataIngestion()
        {
            Logger.Info("Starting Alpaca End of Day Candle Ingestion...");
            var runDate = DateTime.Now;
            var tableRows = await Repo.QueryAllSymbols();
            var client2 = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));
            var asset = new AssetsRequest { AssetStatus = AssetStatus.Active };
            var assetList = client2.ListAssetsAsync(asset).Result.ToList();
            var symbolList = tableRows.Select(e => e.Symbol).ToList();
            var endDate = DateTime.Now.AddHours(-1);
            var client5 = Alpaca.Markets.Environments.Live.GetAlpacaDataClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));
            var assetsToLoad = new List<IAsset>();
            var dailyCandlesToLoad = new List<IAsset>();
            foreach (var stock in assetList)
            {
                if (symbolList.Contains(stock.Symbol))
                {
                    dailyCandlesToLoad.Add(stock);
                }
                else
                {
                    assetsToLoad.Add(stock);
                }
            }
            var tickers2 = await Repo.QueryNightlyBars();
            var processList = new List<NightlyBarsModel>();
            foreach (var item in tickers2)
            {
                if (item.MaxDate < runDate.Date && DateTime.Now.TimeOfDay > new TimeSpan(17, 0, 0))
                {
                    processList.Add(item);
                }
            }
            var processSymbols = processList.Select(e => e.Symbol).ToList();
            foreach (var item in assetsToLoad)
            {
                if(DateTime.Now.TimeOfDay > new TimeSpan(17, 0, 0))
                {
                    SymbolOnboarding(item, client5);
                    Thread.Sleep(TimeSpan.FromSeconds(0.35));
                }
            }       
            foreach (var item in dailyCandlesToLoad)
            {
                if (processSymbols.Contains(item.Symbol))
                {
                    ProcessDailyCandlesAlpaca(endDate, item, client5, tickers2);
                    Thread.Sleep(TimeSpan.FromSeconds(0.35));
                }
            }
        }
        public async void ProcessTestingMethod()
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-7);
            

            var client22 = Alpaca.Markets.Environments.Live.GetAlpacaDataStreamingClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));
            var client2234 = Alpaca.Markets.Environments.Live.GetAlpacaDataClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));

            var barstest = client2234.ListHistoricalBarsAsync(new HistoricalBarsRequest("AMD", startDate, endDate.AddMinutes(-20), BarTimeFrame.Day)).Result;

            var xxxx = client22.GetMinuteBarSubscription();

            var testList = new List<string>{ "AAPL", "AMD" };


            var testing = client2234.GetSnapshotsAsync(testList).Result;
            //var endDate = DateTime.Now;
            //var startDate = endDate.AddDays(-7);
            var test555 = client2234.ListHistoricalBarsAsync(new HistoricalBarsRequest("AAPL", DateTime.Now.AddMinutes(-25), DateTime.Now.AddMinutes(-20), BarTimeFrame.Minute)).Result;

            var xxxx2 = client22.GetMinuteBarSubscription("TSLA");

            
            var client = Alpaca.Markets.Environments.Paper
    .GetAlpacaDataClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));

            var test666 = client.ListHistoricalBarsAsync(new HistoricalBarsRequest("AAPL", DateTime.Now.AddMinutes(-5), DateTime.Now, BarTimeFrame.Minute)).Result;

            await CaptureDailyCandles();

            //CaptureHistoricalDailyCandles();
            var repo = new ByteTraderRepository();

            var tableRows = await repo.QuerySymbols();

            var client2 = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));
            //var client2 = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));
            var asset = new AssetsRequest { AssetStatus = AssetStatus.Active };

            var x = client2.ListAssetsAsync(asset).Result.ToList();

            foreach(var stock in x)
            {
                await repo.InsertStock(stock.Symbol, stock.Name);
            }

            // First, open the API connection
            var client5 = Alpaca.Markets.Environments.Paper
                .GetAlpacaDataClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));

            var into = DateTime.Today;
            var from = into.AddDays(-5);
            // Get daily price data for AAPL over the last 5 trading days.
            var bars = await client.ListHistoricalBarsAsync(
                new HistoricalBarsRequest("AAPL", from, into, BarTimeFrame.Day));

            // See how much AAPL moved in that timeframe.
            var startPrice = bars.Items.First().Open;
            var endPrice = bars.Items.Last().Close;

            var percentChange = (endPrice - startPrice) / startPrice;
            Console.WriteLine($"AAPL moved {percentChange:P} over the last 5 days.");

            Console.Read();

            var url = @"";

            ///var url = @"https://api.tdameritrade.com/v1/marketdata/quotes?apikey=########&symbol=AMD%2CXELA";
            using (var httpClient = new HttpClient())
            {
                var input = new Quotes();            
                var response = httpClient.GetStringAsync(new Uri(url)).Result;
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, Quotes>>(response);
                var test = JsonConvert.DeserializeObject(response);
            }
  
        }
        public async void CheckAssetAvailability(IAsset stock, IAlpacaDataClient client5)
        {
            var endDate = DateTime.Now.AddMinutes(-20);
            var startDate = endDate.AddDays(-7);
            IPage<IBar> bars;
            try
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.6));
                bars = client5.ListHistoricalBarsAsync(new HistoricalBarsRequest(stock.Symbol, startDate, endDate, BarTimeFrame.Day)).Result;
                var test1 = bars.Items;
                var test2 = test1.Count;
            }
            catch (Exception exc)
            {
                bars = null;
            }
            List<IBar> barList;
            if (bars == null)
            {
                barList = new List<IBar>();
            }
            else
            {
                barList = bars.Items.ToList();
            }

            if (barList.Count == 0)
            {
                await Repo.DiscontinueAsset(stock.Symbol, "Y");
                Logger.Info($"Asset: {stock.Symbol} Marked as Discontinued.");
            }
        }
        public async void ProcessDailyCandlesAlpaca(DateTime endDate, IAsset stock, IAlpacaDataClient client5, List<NightlyBarsModel> tickers)
        {
            var test = tickers.FirstOrDefault(e => e.Symbol == stock.Symbol);
            if(test == null)
            {
                SymbolOnboarding(stock, client5);
            }
            else
            {
                IPage<IBar> bars;
                try
                {
                    bars = client5.ListHistoricalBarsAsync(new HistoricalBarsRequest(stock.Symbol, test.MaxDate, endDate, BarTimeFrame.Day)).Result;
                    var test1 = bars.Items;
                    var test2 = test1.Count;
                }
                catch (Exception exc)
                {
                    Logger.Info(exc.ToString());
                    bars = null;
                }
                List<IBar> barList;
                if (bars == null)
                {
                    barList = new List<IBar>();
                }
                else
                {
                    barList = bars.Items.ToList();
                }

                if (barList.Count == 0)
                {
                    CheckAssetAvailability(stock, client5);
                }
                else
                {
                    var candleList = new List<candles>();
                    foreach (var item in barList)
                    {
                        var candle = new candles
                        {
                            close = item.Close.ToString(),
                            datetime = item.TimeUtc,
                            high = item.High.ToString(),
                            low = item.Low.ToString(),
                            open = item.Open.ToString(),
                            volume = item.Volume.ToString()
                        };
                        candleList.Add(candle);
                    }
                    var filteredBars = FilterOutput(candleList, test, test.MaxDate);
                    Repo.InsertHistoricalCandles(filteredBars, stock.Symbol, test.SymbolId);
                    Logger.Info($"{stock.Symbol} Captured {filteredBars.Count} New Records");
                }
            }
        }
        public async void SymbolOnboarding(IAsset stock, IAlpacaDataClient client5)
        {
            Logger.Info($"Onboarding Symbol: {stock.Symbol}");
            var startDate = new DateTime(2018, 6, 1);
            var endDate = DateTime.Now.AddHours(-1);
            //onboard new Symbol
            await Repo.InsertStock(stock.Symbol, stock.Name);
            IPage<IBar> bars;
            try
            {
                bars = client5.ListHistoricalBarsAsync(new HistoricalBarsRequest(stock.Symbol, startDate, endDate, BarTimeFrame.Day)).Result;
                var test1 = bars.Items;
                var test2 = test1.Count;
            }
            catch (Exception exc)
            {
                bars = null;
            }
            List<IBar> barList;
            if(bars == null)
            {
                barList = new List<IBar>();
            }
            else
            {
                barList = bars.Items.ToList();
            }

            if (barList.Count == 0)
            {       
                await Repo.SetAssetFlag(stock.Symbol, "N");
                Logger.Info($"Data Not Found. Symbol: {stock.Symbol} Flagged Unavialable.");
            }
            else
            {
                var symbolId = await Repo.QuerySymbolId(stock.Symbol);

                var candleList = new List<candles>();
                foreach (var item in barList)
                {
                    var candle = new candles
                    {
                        close = item.Close.ToString(),
                        datetime = item.TimeUtc,
                        high = item.High.ToString(),
                        low = item.Low.ToString(),
                        open = item.Open.ToString(),
                        volume = item.Volume.ToString()
                    };
                    candleList.Add(candle);
                }
                Repo.InsertHistoricalCandles(candleList, stock.Symbol, symbolId);
                await Repo.SetAssetFlag(stock.Symbol, "Y");
                await Repo.SetCaptureDate(stock.Symbol);
                Logger.Info($"Symbol: {stock.Symbol} Initialized With {candleList.Count} Records.");
            }
        }
    }
}
