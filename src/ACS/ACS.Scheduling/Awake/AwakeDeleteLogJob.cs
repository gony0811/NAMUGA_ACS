//using ACS.Framework.Scheduling.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Spring.Scheduling.Quartz;
using ACS.Framework.Resource;
using System.Globalization;
using System.IO;
using log4net;
using ACS.Framework.Logging;
namespace ACS.Scheduling
{
    public class AwakeDeleteLogJob : QuartzJobObject//: AbstractJob
    {
        protected Logger logger = Logger.GetLogger("SCHEDULING_LOG");
        //protected static final Logger logger = Logger.getLogger(AwakeDeleteUiInformJob.class);
        protected IResourceManagerEx resourceManager;

        public IResourceManagerEx ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        protected override void ExecuteInternal(JobExecutionContext context)
        {
            try
            {
                //매일 00시 아래 구문 수행.
                //log_del_date에 설정 된 값을 기준으로 로그 삭제
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

            var appenders = LogManager.GetRepository().GetAppenders();
            if (appenders.Length < 1) return;

            foreach (var temp in appenders)
            {
                if (temp.GetType().ToString() != "log4net.Appender.RollingFileAppender")
                    continue;
                log4net.Appender.RollingFileAppender appender = (log4net.Appender.RollingFileAppender)temp;
                string logfilename = appender.File;

                FileInfo logfile = new FileInfo(logfilename);

                string logdir = logfile.Directory.ToString();
                string parent = System.IO.Path.GetDirectoryName(logdir);

                string parent2 = System.IO.Path.GetDirectoryName(parent);

                //DirectoryInfo di = new DirectoryInfo(logdir);

                //월별 폴더를 삭제
                //DirectoryInfo FolderDi = new DirectoryInfo(parent);
                //DirectoryInfo[] diList = FolderDi.GetDirectories();
                //int month = Convert.ToInt16(logdir.Substring(logdir.Length - 2));

                //foreach (DirectoryInfo folder in diList)
                //{
                //    if (Convert.ToInt16(folder.Name) <= month - Convert.ToInt16(delday))
                //    {
                //        Directory.Delete(folder.FullName, true);
                //    }
                //}

                DirectoryInfo SeverFolderDi = new DirectoryInfo(parent2);//server
                check(SeverFolderDi, delday);
            }
        }

        public void check(DirectoryInfo FolderName, string delday)
        {
            DirectoryInfo[] diList = FolderName.GetDirectories();//폴더 내 폴더 취득
            FileInfo[] files = FolderName.GetFiles(); //폴더 내 파일 취득
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
                        //log파일만지움
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
