using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Scheduling;
using ACS.Core.Base;

namespace ACS.Scheduling.Implement
{
    public class DaemonServerManagerImplement : AbstractManager, IDaemonServerManager
    {
        private ISchedulingManager schedulingManager;
        public ISchedulingManager SchedulingManager { get { return schedulingManager; } set { schedulingManager = value; } }

        public void DisplaySchedulingHistoryInfo()
        {
            String groupName = "HISTORY";

            this.schedulingManager.DisplayTriggerGroup(groupName);
            this.schedulingManager.DisplayJobGroup(groupName);
        }

        public void DisplaySchedulingLogInfo()
        {
            String groupName = "LOG";

            this.schedulingManager.DisplayTriggerGroup(groupName);
            this.schedulingManager.DisplayJobGroup(groupName);
        }

        public void DisplaySchedulingAwakeInfo()
        {
            String groupName = "AWAKE";

            this.schedulingManager.DisplayTriggerGroup(groupName);
            this.schedulingManager.DisplayJobGroup(groupName);
        }

        public void DisplaySchedulingTransportInfo()
        {
            String groupName = "TRANSPORT";

            this.schedulingManager.DisplayTriggerGroup(groupName);
            this.schedulingManager.DisplayJobGroup(groupName);
        }

        public void DisplaySchedulingDefaultInfo()
        {
            String groupName = "DEFAULT";

            this.schedulingManager.DisplayTriggerGroup(groupName);
            this.schedulingManager.DisplayJobGroup(groupName);
        }
    }
}