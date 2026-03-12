using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server
{
    public class ProcessPortMessage : TransportMessage
    {
        private string portId;
        private string lotId;
        private string lotInfo;
        private string AccessMode = "AUTO";
        private string subState = "TRANSFERBLOCKED";

        public string PortId { get {return portId;} set {portId = value;}}
        public string LotInfo { get {return lotInfo;} set {lotInfo = value;}}
        public string AccessMode1 { get {return AccessMode;} set {AccessMode = value;}}
        public string SubState { get { return subState; } set { subState = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", transportJobId=").Append(this.TransportJobId);
            sb.Append(", lotId=").Append(this.lotId);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentType=").Append(this.CurrentType);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", destMachineName=").Append(this.DestMachineName);
            sb.Append(", destType=").Append(this.DestType);
            sb.Append(", destUnitName=").Append(this.DestUnitName);
            sb.Append(", priority=").Append(this.Priority);
            sb.Append(", fromUnitName=").Append(this.FromUnitName);
            sb.Append(", toUnitName=").Append(this.ToUnitName);
            sb.Append(", toType=").Append(this.ToType);
            sb.Append(", portId=").Append(this.portId);
            sb.Append(", lotInfo=").Append(this.lotInfo);
            sb.Append(", AccessMode=").Append(this.AccessMode);
            sb.Append(", subState=").Append(this.subState);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
