using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Recovery
{
    public class RecoveryDestGroup : RecoveryDest
    {
        public static string SELECTIONRULE_ROUNDROBIN = "ROUNDROBIN";
        public static string SELECTIONRULE_FIXEDORDER = "FIXEDORDER";
        public static string SELECTIONRULE_FULLRATE = "FULLRATE";
        private string selectionRule = "FIXEDORDER";
        private int lastIndex = 0;

        protected string SelectionRule { get { return selectionRule; } set { selectionRule = value; } }
        protected int LastIndex { get { return lastIndex; } set { lastIndex = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("recoveryDestGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.GroupName);
            sb.Append(", recoveryMachineName=").Append(this.RecoveryMachineName);
            sb.Append(", recoveryZoneName=").Append(this.RecoveryZoneName);
            sb.Append(", recoveryUnitName=").Append(this.RecoveryUnitName);
            sb.Append(", recoveryDestType=").Append(this.RecoveryDestType);
            sb.Append(", recoveryOrder=").Append(this.RecoveryOrder);
            sb.Append(", selectionRule=").Append(this.SelectionRule);
            sb.Append(", lastIndex=").Append(this.LastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
