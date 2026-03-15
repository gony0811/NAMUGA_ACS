using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class EnhancedActiveZoneVariable
    {
        private string zoneName;
        private string zoneCapacity;
        private string zoneSize;
        private string zoneType;

        public string ZoneName { get { return zoneName; } set { zoneName = value; } }
        public string ZoneCapacity { get { return zoneCapacity; } set { zoneCapacity = value; } }
        public string ZoneSize { get { return zoneSize; } set { zoneSize = value; } }
        public string ZoneType { get { return zoneType; } set { zoneType = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedActiveZoneVariable{");
            sb.Append("zoneName=").Append(this.zoneName);
            sb.Append(", zoneCapacity=").Append(this.zoneCapacity);
            sb.Append(", zoneSize=").Append(this.zoneSize);
            sb.Append(", zoneType=").Append(this.zoneType);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
