using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Alternate
{
    public class ZoneAlternateStorageGroup : AbstractAlternateStorageGroup
    {
        private string zoneName;

        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneAlternateStroageGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", selectionRule=").Append(this.selectionRule);
            sb.Append(", lastIndex=").Append(this.lastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
