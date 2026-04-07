using System;
using Autofac;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.Transfer;
using ACS.Core.History;
using ACS.Core.Cache;
using ACS.Core.Path;
using ACS.Core.Message;
using ACS.Core.Host;
using ACS.Core.Alarm;
using ACS.Communication.Socket;
using ACS.Communication.Mqtt;
using ACS.Communication.Msb;

namespace ACS.App.Modules
{
    /// <summary>
    /// trans 프로세스 전용 서비스 등록.
    /// config/{SITE}/Startup/acs/trans/trans-manager.xml을 대체.
    /// </summary>
    public class TransModule : Module
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

            var transferMgrType = Type.GetType("ACS.Manager.Transfer.TransferManagerExImplement, ACS.Manager");
            if (transferMgrType != null)
                builder.RegisterType(transferMgrType)
                    .As<ITransferManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

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

            var pathMgrType = Type.GetType("ACS.Manager.Path.PathManagerExImplement, ACS.Manager");
            if (pathMgrType != null)
                builder.RegisterType(pathMgrType)
                    .As<IPathManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());
            

            builder.RegisterType<ACS.Communication.Msb.Convert.Implement.MsbXmlConverterExImplement>()
                .As<IMsbConverter>()
                .SingleInstance()
                .PropertiesAutowired()
                .OnActivated(e => e.Instance.GetType().GetMethod("Init")?.Invoke(e.Instance, null));

            var cacheMgrType = Type.GetType("ACS.Manager.CacheManagerExImplement, ACS.Manager");
            if (cacheMgrType != null)
                builder.RegisterType(cacheMgrType)
                    .As<ICacheManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // MessageManager
            var messageMgrType = Type.GetType("ACS.Manager.MessageManagerExsImplement, ACS.Manager");
            if (messageMgrType != null)
                builder.RegisterType(messageMgrType)
                    .As<IMessageManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e =>
                    {
                        ((AbstractManager)e.Instance).Init();
                        // HostAgent는 Named registration이므로 PropertiesAutowired로 주입되지 않음 — 명시적 주입
                        var hostAgentProp = e.Instance.GetType().GetProperty("HostAgent");
                        if (hostAgentProp != null)
                        {
                            var hostAgent = e.Context.ResolveNamed<IMessageAgent>("HostAgentSender");
                            hostAgentProp.SetValue(e.Instance, hostAgent);
                        }
                        // EsAgent도 Named registration — RAIL-CARRIERTRANSFER 전송에 필요
                        var esAgentProp = e.Instance.GetType().GetProperty("EsAgent");
                        if (esAgentProp != null)
                        {
                            var esAgent = e.Context.ResolveNamed<IMessageAgent>("EsAgentSender");
                            esAgentProp.SetValue(e.Instance, esAgent);
                        }
                    });

            // HostMessageManager
            var hostMsgMgrType = Type.GetType("ACS.Manager.Host.HostMessageManagerImplement, ACS.Manager");
            if (hostMsgMgrType != null)
                builder.RegisterType(hostMsgMgrType)
                    .As<IHostMessageManager>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // AlarmManager
            var alarmMgrType = Type.GetType("ACS.Manager.Alarm.AlarmManagerExImplement, ACS.Manager");
            if (alarmMgrType != null)
                builder.RegisterType(alarmMgrType)
                    .As<IAlarmManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // RequestManager
            var requestMgrType = Type.GetType("ACS.Manager.Transfer.RequestManagerExImplement, ACS.Manager");
            if (requestMgrType != null)
                builder.RegisterType(requestMgrType)
                    .AsSelf()
                    .SingleInstance()
                    .PropertiesAutowired();

            // Elsa Workflows 3 — hybrid bridge (Elsa + legacy WorkflowManagerImpl)
            // Elsa Workflows 3
            builder.RegisterModule<ACS.Elsa.ElsaModule>();
        }
    }
}
