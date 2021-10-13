using ByteTraderDailyJobs.SubProcessBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ByteTraderDailyJobs.CoreProcess
{
    public class FolderListProcessor
    {
        string BaseFolderPath = "";
        public FolderListProcessor()
        {
            BaseFolderPath = @"C:\Users\Gonzalo\Dropbox\Github_08_2021\byte-trader-daily-jobs\DailyProcessList";
        }


        public List<FolderBase> ReadFolderList()
        {
            var folderList = new List<FolderBase>();


            var folders = Directory.GetDirectories(BaseFolderPath);

            foreach(var folder in folders)
            {
                var folderObject = new FolderBase(folder);
                folderList.Add(folderObject);
            }
            return folderList;
        }

        public void CreateJsonConfig(FolderBase task)
        {
            var testObject = new DailyCandleIngestion();
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            string output = JsonConvert.SerializeObject(testObject, settings);
            //File.WriteAllText("", output);
            //File.WriteAllText(task.FolderDirectory.FullName + "\\DailyCandleIngestionConfig.json", output);
        }

        public async void ExecuteFolderTask(FolderBase task)
        {
            task.ProcessConfig.ExecuteProcess();

            //CreateJsonConfig(task);
            //task.SetJsonText();
            //var testObject = new DailyCandleIngestion();
            //testObject.ProcessConfig = testObject;
            //JsonSerializerSettings settings = new JsonSerializerSettings
            //{
            //    TypeNameHandling = TypeNameHandling.All
            //};
            //string output = JsonConvert.SerializeObject(testObject, settings);

            //File.WriteAllText(task.FolderDirectory.FullName + "\\DailyCandleIngestionConfig.json", output);


            //if (task.Completed == false && task.ExecuteTask == true)
            //{
                
            //}

        }
    }
}
