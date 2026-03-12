using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Factory.Alias
{
    public class MachineAlias : Entity
    {
        private string machineName;
        private string aliasName;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string AliasName
        {
            get { return aliasName; }
            set { aliasName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machineAlias{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", aliasName=").Append(this.aliasName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
