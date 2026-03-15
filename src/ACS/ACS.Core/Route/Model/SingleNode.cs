using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model
{
    public class SingleNode : Entity
    {
        private string transportMachineName = "";
        private int load;

        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public int Load { get { return load; } set { load = value; } }

        public int CalculateTotalWeight()
        {
            return this.load;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("singleNode{");

            sb.Append("transportMachineName=").Append(this.transportMachineName);
            sb.Append(", load=").Append(this.load);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
