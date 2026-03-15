using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Alarm.Model
{
    public class CurrentUnitAlarm : Entity
    {
        private String machineName;
        private String unitName;
        private String alarmId;
        private String alarmCode;
        private String alarmText;
        private String currentUnitPosition;
        private String unitStateClearable = "T";
        private DateTime? createTime = new DateTime();

        public CurrentUnitAlarm()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string AlarmId { get { return alarmId; } set { alarmId = value; } }
        public string AlarmCode { get { return alarmCode; } set { alarmCode = value; } }
        public string AlarmText { get { return alarmText; } set { alarmText = value; } }
        public string CurrentUnitPosition { get { return currentUnitPosition; } set { currentUnitPosition = value; } }
        public string UnitStateClearable { get { return unitStateClearable; } set { unitStateClearable = value; } }
        public DateTime? CreateTime { get { return createTime; } set { createTime = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("currentUnitAlarm{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmCode=").Append(this.alarmCode);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append(", currentUnitPosition=").Append(this.currentUnitPosition);
            sb.Append(", unitStateClearable=").Append(this.unitStateClearable);
            sb.Append(", createTime=").Append(this.createTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
