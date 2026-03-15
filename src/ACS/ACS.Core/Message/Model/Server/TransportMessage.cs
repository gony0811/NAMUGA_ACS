using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class TransportMessage : TransferMessage
    {
        private string transportJobId;
        private string lotId;

        protected internal string TransportJobId { get { return transportJobId; } set {transportJobId = value; } }
        protected internal string LotId { get { return lotId; } set { lotId = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", transportJobId=").Append(this.transportJobId);
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
            sb.Append("}");

            return sb.ToString();
        }

    }
}
