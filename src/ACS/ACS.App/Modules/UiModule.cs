using System;
using Autofac;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Resource;
using ACS.Core.Transfer;
using ACS.Core.Cache;
using ACS.Core.Alarm;
using ACS.Communication.Http;

namespace ACS.App.Modules
{
    /// <summary>
    /// UI(REST API) 프로세스 전용 서비스 등록.
    /// 읽기 전용 데이터 조회에 필요한 매니저만 등록.
    /// </summary>
    public class UiModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationManagerImplement>()
                .As<IApplicationManager>()
                .SingleInstance()
                .PropertiesAutowired();

            var resourceMgrType = Type.GetType("ACS.Manager.Resource.ResourceManagerExImplement, ACS.Manager");
            if (resourceMgrType != null)
                builder.RegisterType(resourceMgrType)
                    .As<IResourceManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());

            var transferMgrType = Type.GetType("ACS.Manager.Transfer.TransferManagerExImplement, ACS.Manager");
            if (transferMgrType != null)
                builder.RegisterType(transferMgrType)
                    .As<ITransferManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            var appControlMgrType = Type.GetType("ACS.Manager.Application.ApplicationControlManagerExImplement, ACS.Manager");
            if (appControlMgrType != null)
                builder.RegisterType(appControlMgrType)
                    .As<IApplicationControlManager>()
                    .SingleInstance()
                    .PropertiesAutowired();

            var cacheMgrType = Type.GetType("ACS.Manager.CacheManagerExImplement, ACS.Manager");
            if (cacheMgrType != null)
                builder.RegisterType(cacheMgrType)
                    .As<ICacheManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            var alarmMgrType = Type.GetType("ACS.Manager.Alarm.AlarmManagerExImplement, ACS.Manager");
            if (alarmMgrType != null)
                builder.RegisterType(alarmMgrType)
                    .As<IAlarmManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            builder.RegisterType<HttpCommServer>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();
        }
    }
}
