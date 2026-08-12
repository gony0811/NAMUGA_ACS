using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        /// Program.Main과 동일하게 AppDomain.CurrentDomain.BaseDirectory(= ACS.App.dll 폴더)를
        /// 우선 사용하여, dotnet 명령/윈도우 서비스 등으로 작업 디렉토리가 달라져도 같은 파일을 읽도록 한다.
        /// </summary>
        public static IConfiguration LoadConfiguration()
        {
            string basePath = ResolveBasePath();

            Log.ForContext("Logger", "ErrorLogger")
                .Information("Configuration base path: {Path} (appsettings.json => {File})",
                    basePath, Path.Combine(basePath, "appsettings.json"));

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }

        /// <summary>
        /// appsettings.json이 위치한 basePath를 결정한다.
        /// 1순위: AppDomain.CurrentDomain.BaseDirectory (Program.Main과 동일)
        /// 2순위: Process.MainModule 경로
        /// 3순위: Environment.CurrentDirectory
        /// </summary>
        private static string ResolveBasePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir) && File.Exists(Path.Combine(baseDir, "appsettings.json")))
                return baseDir;

            try
            {
                string exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe))
                {
                    string exeDir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(exeDir) && File.Exists(Path.Combine(exeDir, "appsettings.json")))
                        return exeDir;
                }
            }
            catch { }

            return Environment.CurrentDirectory;
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

            if (string.IsNullOrEmpty(StartUpPath))
            {
                StartUpPath = ResolveBasePath();
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
                MigrateLogMessageTable(dbContext);
                MigrateUserTable(dbContext);
                MigrateVehicleSlotTable(dbContext);
                SeedAdminUser(dbContext);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize database schema.");
                throw;
            }

            // DB 로깅(NA_L_LOGMESSAGE) 활성화: 공용 LogManager 주입 + 비동기 큐 소비자 기동.
            // DB 스키마 초기화(연결 문자열 캐싱) 직후에 수행하여, 소비자가 항상 올바른 연결로 적재하도록 한다.
            // 이 시점 이전의 startup 로그는 DefaultLogManager 미설정으로 DB 미기록(파일/콘솔에는 기록)된다.
            try
            {
                var logManager = _container.Resolve<ACS.Core.Logging.ILogManager>();
                ACS.Core.Logging.Logger.DefaultLogManager = logManager;
                logManager.Start();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize database logging (LogManager).");
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

            // DB 로깅 큐 잔여분 flush (컨테이너 Dispose 전)
            try
            {
                _container.Resolve<ACS.Core.Logging.ILogManager>().Flush();
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
                // ui 프로세스 폐지: UI 백엔드(REST/SignalR)는 control 프로세스가 겸한다.
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
DECLARE
    pk_name TEXT;
BEGIN
    -- 1. 레거시 id(varchar) → 신 스키마(SERIAL) 마이그레이션
    --    id 값이 'MQTT_CFG01' 같은 문자열이라 직접 캐스팅 불가 → NAME으로 백필 후 SERIAL 재생성
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'id'
        AND data_type = 'character varying'
    ) THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'NA_C_MQTT' AND column_name = 'NAME'
        ) THEN
            ALTER TABLE ""NA_C_MQTT"" ADD COLUMN ""NAME"" VARCHAR(64);
        END IF;

        UPDATE ""NA_C_MQTT""
           SET ""NAME"" = ""id""
         WHERE ""NAME"" IS NULL OR ""NAME"" = '';

        SELECT con.conname INTO pk_name
          FROM pg_constraint con
          JOIN pg_class    rel ON rel.oid = con.conrelid
         WHERE rel.relname = 'NA_C_MQTT' AND con.contype = 'p';
        IF pk_name IS NOT NULL THEN
            EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', 'NA_C_MQTT', pk_name);
        END IF;

        ALTER TABLE ""NA_C_MQTT"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_C_MQTT"" ADD COLUMN ""id"" SERIAL PRIMARY KEY;

        IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
             WHERE tablename = 'NA_C_MQTT' AND indexdef ILIKE '%UNIQUE%(%""NAME""%)%'
        ) THEN
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_NA_C_MQTT_NAME""
                ON ""NA_C_MQTT"" (""NAME"");
        END IF;

        RAISE NOTICE 'NA_C_MQTT migration completed: id varchar -> SERIAL PK, NAME backfilled';
    END IF;

    -- 2. 부수 컬럼 타입 정리
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'brokerPort'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""brokerPort"" TYPE integer USING ""brokerPort""::integer;
        RAISE NOTICE 'NA_C_MQTT: brokerPort converted to integer';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'keepAliveSeconds'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""keepAliveSeconds"" TYPE integer USING ""keepAliveSeconds""::integer;
        RAISE NOTICE 'NA_C_MQTT: keepAliveSeconds converted to integer';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_C_MQTT' AND column_name = 'reconnectDelayMs'
        AND data_type = 'character varying'
    ) THEN
        ALTER TABLE ""NA_C_MQTT"" ALTER COLUMN ""reconnectDelayMs"" TYPE integer USING ""reconnectDelayMs""::integer;
        RAISE NOTICE 'NA_C_MQTT: reconnectDelayMs converted to integer';
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

        /// <summary>
        /// DB 로깅용 NA_L_LOGMESSAGE / NA_L_LARGELOGMESSAGE 를 time RANGE 파티션 테이블로 보장/변환한다.
        /// - 테이블 없음: 파티션 부모 + DEFAULT 파티션 + 인덱스 생성.
        /// - 구 비파티션 테이블(relkind 'r'): 데이터 보존하며 파티션 테이블로 1회 변환(이름변경→생성→복사→삭제).
        /// - 이미 파티션(relkind 'p'): no-op.
        /// 모든 프로세스가 기동 시 동시 실행하므로 pg_advisory_xact_lock으로 변환을 직렬화한다.
        /// 보존 만료 파티션은 AwakeLogPartitionMaintenanceJob이 DROP TABLE로 제거(스캔/bloat 없음).
        /// 컬럼 정의는 docker/init/01_init_acsdb.sql 과 동일.
        /// </summary>
        /// <summary>
        /// NA_R_VEHICLE_SLOT 테이블이 없으면 생성한다 (멱등). 기존 DB 업데이트 경로 대응.
        /// docker/init/01_init_acsdb.sql 과 동일한 컬럼/타입.
        /// </summary>
        private void MigrateVehicleSlotTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c
          JOIN pg_namespace n ON n.oid = c.relnamespace
         WHERE n.nspname = 'public' AND c.relname = 'NA_R_VEHICLE_SLOT'
    ) THEN
        CREATE SEQUENCE IF NOT EXISTS public.""NA_R_VEHICLE_SLOT_id_seq"" AS bigint;
        CREATE TABLE public.""NA_R_VEHICLE_SLOT"" (
            id bigint NOT NULL DEFAULT nextval('public.""NA_R_VEHICLE_SLOT_id_seq""') PRIMARY KEY,
            ""vehicleId"" character varying(64) NOT NULL,
            ""slotNo"" integer NOT NULL,
            role character varying(10),
            state character varying(10),
            ""jobId"" character varying(256),
            phase character varying(5),
            ""updatedTime"" timestamp with time zone
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_VEHICLE_SLOT_VEH_NO""
            ON public.""NA_R_VEHICLE_SLOT"" (""vehicleId"", ""slotNo"");
        RAISE NOTICE 'NA_R_VEHICLE_SLOT table created';
    ELSE
        RAISE NOTICE 'NA_R_VEHICLE_SLOT table already exists';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("VehicleSlot table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "VehicleSlot table migration skipped or failed.");
            }
        }

        /// NA_X_USER 테이블이 없으면 생성한다 (멱등). EF Core EnsureCreated() 는 기존 DB에서 no-op 이라
        /// 신규 추가된 테이블이 자동 생성되지 않으므로 명시적 마이그레이션 필요.
        /// docker/init/01_init_acsdb.sql 의 NA_X_USER CREATE TABLE 정의와 동일한 컬럼/타입을 사용.
        /// 신규 docker 설치 경로에서는 init.sql 이 먼저 생성하므로 이 메서드는 'already exists' 만 로그.
        /// </summary>
        private void MigrateUserTable(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                const string migrationSql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c
          JOIN pg_namespace n ON n.oid = c.relnamespace
         WHERE n.nspname = 'public' AND c.relname = 'NA_X_USER'
    ) THEN
        CREATE TABLE public.""NA_X_USER"" (
            id SERIAL NOT NULL PRIMARY KEY,
            ""userId"" character varying(64) NOT NULL,
            ""passwordHash"" character varying(255),
            role character varying(20) DEFAULT 'Viewer'::character varying,
            ""mustChangePassword"" boolean NOT NULL DEFAULT false,
            ""isActive"" boolean NOT NULL DEFAULT true,
            ""lastLoginTime"" timestamp with time zone,
            description character varying(255),
            ""createTime"" timestamp with time zone,
            creator character varying(45),
            editor character varying(45),
            ""editTime"" timestamp with time zone,
            CONSTRAINT ""UQ_NA_X_USER_userId"" UNIQUE (""userId"")
        );
        RAISE NOTICE 'NA_X_USER table created';
    ELSE
        RAISE NOTICE 'NA_X_USER table already exists';
    END IF;
