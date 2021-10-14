using ByteTraderDailyJobs.Connections;
using ByteTraderDailyJobs.SubProcessBase.DailyDataProcess;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class DailyDataProcessing : ProcessBaseConfig
    {
        public ByteTraderRepository BR = new ByteTraderRepository();
        public DailyDataProcessing()
        {


        }
        public override async void ExecuteProcess()
        {
            //await ProcessDailyChange();
            await InitializePercentChange();
        }

        public async Task InitializePercentChange()
        {
            var stockList = await BR.QueryAvailableSymbols();
            var symbolsInPC = await BR.SymbolsInPercentChangeTable();

            //var processList = new Dictionary<int, Tables.SymbolIndex>();


            foreach (var stock in stockList)
            {
                if (!symbolsInPC.Contains(stock.SymbolId))
                {
                    var endDate = DateTime.Now;
                    var beginDate = endDate.AddDays(-40);
                    var candleList = await BR.QueryCandlesByDate(stock.SymbolId, beginDate, endDate);
                    var percentChange = new PercentChange(candleList);
                    percentChange.CalculateChange();
                    await BR.BulkPercentChangeInsert(percentChange.CalculatedData);
                }
            }



        }

        public async Task ProcessDailyChange()
        {
            try
            {
                var processDate = DateTime.Now;
                var stockList = await BR.QueryAvailableSymbols();
                foreach (var stock in stockList)
                {
                    var ListToProcess = await BR.LoadPercentChangeList(stock.SymbolId);
                    var percentChange = new PercentChange(ListToProcess);
                    percentChange.CalculateChange();
                    //var result = percentChange.CalculatedData;
                    await BR.BulkPercentChangeInsert(percentChange.CalculatedData);
                }
            }
            catch (Exception Exc)
            {
                //Logger.Info(Exc.ToString());
            }
        }

    }
}
