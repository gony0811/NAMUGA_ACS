using System;
using System.Reflection;
using Autofac;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.Transfer;
using ACS.Core.History;
using ACS.Core.Message;
using ACS.Core.Alarm;
using ACS.Control;

namespace ACS.App.Modules
{
    /// <summary>
    /// control 프로세스 전용 서비스 등록.
    /// config/{SITE}/Startup/acs/control/control-manager.xml을 대체.
    /// </summary>
    public class ControlModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationManagerImplement>()
                .As<IApplicationManager>()
                .SingleInstance()
                .PropertiesAutowired();

            // ACS.Manager types resolved by name to avoid circular project reference
            var resourceMgrType = Type.GetType("ACS.Manager.Resource.ResourceManagerExImplement, ACS.Manager");
            if (resourceMgrType != null)
                builder.RegisterType(resourceMgrType)
                    .As<IResourceManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());

            var materialMgrType = Type.GetType("ACS.Manager.Material.MaterialManagerExImplement, ACS.Manager");
            if (materialMgrType != null)
                builder.RegisterType(materialMgrType)
                    .As<IMaterialManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // Spring XML에서는 TransferManagerExsImplement (ITransferManagerExs) 사용
            var transferMgrType = Type.GetType("ACS.Manager.TransferManagerExsImplement, ACS.Manager");
            if (transferMgrType != null)
                builder.RegisterType(transferMgrType)
                    .As<ITransferManagerEx>()
                    .As(Type.GetType("ACS.Core.Transfer.ITransferManagerExs, ACS.Core"))
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((ACS.Core.Base.AbstractManager)e.Instance).Init());

            var historyMgrType = Type.GetType("ACS.Manager.History.HistoryManagerExImplement, ACS.Manager");
            if (historyMgrType != null)
                builder.RegisterType(historyMgrType)
                    .As<IHistoryManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            var appControlMgrType = Type.GetType("ACS.Manager.Application.ApplicationControlManagerExImplement, ACS.Manager");
            if (appControlMgrType != null)
                builder.RegisterType(appControlMgrType)
                    .As<IApplicationControlManager>()
                    .SingleInstance()
                    .PropertiesAutowired();

            builder.RegisterType<ControlServerManagerImplement>()
                .As<IControlServerManager>()
                .SingleInstance()
                .PropertiesAutowired()
                .OnActivated(e =>
                {
                    var mgr = (ControlServerManagerImplement)e.Instance;
                    // Scheduling job types (protected Type — set via reflection)
                    void SetProtected(string name, object value)
                    {
                        var prop = typeof(ControlServerManagerImplement).GetProperty(name,
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        prop?.SetValue(mgr, value);
                    }
                    SetProtected("SimpleHeartBeatJobType", Type.GetType("ACS.Control.Scheduling.SimpleHeartBeatJob, ACS.Control"));
                    SetProtected("HeartBeatJobType", Type.GetType("ACS.Control.Scheduling.HeartBeatJob, ACS.Control"));
                    SetProtected("UiTransportJobType", Type.GetType("ACS.Control.Scheduling.UiTransportJob, ACS.Control"));
                    SetProtected("UiApplicationManagerJobType", Type.GetType("ACS.Control.Scheduling.UiApplicationManagerJob, ACS.Control"));
                    SetProtected("RescheduleHeartBeatJobType", Type.GetType("ACS.Control.Scheduling.RescheduleHeartBeatJob, ACS.Control"));
                    SetProtected("UiCommandJobType", Type.GetType("ACS.Control.Scheduling.UiCommandJob, ACS.Control"));
                    SetProtected("WindowRedirectFilePath", "log/server/control/start");
                    // Feature toggles
                    mgr.UseHeartBeat = false;
                    mgr.UseUiTransport = true;
                    mgr.UseUiCommand = true;
                    mgr.UseUiApplicationManager = false;
                    // HeartBeat settings
                    mgr.HeartBeatInterval = 20000;
                    mgr.HeartBeatStartDelay = 10000;
                    mgr.HeartBeatTimeout = 5000;
                    mgr.HeartBeatRetryCount = 3;
                    mgr.HeartBeatRetryTimeout = 10000;
                    mgr.SimpleHeartBeatInterval = 5000;
                    mgr.SimpleHeartBeatStartDelay = 2000;
                    mgr.HeartBeatFailWhenProcessDown = 1;
                    mgr.HeartBeatFailWhenProcessHang = 1;
                    // UI intervals
                    mgr.UiCommandInterval = 1;
                    mgr.UiTransportInterval = 1;
                    mgr.UiTransportStartDelay = 10000;
                    mgr.UiApplicationManagerInterval = 3000;
                    mgr.UiApplicationManagerStartDelay = 10000;
                    // System settings
                    mgr.UseSystemKill = true;
                    mgr.UseSystemGetProcessId = true;
                    mgr.Scripts = new System.Collections.Hashtable
                    {
                        ["TS-START"] = @"D:\ACS\TS01_P\TS01_P.exe",
                        ["ES-START"] = @"D:\ACS\ES01_P\ES01_P.exe",
                        ["DS-START"] = @"D:\ACS\DS01_P\DS01_P.exe"
                    };
                });

            // MessageManager
            var messageMgrType = Type.GetType("ACS.Manager.MessageManagerExsImplement, ACS.Manager");
            if (messageMgrType != null)
                builder.RegisterType(messageMgrType)
                    .As<IMessageManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());

            // AlarmManager
            var alarmMgrType = Type.GetType("ACS.Manager.Alarm.AlarmManagerExImplement, ACS.Manager");
            if (alarmMgrType != null)
                builder.RegisterType(alarmMgrType)
                    .As<IAlarmManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // Elsa Workflows 3 — hybrid bridge (Elsa + legacy WorkflowManagerImpl)
            // Elsa Workflows 3
            builder.RegisterModule<ACS.Elsa.ElsaModule>();
        }
    }
}
