using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model
{
    public class LinkedZoneNode : Entity
    {
        private string machineName = "";
        private string zoneName = "";
        private string linkedMachineName = "";
        private string linkedZoneName = "";
        private string availability = "";
        public static string AVAILABILITY_AVAILABLE = "0";
        public static string AVAILABILITY_UNAVAILABLE = "1";

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string ZoneName { get { return zoneName; } set { zoneName = value; } }
        public string LinkedMachineName { get { return linkedMachineName; } set { linkedMachineName = value; } }
        public string LinkedZoneName { get { return linkedZoneName; } set { linkedZoneName = value; } }
        public string Availability { get { return availability; } set { availability = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("interZoneNode{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", linkedMachineName=").Append(this.linkedMachineName);
            sb.Append(", linkedZoneName=").Append(this.linkedZoneName);
            sb.Append(", availability=").Append(this.availability);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
