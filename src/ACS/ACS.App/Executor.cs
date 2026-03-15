using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.App.Modules;
using Site = ACS.App.Modules.Site;
using ACS.Core.Application;
using Serilog;

namespace ACS.App
{
    public class Executor
    {
        private ILogger logger = Log.ForContext("Logger", "ErrorLogger");
        public string Id { get; set; }
        public string StartUpPath { get; set; }
        public string Type { get; set; }
        public string HardwareType { get; set; }
        public string Msb { get; set; }
        public string BaseClass { get; set; }
        public string ServicePath { get; set; }
        public bool UseService { get; set; }


        private IContainer _container = null;

        public IContainer Start()
        {
            try
            {
                long startTime = System.DateTime.UtcNow.Millisecond;

                // IConfiguration 빌드를 먼저 수행
                string initialStartUpPath = Environment.CurrentDirectory;
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                string exeDir = Path.GetDirectoryName(exe);

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(exeDir)
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build();

                this.StartUpPath = configuration["Acs:Startup:Path"];

                this.Id = configuration["Acs:Process:Name"];
                if (this.Id == null)
                {
                    throw new ApplicationException("process id is null");
                }

                if (string.IsNullOrEmpty(StartUpPath))
                {
                    StartUpPath = exeDir;
                }

                this.HardwareType = configuration["Acs:Process:HardwareType"];

                // appsettings.json에서 프로세스 설정 로드 (startup.xml 대체)
                this.Type = configuration["Acs:Process:Type"];
                this.Msb = configuration["Acs:Process:Msb"];
                this.BaseClass = configuration["Acs:Process:Base"];
                this.ServicePath = configuration["Acs:Process:ServicePath"];

                if (string.IsNullOrEmpty(this.Type))
                {
                    throw new ApplicationException("acs.process.type is not configured in appsettings.json");
                }

                if (!string.IsNullOrEmpty(ServicePath))
                {
                    string fullPath = StartUpPath + @"/" + ServicePath;
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    this.UseService = true;
                }

                // Autofac 컨테이너 빌드
                var builder = new ContainerBuilder();

                // IConfiguration 등록
                builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

                // Executor 자신을 등록
                builder.RegisterInstance(this).AsSelf().SingleInstance();

                // 공통 모듈
                builder.RegisterModule<CoreModule>();

                // 프로세스 타입별 모듈 등록
                RegisterProcessModule(builder, this.Type);

                // 사이트별 모듈 등록
                string site = configuration["Acs:Site:Name"];
                RegisterSiteModule(builder, site);

                // DB 모듈 등록
                builder.RegisterModule<DatabaseModule>();

                // MSB(RabbitMQ) 모듈 등록
                if (!string.IsNullOrEmpty(this.Msb) && this.Msb.Equals("rabbitmq"))
                {
                    builder.RegisterModule(new MsbRabbitMQModule(this.Type, configuration));
                }

                // 스케줄링 모듈 등록 (프로세스 타입에 따라 Awake 잡 포함 여부 결정)
                builder.RegisterModule(new SchedulingModule(this.Type));

                _container = builder.Build();

                // Autofac ↔ Elsa 브릿지: Elsa Activity에서 Autofac 서비스 접근 가능하도록 설정
                var autofacAccessor = _container.ResolveOptional<ACS.Elsa.Bridge.AutofacContainerAccessor>();
                if (autofacAccessor != null)
                {
                    autofacAccessor.Container = _container;
                    logger.Information("AutofacContainerAccessor: Autofac container linked to Elsa ServiceProvider.");
                }

                // DB 스키마 생성 및 초기화
                try
                {
                    var dbContext = _container.Resolve<ACS.Database.AcsDbContext>();
                    dbContext.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to initialize database schema.");
                    throw; // DB 초기화 실패 시 실행 중단
                }

                // 초기화 실행
                var initializer = _container.Resolve<ApplicationInitializer>();
                initializer.Initialize(this);

                // 스케줄러 시작 (DB 초기화 이후에 실행) — Control/EI 동적 잡용
                try
                {
                    var scheduler = _container.Resolve<Quartz.IScheduler>();
                    if (!scheduler.IsStarted)
                    {
                        scheduler.Start().GetAwaiter().GetResult();
                        logger.Information("Scheduler started successfully.");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to start scheduler.");
                }

                // BackgroundService 시작 (Awake 잡 10개)
                try
                {
                    var hostedServices = _container.Resolve<IEnumerable<Microsoft.Extensions.Hosting.IHostedService>>();
                    foreach (var service in hostedServices)
                    {
                        service.StartAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                    }
                    logger.Information("Background services started successfully.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to start background services.");
                }

                logger.Information("{Type}({Id}) server is started.", this.Type, this.Id);
                return _container;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Executor Start() Error", e);
            }
        }

        public void Stop()
        {
            IApplicationControlManager applicationControlManager = _container.Resolve<IApplicationControlManager>();
            if (applicationControlManager.InvokeStop(this.Type, this.Id))
            {
                logger.Information("{Type}({Id}) server is stopped.", this.Type, this.Id);
            }
            else
            {
                logger.Error("{Type}({Id}) server stop is failed.", this.Type, this.Id);
            }

            // BackgroundService 종료
            try
            {
                var hostedServices = _container.Resolve<IEnumerable<Microsoft.Extensions.Hosting.IHostedService>>();
                foreach (var service in hostedServices)
                {
                    service.StopAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                }
            }
            catch { }

            _container?.Dispose();
        }

        private void RegisterProcessModule(ContainerBuilder builder, string processType)
        {
            switch (processType)
            {
                case "trans":
                    builder.RegisterModule<TransModule>();
                    break;
                case "ei":
                    builder.RegisterModule<EiModule>();
                    break;
                case "daemon":
                    builder.RegisterModule<DaemonModule>();
                    break;
                case "control":
                    builder.RegisterModule<ControlModule>();
                    break;
                case "query":
                case "report":
                    builder.RegisterModule<TransModule>();
                    break;
                case "host":
                    builder.RegisterModule<HostModule>();
                    break;
                case "ui":
                    builder.RegisterModule<UiModule>();
                    break;
                default:
                    throw new ApplicationException($"Unknown process type: {processType}");
            }
        }

        private void RegisterSiteModule(ContainerBuilder builder, string site)
        {
            if (string.IsNullOrEmpty(site)) return;

            switch (site.ToUpperInvariant())
            {
                case "V1":
                    builder.RegisterModule<Site.V1SiteModule>();
                    break;
                case "V2":
                    builder.RegisterModule<Site.V2SiteModule>();
                    break;
                case "SSM1D1F":
                    builder.RegisterModule<Site.Ssm1d1fSiteModule>();
                    break;
                case "NAMUGA":
                    builder.RegisterModule<Site.NamugaSiteModule>();
                    break;
            }
        }

    }
}
