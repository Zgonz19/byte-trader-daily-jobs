using System;
using Abbotware.Interop.TDAmeritrade;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using ByteTraderDailyJobs.DataObjects;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class DailyCandleIngestion : ProcessBaseConfig
    {
        public string ApiKey { get; set; }
        public DateTime ProcessDate { get; set; }
        //public ProcessBaseConfig ProcessConfig { get; set; }
        public DailyCandleIngestion()
        {

        }

        public override void SetDailyTaskParameters()
        {

        }

        public override void ExecuteProcess()
        {
            var url2 = @"";

            var url = @"";
            using (var httpClient = new HttpClient())
            {
                //httpClient.DefaultRequestHeaders.Add(RequestConstants.UserAgent, RequestConstants.UserAgentValue);
                var response = httpClient.GetStringAsync(new Uri(url)).Result;
                var test = JsonConvert.DeserializeObject<CandleList>(response);
                //return response;
            }



            //var set = 
            //    { Configuration = x,
            //};
            //var settings = new Abbotware.Interop.TDAmeritrade.Configuration.IApiSettings = 
            //{
            //}
            //);
            //var test = new TDAmeritradeClient(null, null);

            //string accessToken = util.accessToken; // get the access token from util 
            //Console.WriteLine("Hello World!");
            //var client = new HttpClient();
            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);
            //var result = await client.GetAsync("https://api.tdamer`enter code here`itrade.com/v1/userprincipals?fields=streamerSubscriptionKeys%2CstreamerConnectionInfo");
            //Console.WriteLine("status= {0}", result.StatusCode);
            //Console.WriteLine(result.Content);
            //Console.ReadKey();
        }


    }
}
