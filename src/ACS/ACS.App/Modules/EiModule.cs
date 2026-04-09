using System;
using Autofac;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.Transfer;
using ACS.Core.History;
using ACS.Core.Cache;
using ACS.Core.Message;
using ACS.Core.Alarm;
using ACS.Communication.Socket;
using ACS.Communication.Mqtt;
using ACS.Communication.Msb;

namespace ACS.App.Modules
{
    /// <summary>
    /// ei(외부 인터페이스) 프로세스 전용 서비스 등록.
    /// config/{SITE}/Startup/acs/ei/ei-manager.xml을 대체.
    /// </summary>
    public class EiModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ACS.App.Implement.ApplicationManagerACSImplement>()
                .As<IApplicationManager>()
                .SingleInstance()
                .PropertiesAutowired();

            // ACS.Manager types resolved by name to avoid circular project reference
            var resourceMgrType = Type.GetType("ACS.Manager.ResourceManagerExsImplement, ACS.Manager");
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

            var historyMgrType = Type.GetType("ACS.Manager.HistoryManagerExsImplement, ACS.Manager");
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

            builder.RegisterType<NioInterfaceManager>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

            builder.RegisterType<MqttInterfaceManager>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

            builder.RegisterType<ACS.Communication.Socket.Checker.DuplicatedCheckerImpl>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

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
                        // EsAgent: Named registration — RAIL-CARRIERTRANSFER 전송에 필요
                        var esAgentProp = e.Instance.GetType().GetProperty("EsAgent");
                        if (esAgentProp != null)
                        {
                            var esAgent = e.Context.ResolveNamed<IMessageAgent>("EsAgentSender");
                            esAgentProp.SetValue(e.Instance, esAgent);
                        }
                    });

            // AlarmManager
            var alarmMgrType = Type.GetType("ACS.Manager.Alarm.AlarmManagerExImplement, ACS.Manager");
            if (alarmMgrType != null)
                builder.RegisterType(alarmMgrType)
                    .As<IAlarmManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // Elsa Workflows 3
            builder.RegisterModule<ACS.Elsa.ElsaModule>();
        }
    }
}
