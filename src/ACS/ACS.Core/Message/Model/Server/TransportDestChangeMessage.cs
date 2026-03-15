using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class TransportDestChangeMessage : TransportMessage
    {
        private string orgDestMachineName;
        private string orgDestType;
        private string orgDestUnitName;
        private string newTransportJobId;
        private string newDestGroupName;
        private string newDestMachineName;
        private string newDestType;
        private string newDestUnitName;
        private string newPriority;

        protected internal string OrgDestMachineName { get { return orgDestMachineName; } set { orgDestMachineName = value; } }
        protected internal string OrgDestType { get { return orgDestType; } set { orgDestType = value; } }
        protected internal string OrgDestUnitName { get { return orgDestUnitName; } set { orgDestUnitName = value; } }
        protected internal string NewTransportJobId { get { return newTransportJobId; } set { newTransportJobId = value; } }
        protected internal string NewDestGroupName { get { return newDestGroupName; } set { newDestGroupName = value; } }
        protected internal string NewDestMachineName { get { return newDestMachineName; } set { newDestMachineName = value; } }
        protected internal string NewDestType { get { return newDestType; } set { newDestType = value; } }
        protected internal string NewDestUnitName { get { return newDestUnitName; } set { newDestUnitName = value; } }
        protected internal string NewPriority { get { return newPriority; } set { newPriority = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportDestChangeMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", transportJobId=").Append(this.TransportJobId);
            sb.Append(", lotId=").Append(this.LotId);
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
            sb.Append(", orgDestMachineName=").Append(this.orgDestMachineName);
            sb.Append(", orgDestType=").Append(this.orgDestType);
            sb.Append(", orgDestUnitName=").Append(this.orgDestUnitName);
            sb.Append(", newTransportJobId=").Append(this.newTransportJobId);
            sb.Append(", newDestGroupName=").Append(this.newDestGroupName);
            sb.Append(", newDestMachineName=").Append(this.newDestMachineName);
            sb.Append(", newDestType=").Append(this.newDestType);
            sb.Append(", newDestUnitName=").Append(this.newDestUnitName);
            sb.Append(", newPriority=").Append(this.newPriority);
            sb.Append("}");

            return sb.ToString();
        }

    }
}
