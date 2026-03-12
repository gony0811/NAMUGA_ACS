using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server
{
    public class AlarmMessage: BaseMessage
    {
        private string unitState;
        private string errorId;
        private string errorNumber;
        private string recoveryOptions;

        public string UnitState { get { return unitState; } set { unitState = value; } }
        public string ErrorId { get { return errorId; } set { errorId = value; } }
        public string ErrorNumber { get { return errorNumber; } set { errorNumber = value; } }
        public string RecoveryOptions { get { return recoveryOptions; } set { recoveryOptions = value; } }
        public object ReceivedMessage { get { return base.ReceivedMessage; } set { base.ReceivedMessage = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarmMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", unitState=").Append(this.unitState);
            sb.Append(", errorId=").Append(this.errorId);
            sb.Append(", errorNumber=").Append(this.errorNumber);
            sb.Append(", recoveryOptions=").Append(this.recoveryOptions);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
