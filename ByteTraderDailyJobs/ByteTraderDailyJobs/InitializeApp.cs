using ByteTraderDailyJobs.CoreProcess;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs
{
    public static class ConfigurationManager
    {
        public static IConfiguration AppSetting { get; }
        static ConfigurationManager()
        {
            AppSetting = new ConfigurationBuilder()
                    .SetBasePath(@"C:\Users\Gonzalo\Dropbox\Github_08_2021\byte-trader-daily-jobs")
                    .AddJsonFile("appsettings.json")
                    .Build();
        }
    }
    public class InitializeApp
    {
        public FolderListProcessor FolderProcessor { get; set; }
        public InitializeApp()
        {
            FolderProcessor = new FolderListProcessor();
        }


        public static void MadeupFunction()
        {

        }

        public void ExecuteProcessList()
        {
            try
            {
                var folders = FolderProcessor.ReadFolderList();

                foreach (var folder in folders)
                {
                    FolderProcessor.ExecuteFolderTask(folder);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }


        }

    }
}
