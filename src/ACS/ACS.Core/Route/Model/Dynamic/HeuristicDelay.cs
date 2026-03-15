using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model.Dynamic
{
    public class HeuristicDelay : Entity
    {
        private string fromMachineName = "";
        private string transportMachineName = "";
        private string toMachineName = "";
        private string used = "T";
        private int minimum;
        private int weight;
        private int delay;
        private int threshold;
        private int samplingCount;

        public string FromMachineName { get { return fromMachineName; } set { fromMachineName = value; } }
        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public string ToMachineName { get { return toMachineName; } set { toMachineName = value; } }
        public string Used { get { return used; } set { used = value; } }
        public int Minimum { get { return minimum; } set { minimum = value; } }
        public int Weight { get { return weight; } set { weight = value; } }
        public int Delay { get { return delay; } set { delay = value; } }
        public int Threshold { get { return threshold; } set { threshold = value; } }
        public int SamplingCount { get { return samplingCount; } set { samplingCount = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("heuristicDelay{");
            sb.Append("transportMachineName=").Append(this.transportMachineName);
            sb.Append(", fromMachineName=").Append(this.fromMachineName);
            sb.Append(", toMachineName=").Append(this.toMachineName);
            sb.Append(", used=").Append(this.used);
            sb.Append(", weight=").Append(this.weight);
            sb.Append(", minimum=").Append(this.minimum);
            sb.Append(", samplingCount=").Append(this.samplingCount);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
