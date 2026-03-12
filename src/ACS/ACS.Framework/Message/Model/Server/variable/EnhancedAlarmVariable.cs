using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server.variable
{
    public class EnhancedAlarmVariable
    {
        private string alarmCode;
        private string alarmId;
        private string alarmText;

        public string AlarmCode { get { return alarmCode; } set { alarmCode = value; } }
        public string AlarmId { get { return alarmId; } set { alarmId = value; } }
        public string AlarmText { get { return alarmText; } set { alarmText = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedUnitAlarmVariable{");
            sb.Append("alarmCode=").Append(this.alarmCode);
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
