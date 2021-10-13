using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public interface IProcessBaseConfig
    {
        public void ExecuteProcess();
        public void SetDailyTaskParameters();

    }
}
