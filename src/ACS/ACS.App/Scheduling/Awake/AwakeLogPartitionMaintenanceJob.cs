using System;
using System.Globalization;
using ACS.Core.Base.Interface;
using Microsoft.Extensions.Configuration;

namespace ACS.Scheduling.Awake
{
    /// <summary>
    /// 로그 테이블(NA_L_LOGMESSAGE / NA_L_LARGELOGMESSAGE)의 일별 RANGE 파티션 유지보수.
    /// - 사전 생성: 오늘 ~ 오늘+CreateAheadDays 파티션을 CREATE TABLE IF NOT EXISTS (오늘 파티션은 전날 미리 생성되어, 자정 직후 로그가 DEFAULT로 새지 않게 한다).
    /// - 만료 제거: (보존일+1) ~ (보존일+DropLookbackDays) 일 전 파티션을 DROP TABLE IF EXISTS — 스캔/bloat 없이 즉시 삭제.
    /// 공유 DB이므로 control 프로세스에서만 등록(SchedulingModule). 시각·파티션 경계는 모두 UTC. DEFAULT 파티션은 절대 DROP하지 않는다.
    /// 보존 일수 = Acs:LogDeleteDays(기본 7). 파티션 생성/변환의 1차 보장은 Executor.ConvertLogTableToPartitioned가 담당.
    /// </summary>
    public class AwakeLogPartitionMaintenanceJob : DailyBackgroundService
    {
        private static readonly string[] Tables = { "NA_L_LOGMESSAGE", "NA_L_LARGELOGMESSAGE" };

        private const int CreateAheadDays = 3;    // 오늘 + N일까지 사전 생성
        private const int DropLookbackDays = 40;  // 만료 후 최대 N일 전까지 거슬러 DROP 시도(잡 장기 미실행 복구)

        private readonly IPersistentDao _persistentDao;
        private readonly IConfiguration _configuration;

        public AwakeLogPartitionMaintenanceJob(IPersistentDao persistentDao, IConfiguration configuration)
        {
            _persistentDao = persistentDao ?? throw new ArgumentNullException(nameof(persistentDao));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>실행 시각: 매일 03:00.</summary>
        protected override TimeSpan TimeOfDay => new TimeSpan(3, 0, 0);

        protected override void ExecuteOnce()
        {
            try
            {
                string delday = _configuration["Acs:LogDeleteDays"];
                if (!int.TryParse(delday, out var days) || days <= 0)
                {
                    days = 7;
                }

                DateTime today = DateTime.UtcNow.Date;

                foreach (var table in Tables)
                {
                    // 사전 생성: 오늘 ~ 오늘+CreateAheadDays
                    for (int i = 0; i <= CreateAheadDays; i++)
                    {
                        CreatePartition(table, today.AddDays(i));
                    }

                    // 만료 제거: (days+1) ~ (days+DropLookbackDays) 일 전
                    for (int i = days + 1; i <= days + DropLookbackDays; i++)
                    {
                        DropPartition(table, today.AddDays(-i));
                    }
                }

                logger.Info($"LogPartitionMaintenanceJob completed — retention {days}d, ensured {today:yyyy-MM-dd}..+{CreateAheadDays}d, dropped older than {today.AddDays(-days):yyyy-MM-dd} (lookback {DropLookbackDays}d).");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        private void CreatePartition(string table, DateTime utcDay)
        {
            string name = PartName(table, utcDay);
            string from = utcDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 00:00:00+00";
            string to = utcDay.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 00:00:00+00";
            string sql = $"CREATE TABLE IF NOT EXISTS public.\"{name}\" PARTITION OF public.\"{table}\" FOR VALUES FROM ('{from}') TO ('{to}');";
            try
            {
                _persistentDao.ExecuteUpdate(sql);
            }
            catch (Exception e)
            {
                // DEFAULT 파티션에 해당 범위 행이 있으면(잡 장기 미실행 등) 생성이 실패할 수 있다. 로깅 후 계속.
                logger.Error("Create partition failed: " + sql, e);
            }
        }

        private void DropPartition(string table, DateTime utcDay)
        {
            string name = PartName(table, utcDay);
            string sql = $"DROP TABLE IF EXISTS public.\"{name}\";";
            try
            {
                _persistentDao.ExecuteUpdate(sql);
            }
            catch (Exception e)
            {
                logger.Error("Drop partition failed: " + sql, e);
            }
        }

        private static string PartName(string table, DateTime utcDay)
        {
            return table + "_p" + utcDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }
    }
}
