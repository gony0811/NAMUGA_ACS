using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Alarm.Model
{
    public class AlarmSpecEx : TimedEntity
    {
        public static String STATE_ALARM_SET = "SET";
        public static String STATE_ALARM_CLEARED = "CLEARED";
        public static String CODE_ALARMSET = "128";
        public static String SEVERITY_LIGHT = "LIGHT";
        public static String SEVERITY_HEAVY = "HEAVY";
        public static String SEVERITY_FATAL = "FATAL";
     
        public virtual string AlarmId { get; set; }
        public virtual string AlarmText { get; set; }
        public virtual string Severity { get; set; }
        public virtual string Description1 { get; set; }

        public AlarmSpecEx()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarmSpec{");
            sb.Append(", alarmId=").Append(this.AlarmId);
            sb.Append(", alarmText=").Append(this.AlarmText);
            sb.Append(", severity=").Append(this.Severity);
            sb.Append(", description=").Append(this.Description1);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
