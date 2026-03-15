using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using ACS.Core.Message.Model.Control;

namespace ACS.Core.Application
{
    public interface IApplicationControlManager
    {

        bool Control(ControlMessage paramControlMessage);

        bool HeartBeat(ControlMessage paramControlMessage);

        bool InvokeHeartBeat();

        bool ReloadWorkflow(ControlMessage paramControlMessage);

        bool InvokeReloadWorkflow();

        bool ReloadService(ControlMessage paramControlMessage);

        bool InvokeReloadService();

        bool Stop(ControlMessage paramControlMessage);

        bool InvokeStop(String paramString1, String paramString2);

        bool Gc(ControlMessage paramControlMessage);

        bool InvokeGc();

        bool RefreshCache(ControlMessage paramControlMessage);

        bool InvokeRefreshCache();


        ILifetimeScope LifetimeScope { get; set; }
        IReloadableApplicationContextAware ReloadableApplicationContextAware { get; set; }
        string ReloadableDirectory { get; set; }
        string[] ReloadableAssemblyDefinitions { get; set; }
    }
}
