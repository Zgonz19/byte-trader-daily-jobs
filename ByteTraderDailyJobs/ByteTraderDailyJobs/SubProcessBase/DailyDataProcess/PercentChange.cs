using ByteTraderDailyJobs.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ByteTraderDailyJobs.SubProcessBase.DailyDataProcess
{
    public class PercentChange
    {
        public List<HistoricalDailyCandles> InputList { get; set; }
        //public DateTime ProcessDate { get; set; }
        public List<List<HistoricalDailyCandles>> FilteredInputList { get; set; }

        public List<PercentChangeData> CalculatedData = new List<PercentChangeData>();

        public PercentChange(List<HistoricalDailyCandles> dailyList)
        {
            InputList = dailyList.OrderBy(e => e.DateTime).ToList();
            FilteredInputList = new List<List<HistoricalDailyCandles>>();
            FilteredInputList.Add(InputList);
            //ProcessDate = processDate;
        }

        public void FilterList()
        {
            try
            {
                FilteredInputList = new List<List<HistoricalDailyCandles>>();
                var minDate = InputList.Min(e => e.DateTime);
                var maxDate = InputList.Max(e => e.DateTime);

                var Filter = new List<HistoricalDailyCandles>();
                var startDate = minDate;
                while (startDate <= maxDate)
                {
                    if (!(startDate.DayOfWeek == DayOfWeek.Sunday || startDate.DayOfWeek == DayOfWeek.Saturday))
                    {
                        var test = InputList.Exists(e => e.DateTime.Date == startDate.Date);
                        if (InputList.Exists(e => e.DateTime.Date == startDate.Date))
                        {
                            Filter.Add(InputList.Find(e => e.DateTime.Date == startDate.Date));
                        }
                        else if (Filter.Count == 1)
                        {
                            FilteredInputList[FilteredInputList.Count - 1].Add(Filter[0]);
                            Filter = new List<HistoricalDailyCandles>();
                        }
                        else if (Filter.Count > 1)
                        {
                            FilteredInputList.Add(Filter);
                            Filter = new List<HistoricalDailyCandles>();
                        }
                    }
                    startDate = startDate.AddDays(1);
                }
                if (Filter.Count == 1)
                {
                    //FilteredInputList[FilteredInputList.Count - 1].Add(Filter[0]);
                    Filter = new List<HistoricalDailyCandles>();
                }
                else if (Filter.Count > 1)
                {
                    FilteredInputList.Add(Filter);
                    Filter = new List<HistoricalDailyCandles>();
                }
            }
            catch (Exception exc)
            {
                //getting out of range error
            }


        }
        public List<PercentChangeData> CalculateChange()
        {
            //FilterList();

            ProcessFilteredList();
            return CalculatedData;
        }

        public void ProcessFilteredList()
        {
            try
            {
                if (FilteredInputList.Exists(e => e.Count == 1))
                {
                    var xx = 666;
                }
                var dataRows = new List<PercentChangeData>();
                foreach (var points in FilteredInputList)
                {
                    if (points.Count > 1)
                    {
                        bool skippedFirst = false;
                        foreach (var price in points)
                        {
                            if (!skippedFirst)
                            {
                                skippedFirst = true;
                            }
                            else
                            {
                                var pastPrice = points.ElementAt(points.IndexOf(price) - 1);
                                if (!(pastPrice.Close <= 0))
                                {
                                    var percentChange = new PercentChangeData();
                                    percentChange.MarketDate = price.DateTime;
                                    //percentChange.MarketDateString = ;
                                    percentChange.PreviousMarketDate = pastPrice.DateTime;
                                    percentChange.AbsoluteChange = price.Close - pastPrice.Close;
                                    percentChange.PercentChange = 100 * ((price.Close - pastPrice.Close) / pastPrice.Close);
                                    percentChange.SymbolId = price.SymbolId;
                                    dataRows.Add(percentChange);
                                }
                            }
                        }
                    }
                }
                CalculatedData = dataRows;
            }
            catch (Exception exc)
            {

            }

        }
    }
}
