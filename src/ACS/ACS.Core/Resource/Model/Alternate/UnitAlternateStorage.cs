using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Alternate
{
    public class UnitAlternateStorage : AlternateStorage
    {
        private string unitName = "";

        public string UnitName
        {
            get { return unitName; }
            set { unitName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitAlternateStorage{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
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
