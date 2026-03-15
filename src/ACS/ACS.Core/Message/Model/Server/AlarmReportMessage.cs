using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class AlarmReportMessage : BaseMessage
    {
        public const string STATE_SET = "SET";
        public const string STATE_CLEARED = "CLEARED";

        private string alarmId;
        private string alarmCode;
        private string alarmText;
        private string severity = "LIGHT";
        private string alarmState = "SET";

        public string AlarmId {get {return alarmId;} set {alarmId = value;}}
        public string AlarmCode {get {return alarmCode;} set {alarmCode = value;}}
        public string AlarmText {get {return alarmText;} set {alarmText = value;}}
        public string Severity {get {return severity;} set {severity = value;}}
        public string AlarmState { get { return alarmState; } set { alarmState = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarmReportMessage{");
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
            sb.Append(", severity=").Append(this.severity);
            sb.Append(", alarmState=").Append(this.alarmState);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
