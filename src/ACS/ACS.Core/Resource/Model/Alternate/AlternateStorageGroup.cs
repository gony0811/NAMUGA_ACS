using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Alternate
{
    public class AlternateStorageGroup : AlternateStorage
    {
        public static string SELECTIONRULE_ROUNDROBIN = "ROUNDROBIN";
        public static string SELECTIONRULE_FIXEDORDER = "FIXEDORDER";
        public static string SELECTIONRULE_FULLRATE = "FULLRATE";
        protected string selectionRule = "FIXEDORDER";
        protected int lastIndex = 0;

        public string SelectionRule
        {
            get { return selectionRule; }
            set { selectionRule = value; }
        }

        public int LastIndex
        {
            get { return lastIndex; }
            set { lastIndex = value; }
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alternateStroageGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", alternateStorageMachineName=").Append(this.alternateStorageMachineName);
            sb.Append(", alternateStorageZoneName=").Append(this.alternateStorageZoneName);
            sb.Append(", alternatePriority=").Append(this.alternatePriority);
            sb.Append(", changeDestination=").Append(this.changeDestination);
            sb.Append(", completeJobWhenStored=").Append(this.completeJobWhenStored);
            sb.Append(", selectionRule=").Append(this.selectionRule);
            sb.Append(", lastIndex=").Append(this.lastIndex);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
