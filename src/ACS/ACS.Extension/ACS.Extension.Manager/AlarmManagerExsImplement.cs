using System;
using ACS.Manager.Alarm;
using ACS.Extension.Framework.Alarm;
using ACS.Extension.Framework.Alarm.Model;
using ACS.Extension.Framework.Resource.Model;
using System.Collections;

namespace ACS.Extension.Manager
{
    public class AlarmManagerExsImplement : AlarmManagerExImplement, IAlarmManagerExs
    {
        public override IList GetAlarmsByVehicleId(String paramVehicleId)
        {
            return PersistentDao.FindByAttribute(typeof(AlarmExs), "VehicleId", paramVehicleId);
        }

        public AlarmExs GetAlarmByVehicleId(String paramVehicleId)
        {
            IList alarms = PersistentDao.FindByAttribute(typeof(AlarmExs), "VehicleId", paramVehicleId);
            if (alarms !=null && alarms.Count > 0)
            {
                return (AlarmExs)alarms[0];
            }
            return null;
        }

    }
}
