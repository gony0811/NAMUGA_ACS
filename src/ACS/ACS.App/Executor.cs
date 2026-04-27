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

        /// <summary>
        /// 콘솔 호스트(non-UI) 진입점. 컨테이너를 자체 빌드하고 후속 초기화까지 수행.
        /// UI 프로세스는 ASP.NET Core 호스팅으로 분리되어 RegisterModules + OnContainerBuilt를 직접 호출한다.
        /// </summary>
        public IContainer Start()
        {
            try
            {
                var configuration = LoadConfiguration();
                ApplyProcessSettings(configuration);

                var builder = new ContainerBuilder();
                RegisterModules(builder, configuration);
                _container = builder.Build();

                OnContainerBuilt(_container, startHostedServices: true);
                return _container;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Executor Start() Error", e);
            }
        }

        /// <summary>
        /// appsettings.json을 로드해 IConfiguration을 만든다.
        /// dotnet 실행 시 exeDir이 SDK 경로를 가리키는 경우 CWD를 fallback으로 사용.
        /// </summary>
        public static IConfiguration LoadConfiguration()
        {
            string initialStartUpPath = Environment.CurrentDirectory;
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string exeDir = Path.GetDirectoryName(exe);

            string basePath = File.Exists(Path.Combine(exeDir, "appsettings.json"))
                ? exeDir
                : initialStartUpPath;

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
        }

        /// <summary>
        /// appsettings.json에서 프로세스 메타데이터를 읽어 Executor 인스턴스 필드에 적용.
        /// ServicePath가 지정되면 디렉토리도 생성한다.
        /// </summary>
        public void ApplyProcessSettings(IConfiguration configuration)
        {
            this.StartUpPath = configuration["Acs:Startup:Path"];

            this.Id = configuration["Acs:Process:Name"];
            if (this.Id == null)
            {
                throw new ApplicationException("process id is null");
            }

            string initialStartUpPath = Environment.CurrentDirectory;
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string exeDir = Path.GetDirectoryName(exe);
            string basePath = File.Exists(Path.Combine(exeDir, "appsettings.json"))
                ? exeDir
                : initialStartUpPath;

            if (string.IsNullOrEmpty(StartUpPath))
            {
                StartUpPath = basePath;
            }

            this.HardwareType = configuration["Acs:Process:HardwareType"];

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
        }

        /// <summary>
        /// ContainerBuilder에 ACS 공통/프로세스/사이트/DB/MSB/스케줄링 모듈을 등록한다.
        /// ASP.NET Core 호스트(UI 프로세스)는 Host.ConfigureContainer에서 이 메서드를 호출한다.
        /// </summary>
        public void RegisterModules(ContainerBuilder builder, IConfiguration configuration)
        {
            builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
            builder.RegisterInstance(this).AsSelf().SingleInstance();

            builder.RegisterModule<CoreModule>();
            RegisterProcessModule(builder, this.Type);

            string site = configuration["Acs:Site:Name"];
            RegisterSiteModule(builder, site);

            builder.RegisterModule<DatabaseModule>();

            if (!string.IsNullOrEmpty(this.Msb) && this.Msb.Equals("rabbitmq"))
            {
                builder.RegisterModule(new MsbRabbitMQModule(this.Type, configuration));
            }

            builder.RegisterModule(new SchedulingModule(this.Type));
        }

        /// <summary>
        /// 컨테이너가 빌드된 직후 수행할 후속 초기화: Elsa 브릿지, DB 마이그레이션,
        /// ApplicationInitializer 실행, Quartz 스케줄러 시작, (옵션) IHostedService 시작.
        /// ASP.NET Core 호스트(UI)는 startHostedServices=false로 호출하여 Generic Host가 IHostedService를 관리하도록 한다.
        /// </summary>
        public void OnContainerBuilt(IContainer container, bool startHostedServices)
        {
            _container = container;

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

                MigrateTransportCommandTable(dbContext);
                MigrateVehicleTable(dbContext);
                MigrateLocationTable(dbContext);
                MigrateBayTable(dbContext);
                MigrateZoneTable(dbContext);
                MigrateMqttTable(dbContext);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize database schema.");
                throw;
            }

            var initializer = _container.Resolve<ApplicationInitializer>();
            initializer.Initialize(this);

            // 스케줄러 시작 (DB 초기화 이후) — Control/EI 동적 잡용
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

            // BackgroundService 시작 (Awake 잡)
            // 콘솔 호스트는 직접 시작; ASP.NET Core 호스트(UI)는 Generic Host가 IServiceCollection으로 흡수된
            // IHostedService를 자동 시작하므로 중복 호출 방지를 위해 false로 호출.
            if (startHostedServices)
            {
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
            }

            logger.Information("{Type}({Id}) server is started.", this.Type, this.Id);
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

        /// <summary>
        /// NA_C_MQTT 테이블의 varchar 컬럼을 integer로 변환.
        /// 원격 DB에서 수동 생성 시 brokerPort, keepAliveSeconds, reconnectDelayMs, id가
        /// character varying으로 정의된 경우 EF Core 읽기 오류 방지.
        /// </summary>
        private void MigrateMqttTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    -- brokerPort varchar → integer 변환
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'brokerPort'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""brokerPort"" TYPE integer USING ""brokerPort""::integer;
        RAISE NOTICE 'NA_C_MQTT: brokerPort converted to integer';
    END IF;

    -- keepAliveSeconds varchar → integer 변환
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'keepAliveSeconds'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""keepAliveSeconds"" TYPE integer USING ""keepAliveSeconds""::integer;
        RAISE NOTICE 'NA_C_MQTT: keepAliveSeconds converted to integer';
    END IF;

    -- reconnectDelayMs varchar → integer 변환
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'reconnectDelayMs'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""reconnectDelayMs"" TYPE integer USING ""reconnectDelayMs""::integer;
        RAISE NOTICE 'NA_C_MQTT: reconnectDelayMs converted to integer';
    END IF;

    -- id varchar → integer (serial) 변환
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'id'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""id"" TYPE integer USING ""id""::integer;
        CREATE SEQUENCE IF NOT EXISTS ""NA_C_MQTT_id_seq"" OWNED BY ""NA_C_MQTT"".""id"";
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""id"" SET DEFAULT nextval('""NA_C_MQTT_id_seq""');
        PERFORM setval('""NA_C_MQTT_id_seq""', COALESCE(MAX(""id""), 0) + 1) FROM ""NA_C_MQTT"";
        RAISE NOTICE 'NA_C_MQTT: id converted to serial integer';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("MQTT table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "MQTT table migration skipped or failed (table may not exist yet).");
            }
        }

    }
}
