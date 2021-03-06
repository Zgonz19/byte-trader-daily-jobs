using ByteTraderDailyJobs.DataObjects;
using ByteTraderDailyJobs.Tables;
using Dapper;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderDailyJobs.Connections
{
    public class ByteTraderRepository : DbContext
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        public async Task<List<AppUsers>> GetAppUsers()
        {
            List<AppUsers> appUsers;
            var sqlQuery = "SELECT * FROM AppUsers";
            using (IDbConnection cn = Connection)
            {
                cn.Open();
                var result = cn.QueryAsync<AppUsers>(sqlQuery).GetAwaiter().GetResult();
                cn.Close();
                appUsers = result.ToList();
            }
            return appUsers;
        }
        public async Task<SystemDefaults> GetSystemDefault(string attributeName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AttributeName", attributeName);
            SystemDefaults index;
            var sqlQuery = @"SELECT * FROM SystemDefaults WHERE AttributeName = @AttributeName;";
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<SystemDefaults>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    index = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                index = null;
                Logger.Info(exc.ToString());
            }

            return index;
        }
        public async Task<List<NightlyBarsModel>> QueryNightlyBars()
        {
            List<NightlyBarsModel> nighlyBars;
            var sqlQuery = @"SELECT DISTINCT (DHPD.SymbolId), MAX(DHPD.[DateTime]) AS MaxDate, RSI.Symbol,  Max(DHPD.MarketDate) As MarketDate
                                FROM [HistoricalDailyCandles] DHPD, [SymbolIndex] RSI 
                                WHERE (DHPD.SymbolId = RSI.SymbolId) 
                                GROUP BY RSI.Symbol, DHPD.SymbolId;";
            using (IDbConnection cn = Connection)
            {
                cn.Open();
                var result = cn.QueryAsync<NightlyBarsModel>(sqlQuery, commandTimeout: 200000).Result;
                cn.Close();
                nighlyBars = result.ToList();
            }
            return nighlyBars;
        }
        public async void InsertHistoricalCandles(List<candles> candles, string symbol, int symbolId)
        {
            await BulkCandleInsert(candles, symbolId, symbol);
        }
        public async Task SetCaptureDate(string symbol)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Symbol", symbol);
                parameters.Add("@CaptureDate", DateTime.Now);

                var sqlCommand = $"UPDATE dbo.SymbolIndex SET CaptureDate = @CaptureDate " +
                    $"WHERE Symbol = @Symbol;";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }

                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task<List<ChangeDataReport>> LoadTopDailyLosers(DateTime marketDate)
        {
            List<ChangeDataReport> stockSymbols;
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MarketDate", marketDate);
                var sqlQuery = @"SELECT TOP(250) RSI.Id, RSI.Symbol, PCD.PercentChange, RSI.CompanyName, PCD.AbsoluteChange, PCD.MarketDate 
                         FROM RootSymbolIndex RSI, PercentChangeData PCD 
                         WHERE PCD.MarketDate = @MarketDate AND PCD.SymbolId = RSI.ID 
                         Order By PercentChange Asc;";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<ChangeDataReport>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    stockSymbols = result.ToList();
                }
                return stockSymbols;
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                stockSymbols = new List<ChangeDataReport>();
                return stockSymbols;
            }
        }
        public async Task<List<ChangeDataReport>> LoadTopDailyGainers(DateTime marketDate)
        {
            List<ChangeDataReport> stockSymbols;
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MarketDate", marketDate);
                var sqlQuery = @"SELECT TOP(250) RSI.Id, RSI.Symbol, PCD.PercentChange, RSI.CompanyName, PCD.AbsoluteChange, PCD.MarketDate 
                         FROM RootSymbolIndex RSI, PercentChangeData PCD 
                         WHERE PCD.MarketDate = @MarketDate AND PCD.SymbolId = RSI.ID 
                         Order By PercentChange Desc;";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<ChangeDataReport>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    stockSymbols = result.ToList();
                }
                return stockSymbols;
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                stockSymbols = new List<ChangeDataReport>();
                return stockSymbols;
            }
        }
        public async Task DiscontinueAsset(string symbol, string flag)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Symbol", symbol);
                parameters.Add("@IsAssetDiscontinued", flag);
                parameters.Add("@DiscontinuedDate", DateTime.Now);

                var sqlCommand = $"UPDATE dbo.SymbolIndex SET IsAssetDiscontinued = @IsAssetDiscontinued, DiscontinuedDate = @DiscontinuedDate " +
                    $"WHERE Symbol = @Symbol;";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task SetAssetFlag(string symbol, string flag)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Symbol", symbol);
                parameters.Add("@IsAssetAvailable", flag);
                
                var sqlCommand = $"UPDATE dbo.SymbolIndex SET IsAssetAvailable = @IsAssetAvailable " +
                    $"WHERE Symbol = @Symbol;";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }

                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task<int> QuerySymbolId(string symbol)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Symbol", symbol);
            int symbolId;
            var sqlQuery = $"select SymbolId from dbo.SymbolIndex where Symbol = '{symbol}';";
            using (IDbConnection cn = Connection)
            {
                try
                {
                    cn.Open();
                    var result = cn.QueryAsync<int>(sqlQuery).Result;
                    cn.Close();
                    symbolId = result.First();
                }
                catch (Exception exc)
                {
                    symbolId = 0;
                }

            }
            return symbolId;
        }
        public async Task InsertDailyFundamentalData(DailyFundamentalData data)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SymbolId", data.SymbolId);
                parameters.Add("@DateTimeKey", data.DateTimeKey);
                parameters.Add("@Symbol", data.Symbol);
                parameters.Add("@ReturnOnInvestment", data.ReturnOnInvestment);
                parameters.Add("@QuickRatio", data.QuickRatio);
                parameters.Add("@CurrentRatio", data.CurrentRatio);
                parameters.Add("@InterestCoverage", data.InterestCoverage);
                parameters.Add("@TotalDebtToCapital", data.TotalDebtToCapital);
                parameters.Add("@LtDebtToEquity", data.LtDebtToEquity);
                parameters.Add("@TotalDebtToEquity", data.TotalDebtToEquity);
                parameters.Add("@EpsTTM", data.EpsTTM);
                parameters.Add("@EpsChangePercentTTM", data.EpsChangePercentTTM);
                parameters.Add("@EpsChangeYear", data.EpsChangeYear);
                parameters.Add("@EpsChange", data.EpsChange);
                parameters.Add("@RevChangeYear", data.RevChangeYear);
                parameters.Add("@RevChangeTTM", data.RevChangeTTM);
                parameters.Add("@RevChangeIn", data.RevChangeIn);
                parameters.Add("@SharesOutstanding", data.SharesOutstanding);
                parameters.Add("@MarketCapFloat", data.MarketCapFloat);
                parameters.Add("@MarketCap", data.MarketCap);
                parameters.Add("@BookValuePerShare", data.BookValuePerShare);
                parameters.Add("@ShortIntToFloat", data.ShortIntToFloat);
                parameters.Add("@ShortIntDayToCover", data.ShortIntDayToCover);
                parameters.Add("@DivGrowthRate3Year", data.DivGrowthRate3Year);
                parameters.Add("@DividendPayAmount", data.DividendPayAmount);
                parameters.Add("@DividendPayDate", data.DividendPayDate);
                parameters.Add("@Beta", data.Beta);
                parameters.Add("@Vol1DayAvg", data.Vol1DayAvg);
                parameters.Add("@ReturnOnAssets", data.ReturnOnAssets);
                parameters.Add("@ReturnOnEquity", data.ReturnOnEquity);
                parameters.Add("@OperatingMarginMRQ", data.OperatingMarginMRQ);
                parameters.Add("@OperatingMarginTTM", data.OperatingMarginTTM);
                parameters.Add("@High52", data.High52);
                parameters.Add("@Vol10DayAvg", data.Vol10DayAvg);
                parameters.Add("@DividendAmount", data.DividendAmount);
                parameters.Add("@DividendYield", data.DividendYield);
                parameters.Add("@DividendDate", data.DividendDate);
                parameters.Add("@PeRatio", data.PeRatio);
                parameters.Add("@Low52", data.Low52);
                parameters.Add("@PbRatio", data.PbRatio);
                parameters.Add("@PegRatio", data.PegRatio);
                parameters.Add("@NetProfitMarginMRQ", data.NetProfitMarginMRQ);
                parameters.Add("@NetProfitMarginTTM", data.NetProfitMarginTTM);
                parameters.Add("@Vol3MonthAvg", data.Vol3MonthAvg);
                parameters.Add("@GrossMarginTTM", data.GrossMarginTTM);
                parameters.Add("@PcfRatio", data.PcfRatio);
                parameters.Add("@PrRatio", data.PrRatio);
                parameters.Add("@GrossMarginMRQ", data.GrossMarginMRQ);
                var sqlCommand = @"INSERT INTO dbo.DailyFundamentalData (
                                        SymbolId,
                                        DateTimeKey,
                                        Symbol,
                                        ReturnOnInvestment,
                                        QuickRatio,
                                        CurrentRatio,
                                        InterestCoverage,
                                        TotalDebtToCapital,
                                        LtDebtToEquity,
                                        TotalDebtToEquity,
                                        EpsTTM,
                                        EpsChangePercentTTM,
                                        EpsChangeYear,
                                        EpsChange,
                                        RevChangeYear,
                                        RevChangeTTM,
                                        RevChangeIn,
                                        SharesOutstanding,
                                        MarketCapFloat,
                                        MarketCap,
                                        BookValuePerShare,
                                        ShortIntToFloat,
                                        ShortIntDayToCover,
                                        DivGrowthRate3Year,
                                        DividendPayAmount,
                                        DividendPayDate,
                                        Beta,
                                        Vol1DayAvg,
                                        ReturnOnAssets,
                                        ReturnOnEquity,
                                        OperatingMarginMRQ,
                                        OperatingMarginTTM,
                                        High52,
                                        Vol10DayAvg,
                                        DividendAmount,
                                        DividendYield,
                                        DividendDate,
                                        PeRatio,
                                        Low52,
                                        PbRatio,
                                        PegRatio,
                                        NetProfitMarginMRQ,
                                        NetProfitMarginTTM,
                                        Vol3MonthAvg,
                                        GrossMarginTTM,
                                        PcfRatio,
                                        PrRatio,
                                        GrossMarginMRQ)" +
                                  @"VALUES (
                                        @SymbolId,
                                        @DateTimeKey,
                                        @Symbol,
                                        @ReturnOnInvestment,
                                        @QuickRatio,
                                        @CurrentRatio,
                                        @InterestCoverage,
                                        @TotalDebtToCapital,
                                        @LtDebtToEquity,
                                        @TotalDebtToEquity,
                                        @EpsTTM,
                                        @EpsChangePercentTTM,
                                        @EpsChangeYear,
                                        @EpsChange,
                                        @RevChangeYear,
                                        @RevChangeTTM,
                                        @RevChangeIn,
                                        @SharesOutstanding,
                                        @MarketCapFloat,
                                        @MarketCap,
                                        @BookValuePerShare,
                                        @ShortIntToFloat,
                                        @ShortIntDayToCover,
                                        @DivGrowthRate3Year,
                                        @DividendPayAmount,
                                        @DividendPayDate,
                                        @Beta,
                                        @Vol1DayAvg,
                                        @ReturnOnAssets,
                                        @ReturnOnEquity,
                                        @OperatingMarginMRQ,
                                        @OperatingMarginTTM,
                                        @High52,
                                        @Vol10DayAvg,
                                        @DividendAmount,
                                        @DividendYield,
                                        @DividendDate,
                                        @PeRatio,
                                        @Low52,
                                        @PbRatio,
                                        @PegRatio,
                                        @NetProfitMarginMRQ,
                                        @NetProfitMarginTTM,
                                        @Vol3MonthAvg,
                                        @GrossMarginTTM,
                                        @PcfRatio,
                                        @PrRatio,
                                        @GrossMarginMRQ);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }

        public async Task<HistoricalDailyCandles> QueryDailyCandles(int SymbolId, string DateString)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SymbolId", SymbolId);
            parameters.Add("@DateString", DateString);
            HistoricalDailyCandles index;
            var sqlQuery = @"SELECT * FROM HistoricalDailyCandles WHERE SymbolId = @SymbolId AND DateString = @DateString;";
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<HistoricalDailyCandles>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    index = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                index = null;
                Logger.Info(exc.ToString());
            }

            return index;
        }
        public async Task<WeeklyVolatility> QueryWeeklyVolatility(int SymbolId, string DateString)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SymbolId", SymbolId);
            parameters.Add("@DateString", DateString);
            WeeklyVolatility index;
            var sqlQuery = @"SELECT * FROM WeeklyVolatility WHERE SymbolId = @SymbolId AND DateString = @DateString;";
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<WeeklyVolatility>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    index = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                index = null;
                Logger.Info(exc.ToString());
            }

            return index;
        }

        public async Task InsertWeeklyVolatility(int SymbolId, string DateString, int CountP, int CountN, decimal HighToLowChange, decimal ChangeOnClose, string ChangeDirection, string OrderedChange, string AbsoluteChange, string UnorderedChange)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SymbolId", SymbolId);
                parameters.Add("@DateString", DateString);                
                parameters.Add("@CountP", CountP);
                parameters.Add("@CountN", CountN);
                parameters.Add("@HighToLowChange", HighToLowChange);
                parameters.Add("@ChangeOnClose", ChangeOnClose);
                parameters.Add("@ChangeDirection", ChangeDirection);
                parameters.Add("@OrderedChange", OrderedChange);
                parameters.Add("@AbsoluteChange", AbsoluteChange);
                parameters.Add("@UnorderedChange", UnorderedChange);

                var sqlCommand = $"INSERT INTO dbo.WeeklyVolatility (SymbolId, DateString, CountP, CountN, HighToLowChange, ChangeOnClose, ChangeDirection, OrderedChange, AbsoluteChange, UnorderedChange)" +
                    $"VALUES (@SymbolId, @DateString, @CountP, @CountN, @HighToLowChange, @ChangeOnClose, @ChangeDirection, @OrderedChange, @AbsoluteChange, @UnorderedChange);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }

        public async Task<AverageTradeVolume> QueryAverageTradeVolume(int SymbolId, string DateString)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SymbolId", SymbolId);
            parameters.Add("@DateString", DateString);
            AverageTradeVolume index;
            var sqlQuery = @"SELECT * FROM AverageTradeVolume WHERE SymbolId = @SymbolId AND DateString = @DateString;";
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<AverageTradeVolume>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    index = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                index = null;
                Logger.Info(exc.ToString());
            }

            return index;
        }




        public async Task InsertAverageVolume(int SymbolId, string DateString, double? Avg10, double? Avg20, double? Avg30)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SymbolId", SymbolId);
                parameters.Add("@DateString", DateString);
                parameters.Add("@Avg10", Avg10);
                parameters.Add("@Avg20", Avg20);
                parameters.Add("@Avg30", Avg30);

                var sqlCommand = $"INSERT INTO dbo.AverageTradeVolume (SymbolId, DateString, Avg10, Avg20, Avg30)" +
                    $"VALUES (@SymbolId, @DateString, @Avg10, @Avg20, @Avg30);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task InsertStock(string Symbol, string Description)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Symbol", Symbol);
                parameters.Add("@Description", Description);

                var sqlCommand = $"INSERT INTO dbo.SymbolIndex (Symbol, Description)" +
                    $"VALUES (@Symbol, @Description);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }

                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task<List<SymbolIndex>> QueryAllSymbols()
        {
            List<SymbolIndex> stockSymbols;
            var sqlQuery = "select * from dbo.SymbolIndex;";
            using (IDbConnection cn = Connection)
            {
                try
                {
                    cn.Open();
                    var result = cn.QueryAsync<SymbolIndex>(sqlQuery).Result;
                    cn.Close();
                    stockSymbols = result.ToList();
                }
                catch (Exception exc)
                {
                    stockSymbols = null;
                }

            }
            return stockSymbols;
        }
        public async Task<List<SymbolIndex>> QuerySymbols()
        {
            List<SymbolIndex> stockSymbols;
            var sqlQuery = "select * from dbo.SymbolIndex where IsAssetAvailable IS NULL";
            using (IDbConnection cn = Connection)
            {
                try
                {
                    cn.Open();
                    var result = cn.QueryAsync<SymbolIndex>(sqlQuery).Result;
                    cn.Close();
                    stockSymbols = result.ToList();
                }
                catch (Exception exc)
                {
                    stockSymbols = null;
                }

            }
            return stockSymbols;
        }
        public async Task<List<SymbolIndex>> QueryAvailableSymbols()
        {
            List<SymbolIndex> stockSymbols;
            var sqlQuery = "select * from dbo.SymbolIndex where IsAssetAvailable = 'Y' AND IsAssetDiscontinued IS NULL ";
            using (IDbConnection cn = Connection)
            {
                try
                {
                    cn.Open();
                    var result = cn.QueryAsync<SymbolIndex>(sqlQuery).Result;
                    cn.Close();
                    stockSymbols = result.ToList();
                }
                catch (Exception exc)
                {
                    stockSymbols = null;
                }

            }
            return stockSymbols;
        }
        public async Task BulkCandleInsert(List<candles> data, int symbolId, string symbol)
        {
            try
            {
                var dataTable = ListBarsToDataTable(data, symbolId, symbol);
                using (SqlConnection sqlConn = SqlConnect)
                {
                    sqlConn.Open();
                    using (SqlBulkCopy sqlbc = new SqlBulkCopy(sqlConn))
                    {
                        sqlbc.DestinationTableName = "dbo.HistoricalDailyCandles";
                        sqlbc.ColumnMappings.Add("SymbolId", "SymbolId");
                        sqlbc.ColumnMappings.Add("DateString", "DateString");
                        sqlbc.ColumnMappings.Add("Symbol", "Symbol");
                        sqlbc.ColumnMappings.Add("MarketDate", "MarketDate");
                        sqlbc.ColumnMappings.Add("Open", "Open");
                        sqlbc.ColumnMappings.Add("High", "High");
                        sqlbc.ColumnMappings.Add("Low", "Low");
                        sqlbc.ColumnMappings.Add("Close", "Close");
                        sqlbc.ColumnMappings.Add("Volume", "Volume");
                        sqlbc.ColumnMappings.Add("DateTime", "DateTime");
                        //sqlbc.ColumnMappings.Add("AdjustedClose", "AdjustedClose");
                        await sqlbc.WriteToServerAsync(dataTable);
                    }
                    sqlConn.Close();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }
        public string GenerateDateString(DateTime date)
        {
            return date.Day.ToString("00") + date.Month.ToString("00") + date.Year;
        }
        public static DateTime ConvertUnixTimeStamp(long? unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((long)unixTimeStamp);
        }
        public dynamic NaNToNull(string t)
        {
            if(t == "NaN" || String.IsNullOrWhiteSpace(t))
            {
                return null;
            }
            else
            {
                try
                {
                    var test = Convert.ToDecimal(t);
                    return t;
                }
                catch (Exception exc)
                {
                    decimal result;
                    if (!decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        return null;
                        // do something in case it fails?
                    }
                    else
                    {
                        return result.ToString();
                    }
                }
            }
        }
        public DataTable ListBarsToDataTable(List<candles> data, int symbolId, string symbol)
        {
            var table = new DataTable();
            table.Columns.Add("SymbolId", typeof(int));
            table.Columns.Add("DateString", typeof(string));
            table.Columns.Add("Symbol", typeof(string));
            table.Columns.Add("MarketDate", typeof(DateTime));
            table.Columns.Add("Open", typeof(string));
            table.Columns.Add("High", typeof(string));
            table.Columns.Add("Low", typeof(string));
            table.Columns.Add("Close", typeof(string));
            table.Columns.Add("Volume", typeof(string));
            table.Columns.Add("DateTime", typeof(DateTime));
            //table.Columns.Add("AdjustedClose", typeof(decimal));
            var dates = data.Select(e => e.datetime).ToList();
            var datestrings = new List<string>();
            foreach (var candle in data)
            {
                var dateStamp = GenerateDateString(candle.datetime);
                var row = new Object[]
                {
                    symbolId,
                    dateStamp,
                    symbol,
                    candle.datetime,
                    NaNToNull(candle.open),
                    NaNToNull(candle.high),
                    NaNToNull(candle.low),
                    NaNToNull(candle.close),
                    NaNToNull(candle.volume),
                    candle.datetime,
                    //adjustedClose
                };
                if (datestrings.Contains(dateStamp))
                {
                    var x = table.Rows;
                    var ttt = x[datestrings.Count - 1];
                    var x2 = ttt[9];
                    var x4 = row[9];
                    if((DateTime)x4 > (DateTime)x2)
                    {
                        table.Rows.Remove(ttt);
                        table.Rows.Add(row);
                    }
                }
                else
                {
                    datestrings.Add(dateStamp);
                    table.Rows.Add(row);
                }
            }
            return table;
        }


        public async Task<List<DailyFundamentalData>> QueryFundamentalByDate(DateTime dateKey)
        {
            List<DailyFundamentalData> stockSymbols;
            var parameters = new DynamicParameters();
            try
            {
                parameters.Add("@DateTimeKey", dateKey);

                var sqlQuery = $"SELECT * FROM DailyFundamentalData WHERE DateTimeKey = @DateTimeKey;";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<DailyFundamentalData>(sqlQuery, parameters).Result;
                    cn.Close();
                    stockSymbols = result.ToList();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                stockSymbols = null;
            }
            return stockSymbols;
        }
        public async Task<List<PercentChangeData>> PercentChangeByDate(int SymbolId, DateTime BeginDate, DateTime EndDate)
        {
            List<PercentChangeData> stockSymbols;
            var parameters = new DynamicParameters();
            try
            {
                parameters.Add("@SymbolId", SymbolId);

                var sqlQuery = $"SELECT * FROM PercentChangeData WHERE SymbolId = @SymbolId AND MarketDate >= '{BeginDate.ToString("yyyy-MM-dd")}' AND MarketDate <= '{EndDate.ToString("yyyy-MM-dd")}';";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<PercentChangeData>(sqlQuery, parameters).Result;
                    cn.Close();
                    stockSymbols = result.ToList();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                stockSymbols = null;
            }
            return stockSymbols;
        }
        public async Task<List<HistoricalDailyCandles>> QueryCandlesByDate(int SymbolId, DateTime BeginDate, DateTime EndDate)
        {
            List<HistoricalDailyCandles> stockSymbols;
            var parameters = new DynamicParameters();
            try
            {
                parameters.Add("@SymbolId", SymbolId);

                var sqlQuery = $"SELECT * FROM HistoricalDailyCandles WHERE SymbolId = @SymbolId AND DateTime >= '{BeginDate.ToString("yyyy-MM-dd")}' AND DateTime <= '{EndDate.ToString("yyyy-MM-dd")}';";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<HistoricalDailyCandles>(sqlQuery, parameters).Result;
                    cn.Close();
                    stockSymbols = result.ToList();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                stockSymbols = null;
            }
            return stockSymbols;
        }
        public async Task<List<int>> SymbolsInPercentChangeTable()
        {
            List<int> stockSymbols;
            var sqlQuery = "SELECT DISTINCT SymbolId FROM PercentChangeData;";
            using (IDbConnection cn = Connection)
            {
                cn.Open();
                var result = cn.QueryAsync<int>(sqlQuery).Result;
                cn.Close();
                stockSymbols = result.ToList();
            }
            return stockSymbols;
        }
        public async Task<List<HistoricalDailyCandles>> LoadPercentChangeList(int SymbolId)
        {
            List<HistoricalDailyCandles> stockSymbols;
            var parameters = new DynamicParameters();
            parameters.Add("@SymbolId", SymbolId);
            var sqlQuery = "SELECT * FROM HistoricalDailyCandles WHERE SymbolId = @SymbolId AND MarketDate >= (SELECT MAX(MarketDate) FROM PercentChangeData WHERE SymbolId = @SymbolId);";
            using (IDbConnection cn = Connection)
            {
                cn.Open();
                var result = await cn.QueryAsync<HistoricalDailyCandles>(sqlQuery, parameters);
                cn.Close();
                stockSymbols = result.ToList();
            }
            return stockSymbols;
        }
        public DataTable ListPercentChangeToDataTable(List<PercentChangeData> data)
        {
            var table = new DataTable();
            table.Columns.Add("SymbolId", typeof(int));
            table.Columns.Add("MarketDateString", typeof(string));
            table.Columns.Add("MarketDate", typeof(DateTime));
            table.Columns.Add("PreviousMarketDate", typeof(DateTime));
            table.Columns.Add("PercentChange", typeof(decimal));
            table.Columns.Add("AbsoluteChange", typeof(decimal));
            table.Columns.Add("VolumePercentChange", typeof(decimal));
            
            foreach (var item in data)
            {
                var dateStamp = GenerateDateString((item.MarketDate));
                var row = new Object[]
                {
                    item.SymbolId,
                    dateStamp,
                    item.MarketDate,
                    item.PreviousMarketDate,
                    item.PercentChange,
                    item.AbsoluteChange,
                    item.VolumePercentChange
                };
                table.Rows.Add(row);
            }
            return table;
        }
        public async Task BulkPercentChangeInsert(List<PercentChangeData> data)
        {
            try
            {
                var dataTable = ListPercentChangeToDataTable(data);
                using (SqlConnection sqlConn = SqlConnect)
                {
                    sqlConn.Open();
                    using (SqlBulkCopy sqlbc = new SqlBulkCopy(sqlConn))
                    {
                        sqlbc.DestinationTableName = "PercentChangeData";
                        sqlbc.ColumnMappings.Add("SymbolId", "SymbolId");
                        sqlbc.ColumnMappings.Add("MarketDateString", "MarketDateString");
                        sqlbc.ColumnMappings.Add("MarketDate", "MarketDate");
                        sqlbc.ColumnMappings.Add("PreviousMarketDate", "PreviousMarketDate");
                        sqlbc.ColumnMappings.Add("PercentChange", "PercentChange");
                        sqlbc.ColumnMappings.Add("AbsoluteChange", "AbsoluteChange");
                        sqlbc.ColumnMappings.Add("VolumePercentChange", "VolumePercentChange");
                        sqlbc.WriteToServer(dataTable);
                    }
                    sqlConn.Close();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
    }

    public class NightlyBarsModel
    {
        public int SymbolId { get; set; }
        public DateTime MaxDate { get; set; }
        public string Symbol { get; set; }
        public string MarketDate { get; set; }
    }

    public class ChangeDataReport
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public decimal PercentChange { get; set; }
        public string CompanyName { get; set; }
        public decimal AbsoluteChange { get; set; }
        public DateTime MarketDate { get; set; }

    }
}
