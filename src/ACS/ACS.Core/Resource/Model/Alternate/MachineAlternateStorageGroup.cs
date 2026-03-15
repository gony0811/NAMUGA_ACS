using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Alternate
{
    public class MachineAlternateStorageGroup : AbstractAlternateStorageGroup
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machineAlternateStroageGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", selectionRule=").Append(this.selectionRule);
            sb.Append(", lastIndex=").Append(this.lastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
