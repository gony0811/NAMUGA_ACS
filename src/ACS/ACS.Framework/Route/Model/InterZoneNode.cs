using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model
{
    public class InterZoneNode : Entity
    {
        private string fromZoneName = "";
        private string toZoneName = "";
        private string fromAvailability = "0";
        private string toAvailability = "0";
        private string transportMachineName = "";
        private int cost;
        public static string AVAILABILITY_AVAILABLE = "0";
        public static string AVAILABILITY_UNAVAILABLE = "1";

        public string FromZoneName { get { return fromZoneName; } set { fromZoneName = value; } }
        public string ToZoneName { get { return fromZoneName; } set { fromZoneName = value; } }
        public string FromAvailability { get { return fromAvailability; } set { fromAvailability = value; } }
        public string ToAvailability { get { return toAvailability; } set { toAvailability = value; } }
        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public int Cost { get { return cost; } set { cost = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("intraZoneNode{");
            sb.Append("fromZoneName=").Append(this.fromZoneName);
            sb.Append(", toZoneName=").Append(this.toZoneName);
            sb.Append(", fromAvailability=").Append(this.fromAvailability);
            sb.Append(", toAvailability=").Append(this.toAvailability);
            sb.Append(", transportMachineName=").Append(this.transportMachineName);
            sb.Append(", cost=").Append(this.cost);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
