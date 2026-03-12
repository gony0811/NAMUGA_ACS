using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model.Dynamic
{
    public class DynamicLoad : Entity
    {
        private string transportMachineName = "";
        private string used = "T";
        private int minimum;
        private int weight;
        private int load;
        private int threshold;

        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public string Used { get { return used; } set { used = value; } }
        public int Minimum { get { return minimum; } set { minimum = value; } }
        public int Weight { get { return weight; } set { weight = value; } }
        public int Load { get { return load; } set { load = value; } }
        public int Threshold { get { return threshold; } set { threshold = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("dynamicLoad{");
            sb.Append("transportMachineName=").Append(this.transportMachineName);
            sb.Append(", used=").Append(this.used);
            sb.Append(", weight=").Append(this.weight);
            sb.Append(", minimum=").Append(this.minimum);
            sb.Append(", threshold=").Append(this.threshold);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
