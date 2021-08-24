using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public class ProcessBaseConfig : IProcessBaseConfig
    {
        public string ProcessName { get; set; }
        public bool HasCompleted { get; set; }
        public string Status { get; set; }

        public virtual void ExecuteProcess()
        {

        }
        public virtual void SetDailyTaskParameters()
        {

        }
        

    }
}
