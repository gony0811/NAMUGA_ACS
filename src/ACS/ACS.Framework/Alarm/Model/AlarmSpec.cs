using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Alarm.Model
{
    public class AlarmSpec : TimedEntity
    {
        public static string STATE_ALARM_SET = "SET";
        public static string STATE_ALARM_CLEARED = "CLEARED";
        public static string CODE_ALARMSET = "128";
        public static string SERVERITY_LIGHT = "LIGHT";
        public static string SERVERITY_HEAVY = "HEAVY";
        public static string SERVERITY_FATAL = "FATAL";

        private string alarmId = "";
        private string alarmText = "";
        private string severity = "LIGHT";
        private string description = "";

        public AlarmSpec()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string AlarmId
        {
            get { return this.alarmId; }
            set { this.alarmId = value; }
        }

        public string Serverity
        {
            get { return this.severity; }
            set { this.severity = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarmSpec{");
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append(", severity=").Append(this.severity);
            sb.Append(", description=").Append(this.description);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
