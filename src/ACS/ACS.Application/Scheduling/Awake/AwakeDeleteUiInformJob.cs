using System;
using ACS.Framework.Resource;

namespace ACS.Scheduling
{
    public class AwakeDeleteUiInformJob : PeriodicBackgroundService
    {
        private readonly IResourceManagerEx _resourceManager;
        private const int SavePeriodHours = 6;

        protected override TimeSpan Interval => TimeSpan.FromMinutes(5);

        public AwakeDeleteUiInformJob(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        protected override void ExecuteOnce()
        {
            try
            {
                DeleteUiInformJob(SavePeriodHours);
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }
        }

        private void DeleteUiInformJob(int savePeriod)
        {
            DateTime toCalendar = DateTime.Now;
            DateTime deleteCalendar = toCalendar.AddHours(-savePeriod);

            int count = _resourceManager.DeleteUIInform(deleteCalendar);
        }
    }
}
