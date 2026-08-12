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

            // 파일 로그 정리: 모든 프로세스가 각자 logs/ 폴더를 보존 기간(Acs:LogDeleteDays, 기본 7일)으로 정리.
            // (각 프로세스는 자기 작업 디렉터리의 logs/만 정리하므로 daemon 전용 블록 밖에서 전 프로세스에 등록한다.)
            RegisterHostedService(builder, "ACS.Scheduling.AwakeDeleteLogJob, ACS.App");

            // DB 로그 파티션 유지보수(일별 파티션 사전 생성 + 만료 파티션 DROP): 공유 DB이므로 단일 소유자에서만 실행.
            // 상시 가동·로그 뷰어를 호스팅하는 control 프로세스에 등록.
            if (string.Equals(_processType, "control", StringComparison.OrdinalIgnoreCase))
            {
                RegisterHostedService(builder, "ACS.Scheduling.Awake.AwakeLogPartitionMaintenanceJob, ACS.App");
            }

            // Awake 잡 10개: daemon 프로세스에서만 등록
            if (string.Equals(_processType, "daemon", StringComparison.OrdinalIgnoreCase))
            {
                RegisterHostedService(builder, "ACS.Scheduling.AwakeChargeTransportJob, ACS.App");
                RegisterHostedService(builder, "ACS.Scheduling.AwakeQueueTransportJob, ACS.App");
                RegisterHostedService(builder, "ACS.Scheduling.AwakeExchangeTransportJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeCheckCrossNodeJob, ACS.App");
                RegisterHostedService(builder, "ACS.Scheduling.AwakeCheckVehiclesJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeCheckServerTimeJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeCallVehicleStopWaitJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeDeleteUiInformJob, ACS.App");
                //RegisterHostedService(builder, "ACS.Scheduling.AwakeDeleteVehicleCrossWaitJob, ACS.App");
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
