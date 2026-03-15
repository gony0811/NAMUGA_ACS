using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model
{
    public class IntraNode : Entity
    {
        private string machineName = "";
        private string unitName = "";
        private string transportMachineName = "";

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("intraNode{");
            sb.Append("transportMachineName=").Append(this.transportMachineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
