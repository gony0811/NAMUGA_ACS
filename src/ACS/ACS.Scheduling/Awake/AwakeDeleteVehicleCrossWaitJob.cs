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

namespace ACS.Scheduling
{
    public class AwakeDeleteVehicleCrossWaitJob : QuartzJobObject// : AbstractJob
    {
        //protected static final Logger logger = Logger.getLogger(AwakeDeleteVehicleCrossWaitJob.class);
        protected IResourceManagerEx resourceManager;
        public IResourceManagerEx ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        protected override void ExecuteInternal(JobExecutionContext context)
        {
            //logger.info("AwakeDeleteVehicleCrossWaitJob will be invoked");

            this.resourceManager = ((IResourceManagerEx)context.MergedJobDataMap.Get("ResourceManager"));

            int savePeriod = Convert.ToInt32(context.MergedJobDataMap.Get("SavePeriod").ToString()); //60

            /*
            Calendar toCalendar = new GregorianCalendar();
            toCalendar.SetTime(new Date());
            toCalendar.Add(13, -savePeriod);*/
            DateTime toCalendar = DateTime.Now;
            DateTime toDelete = toCalendar.AddSeconds(-savePeriod);

            //logger.info("VehicleCrossWait will be deleted until {" + TimeUtils.getTimeToMilliPrettyFormat(toCalendar.getTime()) + "}");

            int count = this.resourceManager.DeleteVehicleCrossWait(toDelete);

            //logger.info("VehicleCrossWait(" + count + ") was deleted, DeleteVehicleCrossWaitJob was end");
        }
    }
}
