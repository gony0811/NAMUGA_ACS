using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model.Factory.Zone;

namespace ACS.Framework.Message.Model.Server
{
    public class CarrierMessage : TransferMessage
    {
        private string carrierZoneName;
        private string portType;
        private string handOffType;
        private string holded;
        private string lotId;
        private string carrierType;
        private Zone carrierZone;
        private string carrierState;
        private string transportUnitName;

        public string CarrierZoneName { get { return carrierZoneName; } set { carrierZoneName = value; } }
        public string PortType { get { return portType; } set { portType = value; } }
        public string HandOffType { get { return handOffType; } set { handOffType = value; } }
        public string Holded { get { return holded; } set { holded = value; } }
        public string LotId { get { return lotId; } set { lotId = value; } }
        public string CarrierType { get { return carrierType; } set { carrierType = value; } }
        public Zone CarrierZone { get { return carrierZone; } set { carrierZone = value; } }
        public string CarrierState { get { return carrierState; } set { carrierState = value; } }
        public string TransportUnitName { get { return transportUnitName; } set { transportUnitName = value; } }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carrierMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
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
            sb.Append(", carrierZoneName=").Append(this.carrierZoneName);
            sb.Append(", portType=").Append(this.portType);
            sb.Append(", handOffType=").Append(this.handOffType);
            sb.Append(", holded=").Append(this.holded);
            sb.Append(", lotId=").Append(this.lotId);
            sb.Append(", carrierType=").Append(this.carrierType);
            sb.Append(", carrierState=").Append(this.carrierState);
            sb.Append(", transportUnitName=").Append(this.transportUnitName);
            sb.Append(", carrierZone=").Append(this.carrierZone);
            sb.Append("}");

            return sb.ToString();
        }

    }
}
