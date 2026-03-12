using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Alarm.Model;

namespace ACS.Framework.Message.Model.Server
{
    public class AlarmsMessage : StatusVariableMessage
    {

        private IList alarms = new ArrayList();

        public IList Alarms { get { return alarms; } set { alarms = value; } }

        public  int Add(AlarmReport alarmReport)
        {
            return this.Alarms.Add(alarmReport);
        }

    }
}
