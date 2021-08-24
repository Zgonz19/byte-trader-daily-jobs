using ByteTraderDailyJobs.SubProcessBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ByteTraderDailyJobs.CoreProcess
{
    public class FolderBase
    {
        public DirectoryInfo FolderDirectory { get; set; }
        public string TaskConfigJsonText { get; set; }
        public IProcessBaseConfig ProcessConfig { get; set; } 

        public FolderBase(string folderPath)
        {
            InitializeFolder(folderPath);
        }

        public void InitializeFolder(string folderPath)
        {
            FolderDirectory = new DirectoryInfo(folderPath);
            var x = Directory.GetFiles(FolderDirectory.FullName, "*.json").ToList();
            TaskConfigJsonText = File.ReadAllText(x[0]);
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
            };

            ProcessConfig = JsonConvert.DeserializeObject<IProcessBaseConfig>(TaskConfigJsonText, settings);
            //var list = new List<IProcessBaseConfig>();
            //var item1 = new DailyCandleIngestion();
            //var item2 = new ProcessBaseConfig();

            //list.Add(item1);
            //list.Add(item2);


            //foreach (var item in list)
            //{
            //    item.ExecuteProcess();
            //    item.HasCompleted = true;
            //    item.ProcessName = "";
            //    item.SetDailyTaskParameters();
            //    item.Status = "";
            //}


        }
    }
}
