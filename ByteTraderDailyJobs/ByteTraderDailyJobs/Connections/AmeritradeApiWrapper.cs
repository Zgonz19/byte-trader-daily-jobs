using Abbotware.Interop.TDAmeritrade;
using Abbotware.Interop.TDAmeritrade.Configuration.Models;
using Abbotware.Interop.TDAmeritrade.Models;
using ByteTraderDailyJobs.DataObjects;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteTraderDailyJobs.Connections
{
    public class AmeritradeApiWrapper
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        public string TargetUrl { get; set; }
        public string ResponseString { get; set; }
        public dynamic ResponseObject { get; set; }
        public string TDAmeritradeApiKey { get; set; }
        public AccessToken AccessToken { get; set; }
        public bool KeepApiActive { get; set; }
        public static RefreshToken RefreshToken { get; set; }
        private readonly object RefreshTokenLock = new object();

        public AmeritradeApiWrapper()
        {

        }
        public void InitializeApiWrapper(AccessToken accessToken, string apiKey)
        {
            KeepApiActive = true;
            TDAmeritradeApiKey = apiKey;
            AccessToken = accessToken;
            SetRefreshToken();
        }

        public void PollRefreshToken()
        {
            while (KeepApiActive)
            {
                Thread.Sleep(new TimeSpan(0,20,0));
                SetRefreshToken();
            }
        }

        public async void SetRefreshToken()
        {
            RefreshToken refreshToken = null;
            try
            {
                var client = new HttpClient();
                // Create the HttpContent for the form to be posted.
                var requestContent = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", $"{AccessToken.refresh_token}"),
                        new KeyValuePair<string, string>("client_id", $"{TDAmeritradeApiKey}@AMER.OAUTHAP")});

                // Get the response.
                HttpResponseMessage response = client.PostAsync("https://api.tdameritrade.com/v1/oauth2/token", requestContent).Result;
                // Get the response content.
                HttpContent responseContent = response.Content;
                // Get the stream of the content.
                using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                {
                    var output = reader.ReadToEndAsync().Result;
                    refreshToken = JsonConvert.DeserializeObject<RefreshToken>(output);
                }
                if (!(refreshToken == null))
                {
                    if (refreshToken.access_token != null)
                    {
                        lock (RefreshTokenLock)
                        {
                            RefreshToken = refreshToken;
                            Logger.Info("Refresh Token Updated");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task<Fundamental> GetFundamentalData(string symbol)
        {
            Fundamental data = null;
            try
            {
                var nullLogger = new Abbotware.Core.Logging.Plugins.NullLogger();
                lock (RefreshTokenLock)
                {
                    var settings = new TDAmeritradeSettings
                    {
                        ApiKey = TDAmeritradeApiKey,
                        BearerToken = RefreshToken.access_token
                    };
                    var tdaClient = new TDAmeritradeClient(settings, nullLogger);
                    var result = tdaClient.FundamentalDataAsync(symbol, new CancellationToken()).Result;
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        data = result.Response.Fundamental;
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
            return data;
        }
    }
}
