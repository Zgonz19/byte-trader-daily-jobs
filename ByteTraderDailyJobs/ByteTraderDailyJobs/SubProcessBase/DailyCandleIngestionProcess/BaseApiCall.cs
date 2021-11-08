using ByteTraderDailyJobs.DataObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private AccessToken Token { get; set; }
        public BaseApiCall(string targetUrl)
        {
            TargetUrl = targetUrl;
        }

        public BaseApiCall(string targetUrl, AccessToken Token)
        {
            TargetUrl = targetUrl;
            this.Token = Token;
        }

        public async Task<string> CallApi()
        {
            string response;



            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "encrypted user/pwd");



            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "Your Oauth token");
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

        public class AccessToken
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string scope { get; set; }
            public long expires_in { get; set; }
            public long refresh_token_expires_in { get; set; }
            public string Bearer { get; set; }
        }
    }
}
