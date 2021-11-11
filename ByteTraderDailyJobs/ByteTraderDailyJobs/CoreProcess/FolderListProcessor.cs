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
            //BaseFolderPath = @"C:\Users\Gonzalo\Dropbox\Github_08_2021\byte-trader-daily-jobs\DailyProcessList";
            BaseFolderPath = @"E:\ByteTraderProduction\DailyProcessList";
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
        }


        public async void ExecuteFolderTask(FolderBase task)
        {
            if (task.ProcessConfig.AllowExecution)
            {
                task.ProcessConfig.ExecuteProcess();
            }
            else
            {

            }
        }
    }
}
