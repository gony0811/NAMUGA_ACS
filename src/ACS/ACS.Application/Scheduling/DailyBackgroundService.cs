using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ACS.Framework.Logging;

namespace ACS.Scheduling
{
    /// <summary>
    /// 매일 지정 시각에 실행되는 BackgroundService 베이스 클래스.
    /// Quartz CronTrigger의 "0 0 0 * * ?" (매일 자정) 패턴을 대체.
    /// </summary>
    public abstract class DailyBackgroundService : BackgroundService
    {
        protected readonly Logger logger;

        protected DailyBackgroundService()
        {
            logger = Logger.GetLogger(GetType());
        }

        /// <summary>매일 실행할 작업.</summary>
        protected abstract void ExecuteOnce();

        /// <summary>실행 시각 (자정 = TimeSpan.Zero).</summary>
        protected virtual TimeSpan TimeOfDay => TimeSpan.Zero;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.Add(TimeOfDay);
                if (nextRun <= now)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    ExecuteOnce();
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
            }
        }
    }
}
