using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model.ProcessType
{
    public class MachineProcessType : Entity
    {
        private string machineName = "";
        private string processTypeName = "";

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string ProcessTypeName { get { return processTypeName; } set { processTypeName = value; } }

        public MachineProcessType()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machineProcessType{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", processTypeName=").Append(this.processTypeName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
