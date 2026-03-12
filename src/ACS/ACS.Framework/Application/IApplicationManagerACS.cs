using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Application
{
    public interface IApplicationManagerACS : IApplicationManager
    {
        bool IsWifiMode(string paramString);
        string GetNioConnectMode(string paramString);

        IList GetApplicationNamesByStateAndRunHW(string type, string state, string runningHardware);
    }
}
