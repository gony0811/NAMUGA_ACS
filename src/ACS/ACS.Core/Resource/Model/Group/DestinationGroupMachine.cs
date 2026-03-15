using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Group
{
    public class DestinationGroupMachine : TimedEntity
    {
        private string groupName;
        private string machineName;
        private string zoneName;
        private int priority;

        public string GroupName
        {
            get { return groupName; }
            set { groupName = value; }
        }

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string ZoneName { get { return zoneName; } set { zoneName = value; } }
        public int Priority { get { return priority; } set { priority = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("destinationGroupMachine{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", groupName=").Append(this.groupName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", description=").Append(this.Description);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
