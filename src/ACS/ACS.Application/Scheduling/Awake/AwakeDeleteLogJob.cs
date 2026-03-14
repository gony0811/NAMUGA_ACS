using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource;
using System.Globalization;
using System.IO;
using ACS.Framework.Logging;

namespace ACS.Scheduling
{
    public class AwakeDeleteLogJob : DailyBackgroundService
    {
        public AwakeDeleteLogJob()
        {
        }

        protected override void ExecuteOnce()
        {
            try
            {
                delLog();
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }
        }


        private void delLog()
        {
            string delday = System.Configuration.ConfigurationManager.AppSettings["log_del_date"];
            string logDir = System.Configuration.ConfigurationManager.AppSettings["log_path"];

            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            if (!Directory.Exists(logDir))
            {
                return;
            }

            DirectoryInfo serverFolderDi = new DirectoryInfo(logDir);
            check(serverFolderDi, delday);
        }

        public void check(DirectoryInfo FolderName, string delday)
        {
            DirectoryInfo[] diList = FolderName.GetDirectories();
            FileInfo[] files = FolderName.GetFiles();
            string lDate = DateTime.Today.AddDays(-(int.Parse(delday))).ToString("yyyy-MM-dd");

            if(diList.Length > 0)
            {
                foreach (DirectoryInfo di in diList)
                {
                    check(di, delday);
                }
            }
            if(files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (lDate.CompareTo(file.LastWriteTime.ToString("yyyy-MM-dd")) > 0)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(file.Name, ".log"))
                        {
                            File.Delete(FolderName.FullName + "\\" + file.Name);
                        }

                    }

                }
            }

        }

    }


}
