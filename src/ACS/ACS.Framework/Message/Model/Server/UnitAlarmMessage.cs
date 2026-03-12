using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server
{
    public class UnitAlarmMessage: BaseMessage
    {
        private string alarmId;
        private string alarmCode;
        private string alarmText;
        private string currentUnitPosition;
        private string unitStateClearable;

        public string AlarmId { get { return alarmId; } set { alarmId = value; } }
        public string AlarmCode { get { return alarmCode; } set { alarmCode = value; } }
        public string AlarmText { get { return alarmText; } set { alarmText = value; } }
        public string CurrentUnitPosition { get { return currentUnitPosition; } set { currentUnitPosition = value; } }
        public string UnitStateClearable { get { return unitStateClearable; } set { unitStateClearable = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitAlarmMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmCode=").Append(this.alarmCode);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append(", currentUnitPosition=").Append(this.currentUnitPosition);
            sb.Append(", unitStateClearable=").Append(this.unitStateClearable);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
