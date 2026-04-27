using System;
using Autofac;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Resource;
using ACS.Core.Transfer;
using ACS.Core.Cache;
using ACS.Core.Alarm;

namespace ACS.App.Modules
{
    /// <summary>
    /// UI(REST API + SignalR) 프로세스 전용 서비스 등록.
    /// REST/SignalR 호스팅은 ASP.NET Core(Kestrel)가 담당하며, 본 모듈은 매니저 계층만 등록한다.
    /// 컨트롤러는 ASP.NET Core MVC 활성화 + Autofac DI를 통해 자동으로 매니저를 주입받는다.
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
        }
    }
}
