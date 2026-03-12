using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model.Control;
using Spring.Context;

namespace ACS.Framework.Application
{
    public interface IApplicationControlManager
    {
       
        bool Control(ControlMessage paramControlMessage);

        bool HeartBeat(ControlMessage paramControlMessage);

        bool InvokeHeartBeat();

        bool ReloadWorkflow(ControlMessage paramControlMessage);

        bool InvokeReloadWorkflow();

        //bool StartSecsControllable(ControlMessage paramControlMessage);

        //bool InvokeStartSecsControllable(String paramString);

        //bool StopSecsControllable(ControlMessage paramControlMessage);

        //bool InvokeStopSecsControllable(String paramString);

        //bool LoadSecsControllable(ControlMessage paramControlMessage);

        //bool InvokeLoadSecsControllable(String paramString);

        //bool UnloadSecsControllable(ControlMessage paramControlMessage);

        //bool InvokeUnloadSecsControllable(String paramString);

        //bool UpdateSecsControllable(ControlMessage paramControlMessage);

        //bool InvokeUpdateSecsControllable(String paramString);

        bool ReloadService(ControlMessage paramControlMessage);

        bool InvokeReloadService();

        bool Stop(ControlMessage paramControlMessage);

        bool InvokeStop(String paramString1, String paramString2);

        bool Gc(ControlMessage paramControlMessage);

        bool InvokeGc();

        bool RefreshCache(ControlMessage paramControlMessage);

        bool InvokeRefreshCache();


        IApplicationContext ApplicationContext { get; set; }
        IReloadableApplicationContextAware ReloadableApplicationContextAware { get; set; }
        string ReloadableDirectory { get; set; }
        string[] ReloadableAssemblyDefinitions { get; set; }
    }
}
