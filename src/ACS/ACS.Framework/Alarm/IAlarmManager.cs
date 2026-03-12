using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Alarm.Model;

namespace ACS.Framework.Alarm
{
    public interface IAlarm
    {
        void createAlarm(ACS.Framework.Alarm.Model.Alarm paramAlarm);
        void createAlarmSpec(ACS.Framework.Alarm.Model.AlarmSpec paramAlarmSpec);
        int deleteAlarm(ACS.Framework.Alarm.Model.Alarm paramAlarm);
        int deleteAlarmByVehicleId(string paramString);

        ACS.Framework.Alarm.Model.Alarm GetAlarm(string paramString);

        ACS.Framework.Alarm.Model.Alarm GetAlarmByVehicleId(string paramString);

        ACS.Framework.Alarm.Model.Alarm GetAlarmByVehicleIdAndAlarmId(string paramString1, string paramString2);

        List<ACS.Framework.Alarm.Model.Alarm> GetAlarmsByVehicleId(string paramString);

        ACS.Framework.Alarm.Model.AlarmSpec GetAlarmSpecByAlarmId(string paramString);
    }
}
