using System;
using System.Collections;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.Core.DependencyInjection;
using ACS.Core.Logging;
using ACS.Core.Logging.Implement;
using ACS.Core.Base;
using ACS.Core.Base.Interface;
using ACS.App;

namespace ACS.App.Modules
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

            // LogManager (공통) — DB 로깅(NA_L_LOGMESSAGE) 설정을 appsettings.json에서 주입한다.
            // Acs:Logging:Database 섹션이 없으면 기본값(활성화 / INFO 이상 / 비동기 큐)을 사용.
            builder.Register(c =>
            {
                var config = c.Resolve<IConfiguration>();
                bool enabled = !bool.TryParse(config["Acs:Logging:Database:Enabled"], out var e) || e;
                string level = config["Acs:Logging:Database:Level"] ?? "INFO";
                int capacity = int.TryParse(config["Acs:Logging:Database:QueueCapacity"], out var cap) ? cap : 10000;
                int batchSize = int.TryParse(config["Acs:Logging:Database:BatchSize"], out var bs) ? bs : 200;

                return new LogManagerImpl
                {
                    PersistentDao = c.Resolve<IPersistentDao>(),
                    UseAdoDotNetAppender = enabled,
                    LogLevel = level,
                    SkipLoggingMessages = new ArrayList(),
                    UseShortClassNameAtOperationName = true,
                    ProcessName = config["Acs:Process:Name"],
                    QueueCapacity = capacity,
                    BatchSize = batchSize
                };
            })
                .As<ILogManager>()
                .SingleInstance();

            // MessageNode (공통)
            builder.RegisterType<ACS.Core.Message.MessageNode>()
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
