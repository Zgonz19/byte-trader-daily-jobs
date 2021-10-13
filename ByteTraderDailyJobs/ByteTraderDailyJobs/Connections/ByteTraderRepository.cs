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
        //public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        //public async Task<SymbolIndex> GetSystemDefault(string attributeName)
        //{
        //    var parameters = new DynamicParameters();
        //    parameters.Add("@AttributeName", attributeName);
        //    SymbolIndex index;
        //    var sqlQuery = @"SELECT * FROM SystemDefaults WHERE AttributeName = @AttributeName;";
        //    try
        //    {
        //        using (IDbConnection cn = Connection)
        //        {
        //            cn.Open();
        //            var result = cn.QueryAsync<SymbolIndex>(sqlQuery, parameters).GetAwaiter().GetResult();
        //            cn.Close();
        //            index = result.FirstOrDefault();
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        index = null;
        //        Logger.Info(exc.ToString());
        //    }

        //    return index;
        //}
        public async Task<List<NightlyBarsModel>> QueryNightlyBars()
        {
            List<NightlyBarsModel> nighlyBars;
            var sqlQuery = @"SELECT DISTINCT (DHPD.SymbolId), MAX(DHPD.[DateTime]) AS MaxDate, RSI.Symbol  
                                FROM [HistoricalDailyCandles] DHPD, [SymbolIndex] RSI 
                                WHERE (DHPD.SymbolId = RSI.SymbolId) 
                                GROUP BY RSI.Symbol, DHPD.SymbolId;";
            using (IDbConnection cn = Connection)
            {
                cn.Open();
                var result = cn.QueryAsync<NightlyBarsModel>(sqlQuery).Result;
                cn.Close();
                nighlyBars = result.ToList();
            }
            return nighlyBars;
        }
        public async void InsertHistoricalCandles(List<candles> candles, string symbol, int symbolId)
        {
            await BulkCandleInsert(candles, symbolId, symbol);
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
                        Console.WriteLine(exc.ToString());
                    }

                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
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
                        Console.WriteLine(exc.ToString());
                    }

                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
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
            var sqlQuery = "select * from dbo.SymbolIndex where IsAssetAvailable = 'Y'";
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
            table.Columns.Add("MarketDate", typeof(string));
            table.Columns.Add("Open", typeof(string));
            table.Columns.Add("High", typeof(string));
            table.Columns.Add("Low", typeof(string));
            table.Columns.Add("Close", typeof(string));
            table.Columns.Add("Volume", typeof(string));
            table.Columns.Add("DateTime", typeof(DateTime));
            //table.Columns.Add("AdjustedClose", typeof(decimal));
            foreach (var candle in data)
            {
                var dateStamp = GenerateDateString(ConvertUnixTimeStamp(candle.datetime));


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
                    ConvertUnixTimeStamp(candle.datetime),
                    //adjustedClose
                };
                table.Rows.Add(row);
            }
            return table;
        }

        public async Task<List<HistoricalDailyCandles>> LoadPercentChangeList(int SymbolId)
        {
            List<HistoricalDailyCandles> stockSymbols;
            var parameters = new DynamicParameters();
            parameters.Add("@SymbolId", SymbolId);
            var sqlQuery = "SELECT * FROM DailyHistoricalPriceData WHERE SymbolId = @SymbolId AND MarketDate >= (SELECT MAX(MarketDate) FROM PercentChangeData WHERE SymbolId = @SymbolId);";
            using (IDbConnection cn = Connection)
            {
                cn.Open();
                var result = await cn.QueryAsync<HistoricalDailyCandles>(sqlQuery, parameters);
                cn.Close();
                stockSymbols = result.ToList();
            }
            return stockSymbols;
        }

        //public DataTable ListToDataTable(List<candles> data, int symbolId)
        //{
        //    var table = new DataTable();
        //    table.Columns.Add("SymbolId", typeof(int));
        //    table.Columns.Add("MarketDate", typeof(DateTime));
        //    table.Columns.Add("Open", typeof(decimal));
        //    table.Columns.Add("High", typeof(decimal));
        //    table.Columns.Add("Low", typeof(decimal));
        //    table.Columns.Add("Close", typeof(decimal));
        //    table.Columns.Add("Volume", typeof(long));
        //    table.Columns.Add("AdjustedClose", typeof(decimal));
        //    foreach (var candle in data)
        //    {
        //        var row = new Object[]
        //        {
        //            symbolId,
        //            candle.DateTime,
        //            candle.Open,
        //            candle.High,
        //            candle.Low,
        //            candle.Close,
        //            candle.Volume,
        //            candle.AdjustedClose
        //        };
        //        table.Rows.Add(row);
        //    }
        //    return table;
        //}

        public DataTable ListPercentChangeToDataTable(List<PercentChangeData> data)
        {
            var table = new DataTable();
            table.Columns.Add("SymbolId", typeof(int));
            table.Columns.Add("MarketDate", typeof(DateTime));
            table.Columns.Add("PastDate", typeof(DateTime));
            table.Columns.Add("PercentChange", typeof(decimal));
            table.Columns.Add("AbsoluteChange", typeof(decimal));
            foreach (var item in data)
            {
                var row = new Object[]
                {
                    item.SymbolId,
                    item.MarketDate,
                    item.PreviousMarketDate,
                    item.PercentChange,
                    item.AbsoluteChange
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
                        sqlbc.ColumnMappings.Add("MarketDate", "MarketDate");
                        sqlbc.ColumnMappings.Add("PastDate", "PastDate");
                        sqlbc.ColumnMappings.Add("PercentChange", "PercentChange");
                        sqlbc.ColumnMappings.Add("AbsoluteChange", "AbsoluteChange");
                        await sqlbc.WriteToServerAsync(dataTable);
                    }
                    sqlConn.Close();
                }
            }
            catch (Exception exc)
            {
                //Logger.Info(exc.ToString());
            }
        }





    }

    public class NightlyBarsModel
    {
        public int SymbolId { get; set; }
        public DateTime MaxDate { get; set; }
        public string Symbol { get; set; }
    }
}
