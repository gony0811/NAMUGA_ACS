using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Alternate
{
    public class UnitAlternateStorageGroup : AbstractAlternateStorageGroup
    {
        private string unitName;

        public string UnitName
        {
            get { return unitName; }
            set { unitName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitAlternateStroageGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", selectionRule=").Append(this.selectionRule);
            sb.Append(", lastIndex=").Append(this.lastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
