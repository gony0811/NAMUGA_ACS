using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Scheduling.Model;
using ACS.Control;
using Quartz;

namespace ACS.Control.Scheduling
{
    public class RescheduleHeartBeatJob : AbstractJob
    {

        public static string GROUP_RESCHEDULE_HEARTBEAT = "GROUP-RESCHEDULE-HEARTBEAT";
        //protected Logger logger = Logger.getLogger(typeof(RescheduleHeartBeatJob));


        public override void ExecuteJob(IJobExecutionContext context)
        {
            IControlServerManager controlServerManager = (IControlServerManager)context.MergedJobDataMap.Get("ControlServerManager");
            controlServerManager.RescheduleHeartBeats();
        }
    }
}
