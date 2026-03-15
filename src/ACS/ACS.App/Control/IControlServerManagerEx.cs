using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message.Model.Control;
using ACS.Core.Message;
using ACS.Core.History;
using ACS.Core.Application;
using ACS.Communication.Msb;

namespace ACS.Control
{
    public interface IControlServerManagerEx : IControlServerManager
    {
        long ServerTimeSyncInterval { get; set; }
        long ServerTimeSyncDelay { get; set; }
        long ServerTimeSyncStartDelay { get; set; }
        long ServerTimeSyncTimeout { get; set; }
        int ServerTimeSyncRetryCount { get; set; }

        IApplicationManagerACS ApplicationManagerACS { get; set; }
        void ScheduleServerTimeSync();
        bool ScheduleServerTimeSync(string applicationName);
        bool UnScheduleServerTimeSync(string applicationName);
        void PauseServerTImeSync(string applicationName);
        void ResumeServerTimeSync(string applicationName);
    }
}
