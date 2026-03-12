using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Recovery
{
    public class UnitRecoveryDest : RecoveryDest
    {
        private String unitName = "";

        protected string UnitName { get { return unitName; } set { unitName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitRecoveryDest{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.GroupName);
            sb.Append(", machineName=").Append(this.MachineName);
            sb.Append(", unitName=").Append(this.UnitName);
            sb.Append(", recoveryMachineName=").Append(this.RecoveryMachineName);
            sb.Append(", recoveryZoneName=").Append(this.RecoveryZoneName);
            sb.Append(", recoveryUnitName=").Append(this.RecoveryUnitName);
            sb.Append(", recoveryDestType=").Append(this.RecoveryDestType);
            sb.Append(", recoveryOrder=").Append(this.RecoveryOrder);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
