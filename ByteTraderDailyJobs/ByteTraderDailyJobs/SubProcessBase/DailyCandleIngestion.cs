using System;
using Abbotware.Interop.TDAmeritrade;
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

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class DailyCandleIngestion : ProcessBaseConfig
    {
        public string AlpacaApiKey { get; set; }
        public string AlpacaSecretKey { get; set; }
        public string TDAmeritradeApiKey { get; set; }

        public ByteTraderRepository Repo = new ByteTraderRepository();
        public DailyCandleIngestion()
        {

        }

        public DailyCandleIngestion(string name)
        {

        }
        public override async void ExecuteProcess()
        {
            await PriceHistoryDataUpdate();
        }
        public async Task PriceHistoryDataUpdate()
        {
            var runDate = DateTime.Now;
            var repo = new ByteTraderRepository();
            var tickers = await repo.QueryNightlyBars();
            foreach (var symbol in tickers)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                var urlMaxDate = symbol.MaxDate.AddHours(0);
                var apiUrl = BuildPriceHistoryUrl(urlMaxDate, runDate, symbol.Symbol);
                var apiCall = new BaseApiCall(apiUrl);
                try
                {
                    await apiCall.CallApi();
                    apiCall.SetResponseObject<CandleList>();
                    if (!(apiCall.ResponseObject.candles.Count == 0))
                    {
                        var bars = (List<candles>)apiCall.ResponseObject.candles;
                        var filteredBars = FilterOutput(bars, symbol);
                        repo.InsertHistoricalCandles(filteredBars, symbol.Symbol, symbol.SymbolId);
                    }
                }
                catch (Exception exc)
                {

                }
            }

            List<candles> FilterOutput(List<candles> bars, NightlyBarsModel barsModel)
            {
                var maxDateUnix = (long?)DateTimeToUnixTimestamp(barsModel.MaxDate);
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
                return bars;
            }
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
                    candle.datetime = quote.quoteTimeInLong;
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
                apiCall.SetResponseObject<CandleList>();

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

        //public async Task<Quotes> QuotesAsync()
        //{

        //}
        //public async Task QuotesAsync1()
        //{
        //    await QuotesAsync();
        //}
        //public async Task QuotesAsync2()
        //{
        //    await QuotesAsync();
        //}


        public async void ProcessTestingMethod()
        {

            await CaptureDailyCandles();
            //CaptureHistoricalDailyCandles();
            var repo = new ByteTraderRepository();

            var tableRows = await repo.QuerySymbols();

            var client2 = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(AlpacaApiKey, AlpacaSecretKey));

            var asset = new AssetsRequest { AssetStatus = AssetStatus.Active };

            var x = client2.ListAssetsAsync(asset).Result.ToList();

            foreach(var stock in x)
            {
                await repo.InsertStock(stock.Symbol, stock.Name);
            }

            // First, open the API connection
            var client = Alpaca.Markets.Environments.Paper
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

    }
}
