using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Path domain
using ACS.Core.Path.Model;
// Resource domain
using ACS.Core.Resource.Model;
// Transfer domain
using ACS.Core.Transfer.Model;
// History domain
using ACS.Core.History.Model;
// Application domain
using AppModel = ACS.Core.Application.Model;
// Alarm domain
using ACS.Core.Alarm.Model;
// Logging domain
using ACS.Core.Logging.Model;
// Material domain
using ACS.Core.Material.Model;
// Communication domain
using ACS.Communication.Socket.Model;
using ACS.Communication.Mqtt.Model;

namespace ACS.Database
{
    /// <summary>
    /// EF Core DbContext - NHibernate SessionFactory + 44개 HBM 매핑 대체.
    /// 모든 엔티티는 assigned ID 전략 (application-managed) 사용.
    /// </summary>
    public class AcsDbContext : DbContext
    {
        // ===== Path Domain =====
        public DbSet<StationEx> Stations { get; set; }
        public DbSet<NodeEx> Nodes { get; set; }
        public DbSet<LinkEx> Links { get; set; }
        public DbSet<LocationViewEx> LocationViews { get; set; }
        public DbSet<LinkViewEx> LinkViews { get; set; }
        public DbSet<StationViewEx> StationViews { get; set; }
        public DbSet<InterSectionControlEx> InterSectionControls { get; set; }
        public DbSet<CurrentInterSectionInfoEx> CurrentInterSectionInfos { get; set; }
        public DbSet<WaitPointViewEx> WaitPointViews { get; set; }

        // ===== Resource Domain =====
        public DbSet<VehicleExs> Vehicles { get; set; }
        public DbSet<LocationEx> Locations { get; set; }
        public DbSet<BayEx> Bays { get; set; }
        public DbSet<ZoneEx> Zones { get; set; }
        public DbSet<LinkZoneEx> LinkZones { get; set; }
        public DbSet<VehicleCrossWaitEx> VehicleCrossWaits { get; set; }
        public DbSet<Inform> Informs { get; set; }
        public DbSet<SpecialConfig> SpecialConfigs { get; set; }
        public DbSet<OrderPairNodeEx> OrderPairNodes { get; set; }
        public DbSet<VehicleIdleEx> VehicleIdles { get; set; }
        public DbSet<VehicleOrderEx> VehicleOrders { get; set; }

        // ===== Transfer Domain =====
        public DbSet<TransportCommandEx> TransportCommands { get; set; }
        public DbSet<TransportCommandRequestEx> TransportCommandRequests { get; set; }
        public DbSet<UiTransport> UiTransports { get; set; }
        public DbSet<UiCommand> UiCommands { get; set; }

        // ===== History Domain =====
        public DbSet<TransportCommandHistoryEx> TransportCommandHistories { get; set; }
        public DbSet<VehicleHistoryEx> VehicleHistories { get; set; }
        public DbSet<VehicleBatteryHistoryEx> VehicleBatteryHistories { get; set; }
        public DbSet<HeartBeatFailHistoryEx> HeartBeatFailHistories { get; set; }
        public DbSet<AlarmReportHistoryEx> AlarmReportHistories { get; set; }
        public DbSet<AlarmTimeHistoryEx> AlarmTimeHistories { get; set; }
        public DbSet<NioHistory> NioHistories { get; set; }
        public DbSet<MismatchAndFlyHistoryEx> MismatchAndFlyHistories { get; set; }
        public DbSet<VehicleCrossWaitHistoryEx> VehicleCrossWaitHistories { get; set; }
        public DbSet<VehicleSearchPathHistory> VehicleSearchPathHistories { get; set; }

        // ===== Application Domain =====
        public DbSet<AppModel.Application> Applications { get; set; }
        public DbSet<AppModel.UiApplicationManager> UiApplicationManagers { get; set; }
        public DbSet<AppModel.Option> Options { get; set; }

        // ===== Alarm Domain =====
        public DbSet<AlarmEx> Alarms { get; set; }
        public DbSet<AlarmSpecEx> AlarmSpecs { get; set; }

        // ===== Logging Domain =====
        public DbSet<LogMessage> LogMessages { get; set; }
        public DbSet<LargeLogMessage> LargeLogMessages { get; set; }

        // ===== Material Domain =====
        public DbSet<CarrierEx> Carriers { get; set; }

        // ===== Communication Domain =====
        public DbSet<Nio> Nios { get; set; }
        public DbSet<MqttConfig> MqttConfigs { get; set; }

        private readonly IConfiguration _configuration;

        /// <summary>
        /// 앱 시작 시 설정된 연결 문자열을 캐싱하여
        /// 파라미터 없는 생성자(새 스레드)에서도 올바른 DB에 연결되도록 한다.
        /// </summary>
        private static string _cachedConnectionString;

        public AcsDbContext() { }

        public AcsDbContext(DbContextOptions<AcsDbContext> options) : base(options) { }

