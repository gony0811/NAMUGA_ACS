using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model.Factory.Zone;

namespace ACS.Framework.Message.Model.Server
{
    public class ZoneMessage: BaseMessage
    {
        private string zoneName;
        private string zoneCapacity;
        private Zone zone;

        public Zone Zone { get { return zone; } set { zone = value; } }
        protected internal string ZoneName { get { return zoneName; } set { zoneName = value; } }
        protected internal string ZoneCapacity { get { return zoneCapacity; } set { zoneCapacity = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", zoneCapacity=").Append(this.zoneCapacity);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
