using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource;
using System.Globalization;

namespace ACS.Scheduling
{
    public class AwakeDeleteVehicleCrossWaitJob : DailyBackgroundService
    {
        private readonly IResourceManagerEx _resourceManager;
        private const int SavePeriodSeconds = 60;

        public AwakeDeleteVehicleCrossWaitJob(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        protected override void ExecuteOnce()
        {
            //logger.info("AwakeDeleteVehicleCrossWaitJob will be invoked");

            DateTime toCalendar = DateTime.Now;
            DateTime toDelete = toCalendar.AddSeconds(-SavePeriodSeconds);

            //logger.info("VehicleCrossWait will be deleted until {" + TimeUtils.getTimeToMilliPrettyFormat(toCalendar.getTime()) + "}");

            int count = _resourceManager.DeleteVehicleCrossWait(toDelete);

            //logger.info("VehicleCrossWait(" + count + ") was deleted, DeleteVehicleCrossWaitJob was end");
        }
    }
}