        public AcsDbContext(DbContextOptions<AcsDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;

            // 최초 생성 시 connection string을 캐싱
            if (_cachedConnectionString == null)
            {
                var connStr = configuration?.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connStr))
                {
                    _cachedConnectionString = connStr;
                }
            }
        }

        private static readonly string DefaultConnectionString =
            "Host=localhost;Port=5432;Database=acsdb;Username=postgres;Password=1234";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = null;
                try
                {
                    connectionString = _configuration?.GetConnectionString("DefaultConnection");
                }
                catch { }

                optionsBuilder.UseNpgsql(connectionString ?? _cachedConnectionString ?? DefaultConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Path Domain =====
            modelBuilder.Entity<StationEx>(e =>
            {
                e.ToTable("NA_R_STATION");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.LinkId).HasColumnName("linkId").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.Distance).HasColumnName("distance");
            });

            modelBuilder.Entity<NodeEx>(e =>
            {
                e.ToTable("NA_R_NODE");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id")
                    .ValueGeneratedOnAdd();
                e.Property(x => x.NodeId).HasColumnName("node_id").HasMaxLength(64);
                e.HasIndex(x => x.NodeId).IsUnique();
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.Xpos).HasColumnName("xpos");
                e.Property(x => x.Ypos).HasColumnName("ypos");
                e.Property(x => x.Zpos).HasColumnName("zpos");
            });

            modelBuilder.Entity<LinkEx>(e =>
            {
                e.ToTable("NA_R_LINK");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.FromNodeId).HasColumnName("fromNodeId").HasMaxLength(64);
                e.Property(x => x.ToNodeId).HasColumnName("toNodeId").HasMaxLength(64);
                e.Property(x => x.Length).HasColumnName("length");
                e.Property(x => x.Speed).HasColumnName("speed");
                e.Property(x => x.LeftBranch).HasColumnName("leftBranch").HasMaxLength(20);
                e.Property(x => x.Availability).HasColumnName("availability").HasMaxLength(20);
                e.Property(x => x.Load).HasColumnName("load").HasMaxLength(20);
                e.Property(x => x.AgvType).HasColumnName("agvType").HasMaxLength(20);
            });

            modelBuilder.Entity<LocationViewEx>(e =>
            {
                e.ToView("NA_R_LOCATION_VW");
                e.HasKey(x => x.LocationId);
                e.Property(x => x.LocationId).HasColumnName("portId").HasMaxLength(64);
                e.Property(x => x.StationId).HasColumnName("stationId").HasMaxLength(64);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.Location_Type).HasColumnName("location_Type").HasMaxLength(8);
                e.Property(x => x.CarrierType).HasColumnName("carrierType").HasMaxLength(8);
                e.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(8);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.LinkId).HasColumnName("linkId").HasMaxLength(64);
                e.Property(x => x.ParentNode).HasColumnName("parentNode").HasMaxLength(64);
                e.Property(x => x.Station_type).HasColumnName("station_type").HasMaxLength(20);
                e.Property(x => x.Distance).HasColumnName("distance");
                e.Property(x => x.From_xpos).HasColumnName("from_xpos");
                e.Property(x => x.From_ypos).HasColumnName("from_ypos");
                e.Property(x => x.To_xpos).HasColumnName("to_xpos");
                e.Property(x => x.To_ypos).HasColumnName("to_ypos");
                e.Property(x => x.Length).HasColumnName("length");
                e.Property(x => x.LeftBranch).HasColumnName("leftBranch").HasMaxLength(20);
                e.Property(x => x.Availability).HasColumnName("availability").HasMaxLength(20);
                e.Property(x => x.Load).HasColumnName("load").HasMaxLength(20);
                e.Property(x => x.TransferFlag).HasColumnName("transferFlag").HasMaxLength(20);
            });

            modelBuilder.Entity<LinkViewEx>(e =>
            {
                e.ToView("NA_R_LINK_VW");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.ZoneId).HasColumnName("zoneId").HasMaxLength(64);
                e.Property(x => x.TransferFlag).HasColumnName("transferFlag").HasMaxLength(20);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.FromNodeId).HasColumnName("fromNodeId").HasMaxLength(64);
                e.Property(x => x.From_xpos).HasColumnName("from_xpos");
                e.Property(x => x.From_ypos).HasColumnName("from_ypos");
                e.Property(x => x.ToNodeId).HasColumnName("toNodeId").HasMaxLength(64);
                e.Property(x => x.To_xpos).HasColumnName("to_xpos");
                e.Property(x => x.To_ypos).HasColumnName("to_ypos");
                e.Property(x => x.Length).HasColumnName("length");
                e.Property(x => x.Speed).HasColumnName("speed");
                e.Property(x => x.LeftBranch).HasColumnName("leftBranch").HasMaxLength(20);
                e.Property(x => x.Availability).HasColumnName("availability").HasMaxLength(20);
                e.Property(x => x.Load).HasColumnName("load").HasMaxLength(20);
                e.Property(x => x.Loading).HasColumnName("loading").HasMaxLength(20);
            });

            modelBuilder.Entity<StationViewEx>(e =>
            {
                e.ToView("NA_R_STATION_VW");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.LinkId).HasColumnName("linkId").HasMaxLength(64);
                e.Property(x => x.ParentNode).HasColumnName("parentNode").HasMaxLength(64);
                e.Property(x => x.NextNode).HasColumnName("nextNode").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.Distance).HasColumnName("distance");
            });

            modelBuilder.Entity<InterSectionControlEx>(e =>
            {
                e.ToTable("NA_T_INTERSECTION");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.InterSectionId).HasColumnName("interSectionId").HasMaxLength(64);
                e.Property(x => x.StartNodeId).HasColumnName("startNodeId").HasMaxLength(64);
                e.Property(x => x.EndNodeId).HasColumnName("endNodeId").HasMaxLength(64);
                e.Property(x => x.Interval).HasColumnName("interval");
                e.Property(x => x.Sequence).HasColumnName("sequence");
                e.Property(x => x.CheckPreviousNode).HasColumnName("checkPreviousNode").HasMaxLength(20);
                e.Property(x => x.PreviousNodeIds).HasColumnName("previousNodeIds").HasMaxLength(2000);
                e.Property(x => x.CheckNodeIds).HasColumnName("checkNodeIds").HasMaxLength(2000);
            });

            modelBuilder.Entity<CurrentInterSectionInfoEx>(e =>
            {
                e.ToTable("NA_T_CURRENTINTERSECTION");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.CurrentDirectionNode).HasColumnName("currentDirectionNode").HasMaxLength(64);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.ChangedTime).HasColumnName("changedTime");
            });

            modelBuilder.Entity<WaitPointViewEx>(e =>
            {
                e.ToView("NA_R_WAITP_VW");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.ZoneId).HasColumnName("zoneId").HasMaxLength(64);
            });

            // ===== Resource Domain =====
            modelBuilder.Entity<VehicleExs>(e =>
            {
                e.ToTable("NA_R_VEHICLE");
                e.HasKey(x => x.Seq);
                e.Property(x => x.Seq).HasColumnName("id").ValueGeneratedOnAdd();
                e.Ignore(x => x.Id); // Entity 베이스의 string Id — DB 매핑 제외
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.CommType).HasColumnName("COMMTYPE").HasMaxLength(10);
                e.Property(x => x.CommId).HasColumnName("COMMID").HasMaxLength(64);
                e.Property(x => x.Vendor).HasColumnName("vendor").HasMaxLength(32);
                e.Property(x => x.Version).HasColumnName("version").HasMaxLength(32);
                e.Property(x => x.PlcVersion).HasColumnName("plcVersion").HasMaxLength(32);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.CarrierType).HasColumnName("carrierType").HasMaxLength(8);
                e.Property(x => x.ConnectionState).HasColumnName("connectionState").HasMaxLength(16);
                e.Property(x => x.AlarmState).HasColumnName("alarmState").HasMaxLength(8);
                e.Property(x => x.ProcessingState).HasColumnName("processingState").HasMaxLength(20);
                e.Property(x => x.BatteryRate).HasColumnName("batteryRate");
                e.Property(x => x.BatteryVoltage).HasColumnName("batteryVoltage");
                e.Property(x => x.CurrentNodeId).HasColumnName("currentNodeId").HasMaxLength(64);
                e.Property(x => x.TransportCommandId).HasColumnName("transportCommandId").HasMaxLength(64);
                e.Property(x => x.Path).HasColumnName("path").HasMaxLength(2000);
                e.Property(x => x.NodeCheckTime).HasColumnName("nodeCheckTime");
                e.Property(x => x.EventTime).HasColumnName("eventTime");
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.Installed).HasColumnName("installed").HasMaxLength(20);
                e.Property(x => x.TransferState).HasColumnName("transferState").HasMaxLength(20);
                e.Property(x => x.RunState).HasColumnName("runState").HasMaxLength(10);
                e.Property(x => x.FullState).HasColumnName("fullState").HasMaxLength(10);
                e.Property(x => x.AcsDestNodeId).HasColumnName("acsDestNodeId").HasMaxLength(64);
                e.Property(x => x.VehicleDestNodeId).HasColumnName("vehicleDestNodeId").HasMaxLength(64);
                e.Property(x => x.LastChargeTime).HasColumnName("LASTCHARGETIME");
                e.Property(x => x.LastChargeBattery).HasColumnName("LASTCHARGEBATTERY");
            });

            modelBuilder.Entity<LocationEx>(e =>
            {
                e.ToTable("NA_R_LOCATION");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.LocationId).HasColumnName("locationId").HasMaxLength(64).IsRequired();
                e.HasIndex(x => x.LocationId).IsUnique();
                e.Property(x => x.StationId).HasColumnName("stationId").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(8);
                e.Property(x => x.CarrierType).HasColumnName("carrierType").HasMaxLength(8);
                e.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(8);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
            });

            modelBuilder.Entity<BayEx>(e =>
            {
                e.ToTable("NA_R_BAY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.Floor).HasColumnName("floor").HasMaxLength(20);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
                e.Property(x => x.AgvType).HasColumnName("agvType").HasMaxLength(20);
                e.Property(x => x.ChargeVoltage).HasColumnName("chargeVoltage");
                e.Property(x => x.LimitVoltage).HasColumnName("limitVoltage");
                e.Property(x => x.IdleTime).HasColumnName("idleTime");
            });

            modelBuilder.Entity<ZoneEx>(e =>
            {
                e.ToTable("NA_R_ZONE");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.ZoneId).HasColumnName("zoneId").HasMaxLength(64);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            });

            modelBuilder.Entity<LinkZoneEx>(e =>
            {
                e.ToTable("NA_R_LINK_ZONE");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.LinkId).HasColumnName("linkId").HasMaxLength(64);
                e.Property(x => x.ZoneId).HasColumnName("zoneId").HasMaxLength(64);
                e.Property(x => x.TransferFlag).HasColumnName("transferFlag").HasMaxLength(20);
            });

            modelBuilder.Entity<VehicleCrossWaitEx>(e =>
            {
                e.ToTable("NA_R_VEHICLE_CROSS_WAIT");
                e.HasKey(x => x.VehicleId);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.NodeId).HasColumnName("nodeId").HasMaxLength(64);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.CreatedTime).HasColumnName("createdTime");
            });

            modelBuilder.Entity<Inform>(e =>
            {
                e.ToTable("NA_U_INFORM");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.Message).HasColumnName("message").HasMaxLength(255);
                e.Property(x => x.Source).HasColumnName("source").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            });

            modelBuilder.Entity<SpecialConfig>(e =>
            {
                e.ToTable("NA_R_SPECIALCONFIG");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("ID").HasMaxLength(20);
                e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(64);
                e.Property(x => x.Values).HasColumnName("VALUES").HasMaxLength(255);
            });

            modelBuilder.Entity<OrderPairNodeEx>(e =>
            {
                e.ToTable("NA_R_ORDER_PAIR");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.OrderGroup).HasColumnName("orderGroup").HasMaxLength(64);
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            });

            modelBuilder.Entity<VehicleIdleEx>(e =>
            {
                e.ToTable("NA_R_VEHICLE_IDLE");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.IdleTime).HasColumnName("idleTime");
            });

            modelBuilder.Entity<VehicleOrderEx>(e =>
            {
                e.ToTable("NA_R_VEHICLE_ORDER");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.OrderNode).HasColumnName("orderNode").HasMaxLength(64);
                e.Property(x => x.OrderTime).HasColumnName("orderTime");
            });

            // ===== Transfer Domain =====
            modelBuilder.Entity<TransportCommandEx>(e =>
            {
                e.ToTable("NA_T_TRANSPORTCMD");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.JobId).HasColumnName("jobId").HasMaxLength(64);
                e.Property(x => x.Priority).HasColumnName("priority");
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.VehicleEvent).HasColumnName("vehicleEvent").HasMaxLength(64);
                e.Property(x => x.CarrierId).HasColumnName("carrierId").HasMaxLength(64);
                e.Property(x => x.Source).HasColumnName("source").HasMaxLength(64);
                e.Property(x => x.Dest).HasColumnName("dest").HasMaxLength(64);
                e.Property(x => x.Path).HasColumnName("path").HasMaxLength(2000);
                e.Property(x => x.AdditionalInfo).HasColumnName("additionalInfo").HasMaxLength(1000);
                e.Property(x => x.CreateTime).HasColumnName("createdTime");
                e.Property(x => x.StartedTime).HasColumnName("startedTime");
                e.Property(x => x.QueuedTime).HasColumnName("queuedTime");
                e.Property(x => x.AssignedTime).HasColumnName("assignedTime");
                e.Property(x => x.LoadArrivedTime).HasColumnName("loadArrivedTime");
                e.Property(x => x.LoadedTime).HasColumnName("loadedTime");
                e.Property(x => x.UnloadArrivedTime).HasColumnName("unloadArrivedTime");
                e.Property(x => x.UnloadedTime).HasColumnName("unloadedTime");
                e.Property(x => x.CompletedTime).HasColumnName("completedTime");
                e.Property(x => x.LoadingTime).HasColumnName("loadingTime");
                e.Property(x => x.UnloadingTime).HasColumnName("unloadingTime");
                e.Property(x => x.EqpId).HasColumnName("eqpId").HasMaxLength(64);
                e.Property(x => x.PortId).HasColumnName("portId").HasMaxLength(64);
                e.Property(x => x.AgvName).HasColumnName("agvName").HasMaxLength(64);
                e.Property(x => x.JobType).HasColumnName("jobType").HasMaxLength(64);
                e.Property(x => x.MidLoc).HasColumnName("midLoc").HasMaxLength(64);
                e.Property(x => x.MidPortId).HasColumnName("midPortId").HasMaxLength(64);
                e.Property(x => x.OriginLoc).HasColumnName("originLoc").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
            });

            modelBuilder.Entity<TransportCommandRequestEx>(e =>
            {
                e.ToTable("NA_Q_TRANSPORTCMDREQUEST");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.MessageName).HasColumnName("messageName").HasMaxLength(64);
                e.Property(x => x.JobId).HasColumnName("jobId").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.Dest).HasColumnName("dest").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
            });

            modelBuilder.Entity<UiTransport>(e =>
            {
                e.ToTable("NA_U_TRANSPORT");
                e.HasKey(x => x.ID);
                e.Property(x => x.ID).HasColumnName("ID").HasMaxLength(64);
                e.Property(x => x.MESSAGENAME).HasColumnName("MESSAGENAME").HasMaxLength(64);
                e.Property(x => x.TRANSPORTCOMMANDID).HasColumnName("TRANSPORTCOMMANDID").HasMaxLength(64);
                e.Property(x => x.SOURCEPORTID).HasColumnName("SOURCEPORTID").HasMaxLength(64);
                e.Property(x => x.DESTPORTID).HasColumnName("DESTPORTID").HasMaxLength(64);
                e.Property(x => x.VEHICLEID).HasColumnName("VEHICLEID").HasMaxLength(64);
                e.Property(x => x.DESTNODEID).HasColumnName("DESTNODEID").HasMaxLength(64);
                e.Property(x => x.REQUESTID).HasColumnName("REQUESTID").HasMaxLength(64);
                e.Property(x => x.USERID).HasColumnName("USERID").HasMaxLength(64);
                e.Property(x => x.CAUSE).HasColumnName("CAUSE").HasMaxLength(255);
                e.Property(x => x.DESCRIPTION).HasColumnName("DESCRIPTION").HasMaxLength(255);
                e.Property(x => x.TIME).HasColumnName("TIME");
            });

            modelBuilder.Entity<UiCommand>(e =>
            {
                e.ToTable("NA_U_COMMAND");
                e.Ignore(x => x.Id);
                e.Ignore(x => x.Description);
                e.HasKey(x => x.ID);
                e.Property(x => x.ID).HasColumnName("ID").HasMaxLength(64);
                e.Property(x => x.MESSAGENAME).HasColumnName("MESSAGENAME").HasMaxLength(64);
                e.Property(x => x.APPLICATIONNAME).HasColumnName("APPLICATIONNAME").HasMaxLength(64);
                e.Property(x => x.APPLICATIONTYPE).HasColumnName("APPLICATIONTYPE").HasMaxLength(64);
                e.Property(x => x.USERID).HasColumnName("USERID").HasMaxLength(64);
                e.Property(x => x.CAUSE).HasColumnName("CAUSE").HasMaxLength(255);
                e.Property(x => x.DESCRIPTION).HasColumnName("DESCRIPTION").HasMaxLength(255);
                e.Property(x => x.TIME).HasColumnName("TIME");
            });

            // ===== History Domain =====
            modelBuilder.Entity<TransportCommandHistoryEx>(e =>
            {
                e.ToTable("NA_H_TRANSPORTCMDHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.JobId).HasColumnName("jobId").HasMaxLength(64);
                e.Property(x => x.Priority).HasColumnName("priority");
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.VehicleEvent).HasColumnName("vehicleEvent").HasMaxLength(64);
                e.Property(x => x.CarrierId).HasColumnName("carrierId").HasMaxLength(64);
                e.Property(x => x.Source).HasColumnName("source").HasMaxLength(64);
                e.Property(x => x.Dest).HasColumnName("dest").HasMaxLength(64);
                e.Property(x => x.Path).HasColumnName("path").HasMaxLength(2000);
                e.Property(x => x.AdditionalInfo).HasColumnName("additionalInfo").HasMaxLength(1000);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.QueuedTime).HasColumnName("queuedTime");
                e.Property(x => x.AssignedTime).HasColumnName("assignedTime");
                e.Property(x => x.StartedTime).HasColumnName("startedTime");
                e.Property(x => x.LoadArrivedTime).HasColumnName("loadArrivedTime");
                e.Property(x => x.LoadedTime).HasColumnName("loadedTime");
                e.Property(x => x.UnloadArrivedTime).HasColumnName("unloadArrivedTime");
                e.Property(x => x.UnloadedTime).HasColumnName("unloadedTime");
                e.Property(x => x.CompletedTime).HasColumnName("completedTime");
                e.Property(x => x.LoadingTime).HasColumnName("loadingTime");
                e.Property(x => x.UnloadingTime).HasColumnName("unloadingTime");
                e.Property(x => x.EqpId).HasColumnName("eqpId").HasMaxLength(64);
                e.Property(x => x.PortId).HasColumnName("portId").HasMaxLength(64);
                e.Property(x => x.AgvName).HasColumnName("agvName").HasMaxLength(64);
                e.Property(x => x.JobType).HasColumnName("jobType").HasMaxLength(64);
                e.Property(x => x.MidLoc).HasColumnName("midLoc").HasMaxLength(64);
                e.Property(x => x.MidPortId).HasColumnName("midPortId").HasMaxLength(64);
                e.Property(x => x.OriginLoc).HasColumnName("originLoc").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(255);
                e.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            });

            modelBuilder.Entity<VehicleHistoryEx>(e =>
            {
                e.ToTable("NA_H_VEHICLEHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.CarrierType).HasColumnName("carrierType").HasMaxLength(8);
                e.Property(x => x.ConnectionState).HasColumnName("connectionState").HasMaxLength(16);
                e.Property(x => x.AlarmState).HasColumnName("alarmState").HasMaxLength(8);
                e.Property(x => x.ProcessingState).HasColumnName("processingState").HasMaxLength(20);
                e.Property(x => x.CurrentNodeId).HasColumnName("currentNodeId").HasMaxLength(64);
                e.Property(x => x.TransportCommandId).HasColumnName("transportCommandId").HasMaxLength(64);
                e.Property(x => x.Path).HasColumnName("path").HasMaxLength(2000);
                e.Property(x => x.NodeCheckTime).HasColumnName("nodeCheckTime");
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.Installed).HasColumnName("installed").HasMaxLength(20);
                e.Property(x => x.TransferState).HasColumnName("transferState").HasMaxLength(20);
                e.Property(x => x.RunState).HasColumnName("runState").HasMaxLength(10);
                e.Property(x => x.FullState).HasColumnName("fullState").HasMaxLength(10);
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.MessageName).HasColumnName("messageName").HasMaxLength(64);
                e.Property(x => x.AcsDestNodeId).HasColumnName("acsDestNodeId").HasMaxLength(64);
                e.Property(x => x.VehicleDestNodeId).HasColumnName("vehicleDestNodeId").HasMaxLength(64);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
            });

            modelBuilder.Entity<VehicleBatteryHistoryEx>(e =>
            {
                e.ToTable("NA_H_VEHICLE_BATTERYHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.BatteryRate).HasColumnName("batteryRate");
                e.Property(x => x.BatteryVoltage).HasColumnName("batteryVoltage");
                e.Property(x => x.ProcessingState).HasColumnName("processingState").HasMaxLength(20);
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
            });

            modelBuilder.Entity<HeartBeatFailHistoryEx>(e =>
            {
                e.ToTable("NA_H_HEARTBEATFAILHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.ApplicationName).HasColumnName("applicationName").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.StartTime).HasColumnName("startTime");
                e.Property(x => x.InitialHardware).HasColumnName("initialHardware").HasMaxLength(64);
                e.Property(x => x.RunningHardware).HasColumnName("runningHardware").HasMaxLength(64);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
            });

            modelBuilder.Entity<AlarmReportHistoryEx>(e =>
            {
                e.ToTable("NA_H_ALARMRPTHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.AlarmCode).HasColumnName("alarmCode").HasMaxLength(64);
                e.Property(x => x.AlarmText).HasColumnName("alarmText").HasMaxLength(255);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.AlarmId).HasColumnName("alarmId").HasMaxLength(64);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.TransportCommandId).HasColumnName("transportCommandId").HasMaxLength(64);
            });

            modelBuilder.Entity<AlarmTimeHistoryEx>(e =>
            {
                e.ToTable("NA_H_ALARMTIMEHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.AlarmCode).HasColumnName("alarmCode").HasMaxLength(64);
                e.Property(x => x.AlarmText).HasColumnName("alarmText").HasMaxLength(255);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.AlarmId).HasColumnName("alarmId").HasMaxLength(64);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.ClearTime).HasColumnName("clearTime");
                e.Property(x => x.TransportCommandId).HasColumnName("transportCommandId").HasMaxLength(64);
                e.Property(x => x.NearAgv).HasColumnName("nearAgv").HasMaxLength(64);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.IsCross).HasColumnName("isCross").HasMaxLength(20);
            });

            modelBuilder.Entity<NioHistory>(e =>
            {
                e.ToTable("NA_H_NIOHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.Name).HasColumnName("name").HasMaxLength(64);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.MachineName).HasColumnName("machineName").HasMaxLength(64);
                e.Property(x => x.RemoteIp).HasColumnName("remoteIp").HasMaxLength(64);
                e.Property(x => x.Port).HasColumnName("port");
                e.Property(x => x.ApplicationName).HasColumnName("applicationName").HasMaxLength(64);
                e.Property(x => x.Location).HasColumnName("location").HasMaxLength(255);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
            });

            modelBuilder.Entity<MismatchAndFlyHistoryEx>(e =>
            {
                e.ToTable("NA_H_MISSMATCHANDFLYHISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.CurrentNodeId).HasColumnName("currentNodeId").HasMaxLength(64);
                e.Property(x => x.NgNode).HasColumnName("ngNode").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
            });

            modelBuilder.Entity<VehicleCrossWaitHistoryEx>(e =>
            {
                e.ToTable("NA_H_CROSSWAIT_HISTORY");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.NodeId).HasColumnName("nodeId").HasMaxLength(64);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.PermitTime).HasColumnName("permitTime");
                e.Property(x => x.CrossWaitSeconds).HasColumnName("crossWaitSeconds");
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
            });

            modelBuilder.Entity<VehicleSearchPathHistory>(e =>
            {
                e.ToTable("NA_H_VEHICLESEARCHPATH");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.BayId).HasColumnName("bayId").HasMaxLength(64);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.CurrentNodeId).HasColumnName("currentNodeId").HasMaxLength(64);
                e.Property(x => x.Path).HasColumnName("path").HasMaxLength(2000);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
            });

            // ===== Application Domain =====
            modelBuilder.Entity<AppModel.Application>(e =>
            {
                e.ToTable("NA_X_APPLICATION");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.StartTime).HasColumnName("startTime");
                e.Property(x => x.CheckTime).HasColumnName("checkTime");
                e.Property(x => x.InitialHardware).HasColumnName("initialHardware").HasMaxLength(64);
                e.Property(x => x.RunningHardware).HasColumnName("runningHardware").HasMaxLength(64);
                e.Property(x => x.RunningHardwareAddress).HasColumnName("runningHardwareAddress").HasMaxLength(64);
                e.Property(x => x.Msb).HasColumnName("msb").HasMaxLength(20);
                e.Property(x => x.Memory).HasColumnName("memory").HasMaxLength(64);
                e.Property(x => x.Jmx).HasColumnName("jmx").HasMaxLength(20);
                e.Property(x => x.DestinationName).HasColumnName("destinationName").HasMaxLength(64);
                e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.Creator).HasColumnName("creator").HasMaxLength(45);
                e.Property(x => x.Editor).HasColumnName("editor").HasMaxLength(45);
                e.Property(x => x.EditTime).HasColumnName("editTime");
            });

            modelBuilder.Entity<AppModel.UiApplicationManager>(e =>
            {
                e.ToTable("NA_X_APPLICATION_MANAGER");
                e.HasKey(x => x.ID);
                e.Property(x => x.ID).HasColumnName("ID").HasMaxLength(64);
                e.Property(x => x.TYPE).HasColumnName("TYPE").HasMaxLength(20);
                e.Property(x => x.COMMAND).HasColumnName("COMMAND").HasMaxLength(64);
                e.Property(x => x.REPLY).HasColumnName("REPLY").HasMaxLength(255);
                e.Property(x => x.STATE).HasColumnName("STATE").HasMaxLength(20);
                e.Property(x => x.EVENTTIME).HasColumnName("EVENTTIME");
                e.Property(x => x.USERID).HasColumnName("USERID").HasMaxLength(64);
                e.Property(x => x.IPADDRESS).HasColumnName("IPADDRESS").HasMaxLength(64);
                e.Property(x => x.REQUESTTIME).HasColumnName("REQUESTTIME");
            });

            modelBuilder.Entity<AppModel.Option>(e =>
            {
                e.ToTable("NA_X_OPTION");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.Name).HasColumnName("name").HasMaxLength(64);
                e.Property(x => x.NameDescription).HasColumnName("nameDescription").HasMaxLength(255);
                e.Property(x => x.Value).HasColumnName("value").HasMaxLength(64);
                e.Property(x => x.ValueDescription).HasColumnName("valueDescription").HasMaxLength(255);
                e.Property(x => x.SubValue).HasColumnName("subValue").HasMaxLength(64);
                e.Property(x => x.SubValueDescription).HasColumnName("subValueDescription").HasMaxLength(255);
                e.Property(x => x.Used).HasColumnName("used").HasMaxLength(10);
            });

            // ===== Alarm Domain =====
            modelBuilder.Entity<AlarmEx>(e =>
            {
                e.ToTable("NA_A_ALARM");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.AlarmCode).HasColumnName("alarmCode").HasMaxLength(64);
                e.Property(x => x.AlarmText).HasColumnName("alarmText").HasMaxLength(255);
                e.Property(x => x.VehicleId).HasColumnName("vehicleId").HasMaxLength(64);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.AlarmId).HasColumnName("alarmId").HasMaxLength(64);
                e.Property(x => x.TransportCommandId).HasColumnName("transportCommandId").HasMaxLength(64);
                e.Property(x => x.NearAgv).HasColumnName("nearAgv").HasMaxLength(64);
                e.Property(x => x.IsCross).HasColumnName("isCross").HasMaxLength(20);
            });

            modelBuilder.Entity<AlarmSpecEx>(e =>
            {
                e.ToTable("NA_A_ALARMSPEC");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.AlarmId).HasColumnName("alarmId").HasMaxLength(64);
                e.Property(x => x.AlarmText).HasColumnName("alarmText").HasMaxLength(255);
                e.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(20);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            });

            // ===== Logging Domain =====
            modelBuilder.Entity<LogMessage>(e =>
            {
                e.ToTable("NA_L_LOGMESSAGE");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.TransactionId).HasColumnName("transactionId").HasMaxLength(64);
                e.Property(x => x.ThreadName).HasColumnName("threadName").HasMaxLength(64);
                e.Property(x => x.OperationName).HasColumnName("operationName").HasMaxLength(128);
                e.Property(x => x.CommunicationMessageName).HasColumnName("communicationMessageName").HasMaxLength(64);
                e.Property(x => x.TransportCommandId).HasColumnName("transportCommandId").HasMaxLength(64);
                e.Property(x => x.CarrierName).HasColumnName("carrierName").HasMaxLength(64);
                e.Property(x => x.ProcessName).HasColumnName("processName").HasMaxLength(64);
                e.Property(x => x.MessageName).HasColumnName("messageName").HasMaxLength(64);
                e.Property(x => x.MachineName).HasColumnName("machineName").HasMaxLength(64);
                e.Property(x => x.UnitName).HasColumnName("unitName").HasMaxLength(64);
                e.Property(x => x.Text).HasColumnName("text").HasMaxLength(4000);
                e.Property(x => x.LogLevel).HasColumnName("logLevel").HasMaxLength(20);
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
            });

            modelBuilder.Entity<LargeLogMessage>(e =>
            {
                e.ToTable("NA_L_LARGELOGMESSAGE");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.LogMessageId).HasColumnName("logMessageId").HasMaxLength(64);
                e.Property(x => x.LargeText).HasColumnName("largeText");
                e.Property(x => x.PartitionId).HasColumnName("partitionId");
                e.Property(x => x.Time).HasColumnName("time");
                e.Property(x => x.Sequence).HasColumnName("sequence");
            });

            // ===== Material Domain =====
            modelBuilder.Entity<CarrierEx>(e =>
            {
                e.ToTable("NA_M_CARRIER");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                e.Property(x => x.CarrierLoc).HasColumnName("carrierLoc").HasMaxLength(64);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
            });

            // ===== Communication Domain =====
            modelBuilder.Entity<Nio>(e =>
            {
                e.ToTable("NA_C_NIO");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").HasMaxLength(64);
                e.Property(x => x.InterfaceClassName).HasColumnName("interfaceClassName").HasMaxLength(255);
                e.Property(x => x.WorkflowManagerName).HasColumnName("workflowManagerName").HasMaxLength(255);
                e.Property(x => x.ApplicationName).HasColumnName("applicationName").HasMaxLength(64);
                e.Property(x => x.Port).HasColumnName("port");
                e.Property(x => x.RemoteIp).HasColumnName("remoteIp").HasMaxLength(64);
                e.Property(x => x.MachineName).HasColumnName("machineName").HasMaxLength(64);
                e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(64);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.Creator).HasColumnName("creator").HasMaxLength(45);
                e.Property(x => x.Editor).HasColumnName("editor").HasMaxLength(45);
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.EditTime).HasColumnName("editTime");
            });

            modelBuilder.Entity<MqttConfig>(e =>
            {
                e.ToTable("NA_C_MQTT");
                e.HasKey(x => x.Seq);
                e.Property(x => x.Seq).HasColumnName("id").ValueGeneratedOnAdd();
                e.Ignore(x => x.Id);
                e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(64);
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.ApplicationName).HasColumnName("applicationName").HasMaxLength(64);
                e.Property(x => x.WorkflowManagerName).HasColumnName("workflowManagerName").HasMaxLength(255);
                e.Property(x => x.BrokerIp).HasColumnName("brokerIp").HasMaxLength(64);
                e.Property(x => x.BrokerPort).HasColumnName("brokerPort");
                e.Property(x => x.TopicPrefix).HasColumnName("topicPrefix").HasMaxLength(128);
                e.Property(x => x.ClientId).HasColumnName("clientId").HasMaxLength(128);
                e.Property(x => x.UserName).HasColumnName("userName").HasMaxLength(64);
                e.Property(x => x.Password).HasColumnName("password").HasMaxLength(128);
                e.Property(x => x.KeepAliveSeconds).HasColumnName("keepAliveSeconds");
                e.Property(x => x.ReconnectDelayMs).HasColumnName("reconnectDelayMs");
                e.Property(x => x.State).HasColumnName("state").HasMaxLength(20);
                e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
                e.Property(x => x.CreateTime).HasColumnName("createTime");
                e.Property(x => x.Creator).HasColumnName("creator").HasMaxLength(45);
                e.Property(x => x.Editor).HasColumnName("editor").HasMaxLength(45);
                e.Property(x => x.EditTime).HasColumnName("editTime");
            });

            // NOTE: Secs 엔티티는 전용 모델 클래스가 없으므로 제외.
            // 필요 시 ACS.Core에 Secs 모델 클래스를 생성하여 추가.
        }
    }
}
