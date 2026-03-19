using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.App.Modules;
using Site = ACS.App.Modules.Site;
using ACS.Core.Application;
using Microsoft.EntityFrameworkCore;
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

                // dotnet 명령으로 실행 시 exeDir이 SDK 경로를 가리킬 수 있으므로
                // appsettings.json이 exeDir에 없으면 CWD를 basePath로 사용
                string basePath = File.Exists(Path.Combine(exeDir, "appsettings.json"))
                    ? exeDir
                    : initialStartUpPath;

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
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
                    StartUpPath = basePath;
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

                    // 기존 DB 마이그레이션
                    MigrateTransportCommandTable(dbContext);
                    MigrateVehicleTable(dbContext);
                    MigrateLocationTable(dbContext);
                    MigrateBayTable(dbContext);
                    MigrateZoneTable(dbContext);
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

        /// <summary>
        /// 기존 NA_T_TRANSPORTCMD 테이블 마이그레이션.
        /// id(string PK) → id(bigserial PK) + jobId(varchar) 컬럼 추가.
        /// jobId 컬럼이 이미 존재하면 마이그레이션 완료된 것으로 판단하여 건너뜀.
        /// </summary>
        private void MigrateTransportCommandTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    -- jobId 컬럼이 없으면 마이그레이션 수행
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_T_TRANSPORTCMD' AND column_name = 'jobId'
    ) THEN
        -- 1. jobId 컬럼 추가 및 기존 id 값 복사
        ALTER TABLE ""NA_T_TRANSPORTCMD"" ADD COLUMN ""jobId"" VARCHAR(64);
        UPDATE ""NA_T_TRANSPORTCMD"" SET ""jobId"" = ""id"";

        -- 2. 기존 PK 제약조건 삭제
        ALTER TABLE ""NA_T_TRANSPORTCMD"" DROP CONSTRAINT IF EXISTS ""PK_NA_T_TRANSPORTCMD"";

        -- 3. 기존 id 컬럼 삭제 후 bigserial로 재생성
        ALTER TABLE ""NA_T_TRANSPORTCMD"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_T_TRANSPORTCMD"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;

        RAISE NOTICE 'NA_T_TRANSPORTCMD migration completed: id -> bigserial, jobId column added';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("TransportCommand table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "TransportCommand table migration skipped or failed (table may not exist yet).");
            }
        }

        /// <summary>
        /// 기존 NA_R_VEHICLE 테이블 마이그레이션.
        /// id(string PK) → id(bigserial PK) + vehicleId(varchar) 컬럼 추가.
        /// vehicleId 컬럼이 이미 존재하면 마이그레이션 완료된 것으로 판단하여 건너뜀.
        /// </summary>
        private void MigrateVehicleTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_R_VEHICLE' AND column_name = 'vehicleId'
    ) THEN
        ALTER TABLE ""NA_R_VEHICLE"" ADD COLUMN ""vehicleId"" VARCHAR(64);
        UPDATE ""NA_R_VEHICLE"" SET ""vehicleId"" = ""id"";

        ALTER TABLE ""NA_R_VEHICLE"" DROP CONSTRAINT IF EXISTS ""PK_NA_R_VEHICLE"";
        ALTER TABLE ""NA_R_VEHICLE"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_R_VEHICLE"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;

        RAISE NOTICE 'NA_R_VEHICLE migration completed: id -> bigserial, vehicleId column added';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("Vehicle table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Vehicle table migration skipped or failed (table may not exist yet).");
            }
        }

        /// <summary>
        /// 기존 NA_R_LOCATION 테이블 마이그레이션.
        /// portId(string PK) → id(bigserial PK) + locationId(varchar) 컬럼으로 변환.
        /// </summary>
        private void MigrateLocationTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    -- Step 1: portId → locationId 컬럼 변환 + id bigserial PK 추가
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_R_LOCATION' AND column_name = 'locationId'
    ) THEN
        ALTER TABLE ""NA_R_LOCATION"" RENAME COLUMN ""portId"" TO ""locationId"";
        ALTER TABLE ""NA_R_LOCATION"" DROP CONSTRAINT IF EXISTS ""PK_NA_R_LOCATION"";
        ALTER TABLE ""NA_R_LOCATION"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;

        RAISE NOTICE 'NA_R_LOCATION migration completed: portId -> locationId, id bigserial added';
    END IF;

    -- Step 2: locationId에 NOT NULL + UNIQUE 제약 추가
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_name = 'NA_R_LOCATION' AND constraint_name = 'uq_location_locationId'
    ) THEN
        UPDATE ""NA_R_LOCATION"" SET ""locationId"" = 'UNKNOWN_' || ""id"" WHERE ""locationId"" IS NULL;
        ALTER TABLE ""NA_R_LOCATION"" ALTER COLUMN ""locationId"" SET NOT NULL;
        ALTER TABLE ""NA_R_LOCATION"" ADD CONSTRAINT ""uq_location_locationId"" UNIQUE (""locationId"");

        RAISE NOTICE 'NA_R_LOCATION: locationId NOT NULL + UNIQUE constraint added';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("Location table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Location table migration skipped or failed (table may not exist yet).");
            }
        }

        /// <summary>
        /// 기존 NA_R_BAY 테이블 마이그레이션.
        /// id(string PK) → id(bigserial PK) + bayId(varchar) 컬럼 추가.
        /// </summary>
        private void MigrateBayTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_R_BAY' AND column_name = 'bayId'
    ) THEN
        ALTER TABLE ""NA_R_BAY"" ADD COLUMN ""bayId"" VARCHAR(64);
        UPDATE ""NA_R_BAY"" SET ""bayId"" = ""id"";

        ALTER TABLE ""NA_R_BAY"" DROP CONSTRAINT IF EXISTS ""PK_NA_R_BAY"";
        ALTER TABLE ""NA_R_BAY"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_R_BAY"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;

        RAISE NOTICE 'NA_R_BAY migration completed: id -> bigserial, bayId column added';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("Bay table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Bay table migration skipped or failed (table may not exist yet).");
            }
        }

        /// <summary>
        /// 기존 NA_R_ZONE 테이블 마이그레이션.
        /// id(string PK) → id(bigserial PK) + zoneId(varchar) 컬럼 추가.
        /// </summary>
        private void MigrateZoneTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_R_ZONE' AND column_name = 'zoneId'
    ) THEN
        ALTER TABLE ""NA_R_ZONE"" ADD COLUMN ""zoneId"" VARCHAR(64);
        UPDATE ""NA_R_ZONE"" SET ""zoneId"" = ""id"";

        ALTER TABLE ""NA_R_ZONE"" DROP CONSTRAINT IF EXISTS ""PK_NA_R_ZONE"";
        ALTER TABLE ""NA_R_ZONE"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_R_ZONE"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;

        RAISE NOTICE 'NA_R_ZONE migration completed: id -> bigserial, zoneId column added';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("Zone table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Zone table migration skipped or failed (table may not exist yet).");
            }
        }

    }
}
