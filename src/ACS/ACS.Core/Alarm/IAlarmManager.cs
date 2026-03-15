using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Alarm.Model;

namespace ACS.Core.Alarm
{
    public interface IAlarm
    {
        void createAlarm(ACS.Core.Alarm.Model.Alarm paramAlarm);
        void createAlarmSpec(ACS.Core.Alarm.Model.AlarmSpec paramAlarmSpec);
        int deleteAlarm(ACS.Core.Alarm.Model.Alarm paramAlarm);
        int deleteAlarmByVehicleId(string paramString);

        ACS.Core.Alarm.Model.Alarm GetAlarm(string paramString);

        ACS.Core.Alarm.Model.Alarm GetAlarmByVehicleId(string paramString);

        ACS.Core.Alarm.Model.Alarm GetAlarmByVehicleIdAndAlarmId(string paramString1, string paramString2);

        List<ACS.Core.Alarm.Model.Alarm> GetAlarmsByVehicleId(string paramString);

        ACS.Core.Alarm.Model.AlarmSpec GetAlarmSpecByAlarmId(string paramString);
    }
}
