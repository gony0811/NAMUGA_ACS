using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Scheduling.Model;
using Quartz;

namespace ACS.Control.Scheduling
{
    public class SimpleHeartBeatJob : AbstractJob
    {

        public static string GROUP_SIMPLE_HEARTBEAT = "GROUP-SIMPLE-HEARTBEAT";
        public override void ExecuteJob(IJobExecutionContext context)
        {
            IControlServerManager controlServerManager = (IControlServerManager)context.MergedJobDataMap.Get("ControlServerManager");
            string applicationName = (string)context.MergedJobDataMap.Get("ApplicationName");

            // logger.info("myself{" + applicationName + "} will be checked");

            controlServerManager.ApplicationManager.UpdateApplicationCheckTime(applicationName, DateTime.UtcNow);

            controlServerManager.DisplayTriggers();
        }
    }
}
