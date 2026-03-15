using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Alternate
{
    public class MachineAlternateStorage : AlternateStorage
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machineAlternateStorage{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", alternateStorageMachineName=").Append(this.alternateStorageMachineName);
            sb.Append(", alternateStorageZoneName=").Append(this.alternateStorageZoneName);
            sb.Append(", alternatePriority=").Append(this.alternatePriority);
            sb.Append(", changeDestination=").Append(this.changeDestination);
            sb.Append(", completeJobWhenStored=").Append(this.completeJobWhenStored);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
