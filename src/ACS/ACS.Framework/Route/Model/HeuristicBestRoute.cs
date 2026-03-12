using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model
{
    public class HeuristicBestRoute : Entity
    {
        private string sourceTransportMachineName;
        private string destTransportMachineName;
        private string route;
        private int cost;

        public string SourceTransportMachineName { get { return sourceTransportMachineName; } set { sourceTransportMachineName = value; } }
        public string DestTransportMachineName { get { return destTransportMachineName; } set { destTransportMachineName = value; } }
        public string Route { get { return route; } set { route = value; } }
        public int Cost { get { return cost; } set { cost = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("heuristicBestRoute{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", sourceTransportMachineName=").Append(this.sourceTransportMachineName);
            sb.Append(", destTransportMachineName=").Append(this.destTransportMachineName);
            sb.Append(", route=").Append(this.route);
            sb.Append(", cost=").Append(this.cost);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
