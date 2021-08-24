using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs.SubProcessBase
{
    public interface IProcessBaseConfig
    {
        public string ProcessName { get; set; }
        public bool HasCompleted { get; set; }
        public string Status { get; set; }
        public void ExecuteProcess();
        public void SetDailyTaskParameters();

    }
}
