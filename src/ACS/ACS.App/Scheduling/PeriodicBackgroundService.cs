using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ACS.Core.Logging;

namespace ACS.Scheduling
{
    /// <summary>
    /// 주기적으로 실행되는 BackgroundService 베이스 클래스.
    /// Quartz CronTrigger의 단순 주기 실행을 대체.
    /// </summary>
    public abstract class PeriodicBackgroundService : BackgroundService
    {
        protected readonly Logger logger;

        protected PeriodicBackgroundService()
        {
            logger = Logger.GetLogger(GetType());
        }

        /// <summary>매 주기마다 실행할 작업.</summary>
        protected abstract void ExecuteOnce();

        /// <summary>실행 간격.</summary>
        protected abstract TimeSpan Interval { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 첫 실행 전 한 주기 대기
            await Task.Delay(Interval, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ExecuteOnce();
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
    }
}