END $$;
";
                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.Information("User table migration check completed.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "User table migration skipped or failed.");
            }
        }

        /// <summary>
        /// NA_X_USER 테이블이 비어 있으면 기본 Admin(admin/admin) 계정을 시드한다.
        /// MustChangePassword=true 로 둬서 최초 로그인 시 UI가 비밀번호 변경을 강제한다.
        /// </summary>
        private void SeedAdminUser(ACS.Database.AcsDbContext dbContext)
        {
            try
            {
                if (dbContext.Users.Any()) return;

                var nowUtc = DateTime.UtcNow;
                dbContext.Users.Add(new ACS.Core.User.Model.User
                {
                    UserId = "admin",
                    PasswordHash = ACS.App.Web.Auth.PasswordHasher.Hash("admin"),
                    Role = ACS.Core.User.Model.User.ROLE_ADMIN,
                    MustChangePassword = true,
                    IsActive = true,
                    Description = "Initial bootstrap administrator",
                    CreateTime = nowUtc,
                    EditTime = nowUtc,
                    Creator = "SYSTEM",
                    Editor = "SYSTEM"
                });
                dbContext.SaveChanges();
                logger.Information("Seeded initial admin user (admin/admin). MustChangePassword=true.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "SeedAdminUser skipped or failed.");
            }
        }

        private void MigrateLogMessageTable(ACS.Database.AcsDbContext dbContext)
        {
            ConvertLogTableToPartitioned(
                dbContext,
                "NA_L_LOGMESSAGE",
                778811001,
                @"
    id character varying(64) NOT NULL,
    ""transactionId"" character varying(64),
    ""threadName"" character varying(64),
    ""operationName"" character varying(128),
    ""processName"" character varying(64),
    ""messageName"" character varying(64),
    ""communicationMessageName"" character varying(64),
    ""transportCommandId"" character varying(64),
    ""carrierName"" character varying(64),
    ""machineName"" character varying(64),
    ""unitName"" character varying(64),
    text character varying(4000),
    ""logLevel"" character varying(20),
    ""WorkflowLog"" boolean NOT NULL DEFAULT false,
    ""SaveToDatabase"" boolean NOT NULL DEFAULT true,
    ""partitionId"" integer NOT NULL DEFAULT 0,
    ""time"" timestamp with time zone",
                @"id, ""transactionId"", ""threadName"", ""operationName"", ""processName"", ""messageName"", ""communicationMessageName"", ""transportCommandId"", ""carrierName"", ""machineName"", ""unitName"", text, ""logLevel"", ""WorkflowLog"", ""SaveToDatabase"", ""partitionId"", ""time""",
                @"CREATE INDEX IF NOT EXISTS ""IX_NA_L_LOGMESSAGE_time"" ON public.""NA_L_LOGMESSAGE"" (""time"");");

            ConvertLogTableToPartitioned(
                dbContext,
                "NA_L_LARGELOGMESSAGE",
                778811002,
                @"
    id character varying(64) NOT NULL,
    ""logMessageId"" character varying(64),
    ""largeText"" text,
    sequence integer NOT NULL DEFAULT 0,
    ""partitionId"" integer NOT NULL DEFAULT 0,
    ""time"" timestamp with time zone",
                @"id, ""logMessageId"", ""largeText"", sequence, ""partitionId"", ""time""",
                @"CREATE INDEX IF NOT EXISTS ""IX_NA_L_LARGELOGMESSAGE_time"" ON public.""NA_L_LARGELOGMESSAGE"" (""time"");
CREATE INDEX IF NOT EXISTS ""IX_NA_L_LARGELOGMESSAGE_logMessageId"" ON public.""NA_L_LARGELOGMESSAGE"" (""logMessageId"");");
        }

        /// <summary>
        /// 단일 로그 테이블을 time RANGE 파티션 테이블로 보장/변환한다(idempotent, 동시 실행 안전).
        /// columnsDdl: 컬럼 정의(괄호 제외), columnList: 복사용 컬럼 목록, indexesDdl: 부모에 만들 인덱스.
        /// </summary>
        private void ConvertLogTableToPartitioned(
            ACS.Database.AcsDbContext dbContext,
            string table,
            long lockKey,
            string columnsDdl,
            string columnList,
            string indexesDdl)
        {
            string sql = $@"
DO $migrate$
DECLARE
    v_relkind text;
    v_mindate date;
    v_startdate date;
    v_curdate date;
BEGIN
    SET LOCAL timezone = 'UTC';
    PERFORM pg_advisory_xact_lock({lockKey});

    SELECT c.relkind::text INTO v_relkind
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '{table}';

    -- 파티션 테이블이 아니면(신규 또는 구 'r' 테이블) 부모+DEFAULT+인덱스를 만들고, 'r'이면 데이터를 보존 변환한다.
    -- 이미 'p'면 아래 일별 파티션 보장만 수행한다(매 기동마다 idempotent).
    IF v_relkind IS DISTINCT FROM 'p' THEN
        DROP TABLE IF EXISTS public.""{table}_old"";  -- 이전 중단 잔여물 정리

        IF v_relkind = 'r' THEN
            ALTER TABLE public.""{table}"" RENAME TO ""{table}_old"";
        END IF;

        CREATE TABLE public.""{table}"" ({columnsDdl}
        ) PARTITION BY RANGE (""time"");
        CREATE TABLE public.""{table}_pdefault"" PARTITION OF public.""{table}"" DEFAULT;
        {indexesDdl}

        IF v_relkind = 'r' THEN
            SELECT date_trunc('day', min(""time""))::date INTO v_mindate FROM public.""{table}_old"";
        END IF;
    END IF;

    -- 오늘..+3일(변환 시엔 기존 데이터 최소일부터) 일별 파티션 보장. 신규 DB가 'p'로 시작해도 기동 시점에 보장된다.
    v_startdate := LEAST(COALESCE(v_mindate, current_date), current_date);
    v_curdate := v_startdate;
    WHILE v_curdate <= current_date + 3 LOOP
        BEGIN
            EXECUTE format(
                'CREATE TABLE IF NOT EXISTS public.%I PARTITION OF public.%I FOR VALUES FROM (%L) TO (%L)',
                '{table}_p' || to_char(v_curdate, 'YYYYMMDD'),
                '{table}',
                v_curdate::timestamptz,
                (v_curdate + 1)::timestamptz);
        EXCEPTION WHEN others THEN
            -- DEFAULT 파티션에 해당 범위 행이 있으면 생성이 실패할 수 있다(잡 장기 미실행 등). 경고만 남기고 계속.
            RAISE WARNING 'log partition create skipped for % %: %', '{table}', v_curdate, SQLERRM;
        END;
        v_curdate := v_curdate + 1;
    END LOOP;

    -- 변환 데이터 복사 후 구 테이블 제거
    IF v_relkind = 'r' THEN
        INSERT INTO public.""{table}"" ({columnList})
        SELECT {columnList} FROM public.""{table}_old"";
        DROP TABLE public.""{table}_old"";
    END IF;
END
$migrate$;";

            try
            {
                dbContext.Database.ExecuteSqlRaw(sql);
                logger.Information("Log table partition check/conversion completed for {Table}.", table);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Log table partition conversion skipped or failed for {Table}.", table);
            }
        }

    }
}
