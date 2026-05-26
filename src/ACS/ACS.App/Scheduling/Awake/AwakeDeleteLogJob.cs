using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource;
using System.Globalization;
using System.IO;
using ACS.Core.Logging;
using Microsoft.Extensions.Configuration;

namespace ACS.Scheduling
{
    public class AwakeDeleteLogJob : DailyBackgroundService
    {
        private readonly IConfiguration _configuration;

        public AwakeDeleteLogJob(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
            // appsettings.json은 Acs:LogDeleteDays(자식 키)로 보존 일수를 둔다.
            // null/빈값/비정수면 int.Parse가 예외를 던지므로 TryParse + 기본 7일로 가드한다.
            string delday = _configuration["Acs:LogDeleteDays"];
            if (!int.TryParse(delday, out var days) || days <= 0)
            {
                days = 7;
            }

            string logDir = _configuration["Acs:Logging:Path"];

            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            if (!Directory.Exists(logDir))
            {
                return;
            }

            DirectoryInfo serverFolderDi = new DirectoryInfo(logDir);
            check(serverFolderDi, days);
        }

        public void check(DirectoryInfo FolderName, int days)
        {
            DirectoryInfo[] diList = FolderName.GetDirectories();
            FileInfo[] files = FolderName.GetFiles();
            string lDate = DateTime.Today.AddDays(-days).ToString("yyyy-MM-dd");

            if(diList.Length > 0)
            {
                foreach (DirectoryInfo di in diList)
                {
                    check(di, days);
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
