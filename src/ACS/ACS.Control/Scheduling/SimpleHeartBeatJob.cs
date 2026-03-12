using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Scheduling.Model;

namespace ACS.Control.Scheduling
{
    public class SimpleHeartBeatJob : AbstractJob
    {

        public static string GROUP_SIMPLE_HEARTBEAT = "GROUP-SIMPLE-HEARTBEAT";
        //protected static ILog logger = LogManager.GetLogger(typeof(SimpleHeartBeatJob)); 
        public override void Execute(Quartz.JobExecutionContext context)
        {
            IControlServerManager controlServerManager = (IControlServerManager)context.MergedJobDataMap.Get("ControlServerManager");
            string applicationName = (string)context.MergedJobDataMap.Get("ApplicationName");

            // logger.info("myself{" + applicationName + "} will be checked");

            controlServerManager.ApplicationManager.UpdateApplicationCheckTime(applicationName, DateTime.Now);

            controlServerManager.DisplayTriggers();
        }
    }
}
