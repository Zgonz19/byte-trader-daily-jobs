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
            //await DailyPercentChange();
        }

        public async Task InitializePercentChange()
        {
            var stockList = await BR.QueryAvailableSymbols();




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
                    var percentChange = new PercentChange(ListToProcess, processDate);
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
