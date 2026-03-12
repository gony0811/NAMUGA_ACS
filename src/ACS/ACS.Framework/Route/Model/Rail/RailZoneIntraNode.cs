using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model.Rail
{
    public class RailZoneIntraNode : Entity
    {
        private string unitName = "";
        private string railZoneName = "";

        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string RailZoneName { get { return railZoneName; } set { railZoneName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RailZoneIntraNode{");
            sb.Append("unitName=").Append(this.unitName);
            sb.Append(", railZoneName=").Append(this.railZoneName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
