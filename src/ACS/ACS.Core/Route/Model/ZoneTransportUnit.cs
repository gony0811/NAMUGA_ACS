using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model
{
    public class ZoneTransportUnit : Entity
    {
        private string zoneName;
        private string transportUnitName;

        public string ZoneName { get { return zoneName; } set { zoneName = value; } }
        public string TransportUnitName { get { return transportUnitName; } set { transportUnitName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneTransportUnit{");
            sb.Append("zoneName=").Append(this.zoneName);
            sb.Append(", transportUnitName=").Append(this.transportUnitName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
