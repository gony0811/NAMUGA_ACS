using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model.Dynamic
{
    public class BalanceLoad : Entity
    {
        private string transportMachineName = "";
        private string nextTransportMachineName = "";
        private string linkedUnitName = "";
        private string used = "T";
        private int minimum;
        private int weight;
        private int load;
        private int threshold;

        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public string NextTransportMachineName { get { return nextTransportMachineName; } set { nextTransportMachineName = value; } }
        public string LinkedUnitName { get { return linkedUnitName; } set { linkedUnitName = value; } }
        public string Used { get { return used; } set { used = value; } }
        public int Minimum { get { return minimum; } set { minimum = value; } }
        public int Weight { get { return weight; } set { weight = value; } }
        public int Load { get { return load; } set { load = value; } }
        public int Threshold { get { return threshold; } set { threshold = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("balanceLoad{");
            sb.Append("transportMachineName=").Append(this.transportMachineName);
            sb.Append(", nextTransportMachineName=").Append(this.nextTransportMachineName);
            sb.Append(", linkedUnitName=").Append(this.linkedUnitName);
            sb.Append(", used=").Append(this.used);
            sb.Append(", weight=").Append(this.weight);
            sb.Append(", minimum=").Append(this.minimum);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
