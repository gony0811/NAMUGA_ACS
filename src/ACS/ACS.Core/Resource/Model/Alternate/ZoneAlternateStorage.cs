using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Alternate
{
    public class ZoneAlternateStorage : AlternateStorage
    {
        private string zoneName = "";
        
        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneAlternateStorage{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", zoneName=").Append(this.zoneName);
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
