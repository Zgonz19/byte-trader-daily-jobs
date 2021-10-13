using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderDailyJobs.SubProcessBase.DailyCandleIngestionProcess
{
    public interface IBaseApiCall
    {
        public string TargetUrl { get; set; }
        public string ResponseString { get; set; }
        public dynamic ResponseObject { get; set; }
        public Task<string> CallApi();
        public dynamic SetResponseObject<T>();
    }
}
