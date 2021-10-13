using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public interface IProcessBaseConfig
    {
        public bool AllowExecution { get; set; }
        public void ExecuteProcess();
        public void SetDailyTaskParameters();

    }
}
