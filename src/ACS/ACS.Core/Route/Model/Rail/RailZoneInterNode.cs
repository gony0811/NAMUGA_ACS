using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model.Rail
{
    public class RailZoneInterNode : Entity
    {
        private string fromRailZoneName = "";
        private string toRailZoneName = "";
        private int weight;

        public string FromRailZoneName { get { return fromRailZoneName; } set { fromRailZoneName = value; } }
        public string ToRailZoneName { get { return toRailZoneName; } set { toRailZoneName = value; } }
        public int Weight { get { return weight; } set { weight = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RailZoneInterNode{");
            sb.Append("fromRailZoneName=").Append(this.fromRailZoneName);
            sb.Append(", toRailZoneName=").Append(this.toRailZoneName);
            sb.Append(", weight=").Append(this.weight);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
