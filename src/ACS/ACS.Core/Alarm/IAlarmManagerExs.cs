using ACS.Core.Alarm.Model;
using ACS.Core.Alarm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


namespace ACS.Core.Alarm
{
    public interface IAlarmManagerExs : IAlarmManagerEx
    {

        AlarmExs GetAlarmByVehicleId(String VehicleId);
    }
}
