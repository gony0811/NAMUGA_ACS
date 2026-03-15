using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model.ProcessType
{
    public class UnitProcessType : Entity
    {
        private string unitName = "";
        private string machineName = "";
        private string processTypeName = "";

        public UnitProcessType()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string ProcessTypeName { get { return processTypeName; } set { processTypeName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitProcessType{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", processTypeName=").Append(this.processTypeName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
