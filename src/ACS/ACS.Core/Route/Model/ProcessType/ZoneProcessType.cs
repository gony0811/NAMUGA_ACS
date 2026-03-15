using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model.ProcessType
{
    public class ZoneProcessType : Entity
    {
        private string zoneName = "";
        private string machineName = "";
        private string processTypeName = "";

        public ZoneProcessType()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string ZoneName { get { return zoneName; } set { zoneName = value; } }
        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string ProcessTypeName { get { return processTypeName; } set { processTypeName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneProcessType{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", processTypeName=").Append(this.processTypeName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
