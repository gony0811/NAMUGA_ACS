using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Alarm;
using ACS.Framework.Alarm.Model;
using ACS.Framework.History;
using System.Runtime.Serialization;

namespace ACS.Manager.Alarm
{
    public class AlarmManagerExImplement : AbstractManager, IAlarmManagerEx
    {
        public IHistoryManagerEx HistoryManager { get; set; }
        public void CreateAlarm(AlarmEx paramAlarm)
        {
            PersistentDao.Save(paramAlarm);
            //logger.fine("alarm{" + alarm.getId() + "} was created, " + paramAlarm);

            String state = "SET";
            this.HistoryManager.CreateAlarmReportHistory(paramAlarm, state);
        }

        public void CreateAlarmSpec(AlarmSpecEx paramAlarmSpec)
        {
            PersistentDao.Save(paramAlarmSpec);
            //logger.fine("alarmSpec{" + alarmSpec.getAlarmId() + "} was created, " + alarmSpec);
        }

        public int DeleteAlarm(AlarmEx paramAlarm)
        {
            StringBuilder sb = new StringBuilder(paramAlarm.Id);
            return PersistentDao.Delete(typeof(AlarmEx), (ISerializable)sb);
        }

        public int DeleteAlarmByVehicleId(String paramVehicleId)
        {
            String state = "CLEAR";
            IList alarms = PersistentDao.FindByAttribute(typeof(AlarmEx), "VehicleId", paramVehicleId);
            if (alarms.Count > 0)
            {
                foreach (var item in alarms)
                {
                    AlarmEx alarmACS = (AlarmEx)item;
                    alarmACS.AlarmCode = "0";
                    this.HistoryManager.CreateAlarmReportHistory(alarmACS, state);
                }
            }
            return PersistentDao.DeleteByAttribute(typeof(AlarmEx), "VehicleId", paramVehicleId);
        }

        public AlarmEx GetAlarm(String paramId)
        {
            StringBuilder sb = new StringBuilder(paramId);
            return (AlarmEx)PersistentDao.Find(typeof(AlarmEx), (ISerializable)sb);
        }

        public AlarmEx GetAlarmByVehicleId(String paramVehicleId)
        {
            IList alarms = PersistentDao.FindByAttribute(typeof(AlarmEx), "VehicleId", paramVehicleId);
            if (alarms.Count > 0)
            {
                return (AlarmEx)alarms[0];
            }
            return null;
        }

        public AlarmEx GetAlarmByVehicleIdAndAlarmId(String paramVehicleId, String paramAlarmId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("VehicleId", paramVehicleId);
            attributes.Add("AlarmId", paramAlarmId);

            IList alarms = PersistentDao.FindByAttributes(typeof(AlarmEx), attributes);
            if (alarms.Count > 0)
            {
                return (AlarmEx)alarms[0];
            }
            return null;
        }

        public virtual IList GetAlarmsByVehicleId(String paramVehicleId)
        {
            return PersistentDao.FindByAttribute(typeof(AlarmEx), "VehicleId", paramVehicleId);
        }

        public AlarmSpecEx GetAlarmSpecByAlarmId(String paramAlarmId)
        {
            IList alarms = PersistentDao.FindByAttribute(typeof(AlarmSpecEx), "AlarmId", paramAlarmId);
            if (alarms.Count > 0)
            {
                foreach (AlarmSpecEx alarmspec in alarms)
                {
                    if (alarmspec.Severity == null) continue;
                    else
                    {
                        return alarmspec;
                    }
                }
                //return (AlarmSpecEx)alarms[0];
            }
            return null;
        }
    }
}
