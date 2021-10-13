using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class ProcessBaseConfig : IProcessBaseConfig
    {
        public bool AllowExecution { get; set; }

        public virtual void ExecuteProcess()
        {

        }
        public virtual void SetDailyTaskParameters()
        {

        }
        

    }
}
