using ACS.Framework.Alarm.Model;
using ACS.Framework.Alarm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


namespace ACS.Framework.Alarm
{
    public interface IAlarmManagerExs : IAlarmManagerEx
    {

        AlarmExs GetAlarmByVehicleId(String VehicleId);
    }
}
