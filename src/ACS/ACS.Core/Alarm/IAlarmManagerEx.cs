using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Alarm.Model;

namespace ACS.Core.Alarm
{
    public interface IAlarmManagerEx
    {
        void CreateAlarm(Model.AlarmEx paramAlarm);

        void CreateAlarmSpec(Model.AlarmSpecEx paramAlarmSpec);

        int DeleteAlarm(Model.AlarmEx paramAlarm);

        int DeleteAlarmByVehicleId(String paramString);

        Model.AlarmEx GetAlarm(String paramString);

        Model.AlarmEx GetAlarmByVehicleId(String paramString);

        Model.AlarmEx GetAlarmByVehicleIdAndAlarmId(String paramString1, String paramString2);

        IList GetAlarmsByVehicleId(String paramString);

        AlarmSpecEx GetAlarmSpecByAlarmId(String paramString);
    }
}
