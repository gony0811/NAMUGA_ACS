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
using ACS.Framework.Logging;

namespace ACS.Scheduling
{
    public class AwakeDeleteUiInformJob : QuartzJobObject//: AbstractJob
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
            //logger.info("AwakeDeleteUiInformJob will be invoked");
            try
            {
                this.resourceManager = ((IResourceManagerEx)context.MergedJobDataMap.Get("ResourceManager"));

                int savePeriod = Convert.ToInt32((String)context.MergedJobDataMap.Get("SavePeriod")); //6
                //매일 00시 아래 구문 수행.
                //log_del_date에 설정 된 값을 기준으로 로그 삭제
                DeleteUiInformJob(savePeriod);
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }            
        }

        private void DeleteUiInformJob(int savePeriod)
        {
            //Calendar calendar = new GregorianCalendar();
            //calendar.Add(10, -savePeriod);

            //Calendar toCalendar = new GregorianCalendar(calendar.get(1), calendar.get(2), calendar.get(5) + 1);
            //logger.info("UiInform will be deleted until {" + toCalendar.getTime() + "}");
            DateTime toCalendar = DateTime.Now;
            DateTime deleteCalendar = toCalendar.AddHours(-savePeriod);

            int count = this.resourceManager.DeleteUIInform(deleteCalendar);

            //logger.info("UIInform(" + count + ") was deleted, DeleteUiInformJob was end.");
        }
    }
}
