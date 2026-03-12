using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Scheduling
{
    public interface IDaemonServerManager
    {
        void DisplaySchedulingHistoryInfo();

        void DisplaySchedulingLogInfo();

        void DisplaySchedulingAwakeInfo();

        void DisplaySchedulingTransportInfo();

        void DisplaySchedulingDefaultInfo();
    }
}
