using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.Framework.DependencyInjection;
using ACS.Framework.Logging;
using ACS.Framework.Logging.Implement;
using ACS.Framework.Base;

namespace ACS.Application.Modules
{
    /// <summary>
    /// 모든 프로세스 타입에 공통으로 등록되는 서비스 모듈.
    /// Spring.NET XML의 AbstractManager 추상 빈 및 공통 빈 정의를 대체.
    /// </summary>
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // IEventAggregator (Spring ApplicationContext.PublishEvent 대체)
            builder.RegisterType<EventAggregator>()
                .As<IEventAggregator>()
                .SingleInstance();

            // IServiceLocator (Spring GetObject/GetObjectsOfType 레거시 호환)
            builder.Register(c => new AutofacServiceLocator(c.Resolve<ILifetimeScope>()))
                .As<IServiceLocator>()
                .InstancePerLifetimeScope();

            // LogManager (공통)
            builder.RegisterType<LogManagerImpl>()
                .As<ILogManager>()
                .SingleInstance()
                .PropertiesAutowired();

            // MessageNode (공통)
            builder.RegisterType<ACS.Framework.Message.MessageNode>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

            // ApplicationEventListener 대체 — ApplicationInitializer 등록
            builder.RegisterType<ApplicationInitializer>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
