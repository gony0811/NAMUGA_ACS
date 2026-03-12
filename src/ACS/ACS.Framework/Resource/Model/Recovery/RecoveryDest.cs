using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Recovery
{
    public class RecoveryDest : TimedEntity
    {
        private string groupName = "";
        private string machineName = "";
        private string recoveryMachineName = "";
        private string recoveryZoneName = "";
        private string recoveryUnitName = "";
        private string recoveryDestType = "NOTDESIGNATED";
        private int recoveryOrder;

        protected string GroupName { get { return groupName; } set { groupName = value; } }
        protected string MachineName { get { return machineName; } set { machineName = value; } }
        protected string RecoveryMachineName { get { return recoveryMachineName; } set { recoveryMachineName = value; } }
        protected string RecoveryZoneName { get { return recoveryZoneName; } set { recoveryZoneName = value; } }
        protected string RecoveryUnitName { get { return recoveryUnitName; } set { recoveryUnitName = value; } }
        protected string RecoveryDestType { get { return recoveryDestType; } set { recoveryDestType = value; } }
        protected int RecoveryOrder { get { return recoveryOrder; } set { recoveryOrder = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("recoveryDest{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", recoveryMachineName=").Append(this.recoveryMachineName);
            sb.Append(", recoveryZoneName=").Append(this.recoveryZoneName);
            sb.Append(", recoveryUnitName=").Append(this.recoveryUnitName);
            sb.Append(", recoveryDestType=").Append(this.recoveryDestType);
            sb.Append(", recoveryOrder=").Append(this.recoveryOrder);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
