using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class EnhancedUnitAlarmVariable
    {
        private string unitId;
        private string alarmCode;
        private string alarmId;
        private string alarmText;
        private string currentUnitPosition;
        private string unitStateClearable;

        public string UnitId { get { return unitId; } set { unitId = value; } }
        public string AlarmCode { get { return alarmCode; } set { alarmCode = value; } }
        public string AlarmId { get { return alarmId; } set { alarmId = value; } }
        public string AlarmText { get { return alarmText; } set { alarmText = value; } }
        public string CurrentUnitPosition { get { return currentUnitPosition; } set { currentUnitPosition = value; } }
        public string UnitStateClearable { get { return unitStateClearable; } set { unitStateClearable = value; } }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedUnitAlarmVariable{");
            sb.Append("unitId=").Append(this.unitId);
            sb.Append(", alarmCode=").Append(this.alarmCode);
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append(", currentUnitPosition=").Append(this.currentUnitPosition);
            sb.Append(", unitStateClearable=").Append(this.unitStateClearable);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
