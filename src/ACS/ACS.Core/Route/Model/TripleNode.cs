using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model
{
    public class TripleNode : Entity
    {
        private string transportMachineName = "";
        private string fromMachineName = "";
        private string toMachineName = "";
        private int delay;

        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public string FromMachineName { get { return fromMachineName; } set { fromMachineName = value; } }
        public string ToMachineName { get { return toMachineName; } set { toMachineName = value; } }
        public int Delay { get { return delay; } set { delay = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("tripleNode{");

            sb.Append("fromMachineName=").Append(this.fromMachineName);
            sb.Append(", transportMachineName=").Append(this.transportMachineName);
            sb.Append(", toMachineName=").Append(this.toMachineName);
            sb.Append(", delay=").Append(this.delay);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
