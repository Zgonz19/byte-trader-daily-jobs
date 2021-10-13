using ByteTraderDailyJobs.DataObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderDailyJobs.SubProcessBase.DailyCandleIngestionProcess
{
    public class BaseApiCall : IBaseApiCall
    {
        public string TargetUrl { get; set; }
        public string ResponseString { get; set; }
        public dynamic ResponseObject { get; set; }
        public bool IsResponseValid { get; set; }

        public BaseApiCall(string targetUrl)
        {
            TargetUrl = targetUrl;
        }



        public async Task<string> CallApi()
        {
            string response;
            using (var httpClient = new HttpClient())
            {
                response = httpClient.GetStringAsync(new Uri(TargetUrl)).Result;
            }
            ResponseString = response;
            return response;
        }

        public virtual dynamic SetResponseObject<T>()
        {
            //var x =  new JsonSerializerSettings
            //{(
            //    Error = (sender, args) =>
            //    {
            //        Reading reading = args.CurrentObject as Reading;

            //        if (reading != null && args.ErrorContext.Member.ToString() == "temperature")
            //        {
            //            reading.Temperature = null;
            //            args.ErrorContext.Handled = true;
            //        }
            //    });
            //Dictionary<string, Quotes>
            ResponseObject = JsonConvert.DeserializeObject<T>(ResponseString);
            //if (ResponseObject.candles.Count == 0)
            //{
            //    IsResponseValid = false;
            //}
            //else
            //{
            //    IsResponseValid = true;
            //}
            return ResponseObject;
        }
    }
}
