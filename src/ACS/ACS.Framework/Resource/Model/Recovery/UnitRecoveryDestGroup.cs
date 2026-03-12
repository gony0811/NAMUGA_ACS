using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Recovery
{
    public class UnitRecoveryDestGroup : AbstractRecoveryDestGroup
    {
        private string unitName;

        public string UnitName { get { return unitName; } set { unitName = value; } }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitRecoveryDestGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", machineName=").Append(this.MachineName);
            sb.Append(", groupName=").Append(this.GroupName);
            sb.Append(", selectionRule=").Append(this.SelectionRule);
            sb.Append(", lastIndex=").Append(this.LastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
