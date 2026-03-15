using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class PortMessage: BaseMessage
    {
        private string inOutType;
        private string accessMode;
        private string portState;

        public string InOutType { get {return inOutType;} set {inOutType = value;}}
        public string AccessMode { get {return accessMode;} set {accessMode = value;}}
        public string PortState { get { return portState; } set { portState = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("portMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", inOutType=").Append(this.inOutType);
            sb.Append(", portState=").Append(this.portState);
            sb.Append(", accessMode=").Append(this.accessMode);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
