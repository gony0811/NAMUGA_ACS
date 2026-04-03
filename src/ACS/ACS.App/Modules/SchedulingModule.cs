using System;
using System.Collections.Specialized;
using Autofac;
using Quartz;
using Quartz.Impl;
using Microsoft.Extensions.Hosting;
using ACS.Core.Scheduling;

namespace ACS.App.Modules
{
    /// <summary>
    /// 스케줄러 및 BackgroundService 잡 등록 모듈.
    /// - 10개 Awake 잡: daemon 프로세스에서만 BackgroundService(IHostedService)로 등록
    /// - Quartz IScheduler: Control/EI 동적 잡용으로 유지
    /// - ISchedulingManager: Control의 동적 스케줄링용으로 등록
    /// </summary>
    public class SchedulingModule : Module
    {
        private readonly string _processType;

        public SchedulingModule(string processType)
        {
            _processType = processType;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Quartz.NET IScheduler — Control/EI 동적 잡(HeartBeat 등)용으로 유지
            builder.Register(c =>
            {
                var properties = new NameValueCollection
                {
                    ["quartz.scheduler.instanceName"] = "ACSScheduler",
                    ["quartz.threadPool.threadCount"] = "5",
                    ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz"
                };

                var factory = new StdSchedulerFactory(properties);
                var scheduler = factory.GetScheduler().GetAwaiter().GetResult();
                return scheduler;
            })
            .As<IScheduler>()
            .SingleInstance();

            // ISchedulingManager — ControlServerManagerImplement의 동적 잡 관리용
            builder.RegisterType<SchedulingManagerImplement>()
                .As<ISchedulingManager>()
                .SingleInstance()
                .PropertiesAutowired();

            // Awake 잡 10개: daemon 프로세스에서만 등록
            if (string.Equals(_processType, "daemon", StringComparison.OrdinalIgnoreCase))
            {
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeChargeTransportJob, ACS.App");
                RegisterHostedService(builder, "ACS.Scheduling.AwakeQueueTransportJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeCheckCrossNodeJob, ACS.App");
                RegisterHostedService(builder, "ACS.Scheduling.AwakeCheckVehiclesJob, ACS.App");
                RegisterHostedService(builder, "ACS.Scheduling.AwakeCheckServerTimeJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeCallVehicleStopWaitJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeDeleteUiInformJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeDeleteVehicleCrossWaitJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeDeleteLogJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.Awake.AwakeTruncateHistoryJob, ACS.App");
            }
        }

        private void RegisterHostedService(ContainerBuilder builder, string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                builder.RegisterType(type)
                    .As<IHostedService>()
                    .SingleInstance();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingModule] Job type not found: {typeName}");
            }
        }
    }
}
