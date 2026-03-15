using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Group
{
    public class DestinationGroup : TimedEntity
    {
        public static string SELECTIONRULE_ROUNDROBIN = "ROUNDROBIN";
        public static string SELECTIONRULE_FIXEDORDER = "FIXEDORDER";
        public static string SELECTIONRULE_FULLRATE = "FULLRATE";
        public static string GROUPTYPE_STORAGE = "STORAGE";
        public static string GROUPTYPE_PORT = "PORT";
        private string groupType = "STORAGE";
        private string portGroupName = "";
        private string groupName;
        private string selectionRule = "ROUNDROBIN";
        private int lastIndex = 0;

        public string GroupName
        {
            get { return groupName; }
            set { groupName = value; }
        }

        public string GroupType
        {
            get { return groupType; }
            set { groupType = value; }
        }

        public string SelectionRule
        {
            get { return selectionRule; }
            set { selectionRule = value; }
        }

        public string PortGroupName
        {
            get { return portGroupName; }
            set { portGroupName = value; }
        }

        public int LastIndex
        {
            get { return lastIndex; }
            set { lastIndex = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("destinationGroup{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupType=").Append(this.groupType);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", portGroupName=").Append(this.portGroupName);
            sb.Append(", selectionRule=").Append(this.selectionRule);
            sb.Append(", lastIndex=").Append(this.lastIndex);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", description=").Append(this.Description);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
