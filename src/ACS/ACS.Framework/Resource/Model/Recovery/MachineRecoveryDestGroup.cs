using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Recovery
{
    public class MachineRecoveryDestGroup : AbstractRecoveryDestGroup
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machineRecoveryDestGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.GroupName);
            sb.Append(", machineName=").Append(this.MachineName);
            sb.Append(", selectionRule=").Append(this.SelectionRule);
            sb.Append(", lastIndex=").Append(this.LastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
