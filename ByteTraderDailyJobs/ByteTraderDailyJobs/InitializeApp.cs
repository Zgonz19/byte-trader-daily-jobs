using ByteTraderDailyJobs.CoreProcess;
using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderDailyJobs
{
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
            var folders = FolderProcessor.ReadFolderList();

            foreach(var folder in folders)
            {
                FolderProcessor.ExecuteFolderTask(folder);
            }
        }

    }
}
