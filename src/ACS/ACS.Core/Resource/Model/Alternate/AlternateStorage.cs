using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Alternate
{
    public class AlternateStorage : TimedEntity 
    {
        protected string groupName = "";
        protected string machineName = "";
        protected string alternateStorageMachineName = "";
        protected string alternateStorageZoneName = "";
        protected int alternatePriority;
        protected string changeDestination = "F";
        protected string completeJobWhenStored = "F";

        public string GroupName
        {
            get { return groupName; }
            set { groupName = value; }
        }

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string AlternateStorageMachineName
        {
            get { return alternateStorageMachineName; }
            set { alternateStorageMachineName = value; }
        }

        public string AlternateStorageZoneName
        {
            get { return alternateStorageZoneName; }
            set { alternateStorageZoneName = value; }
        }

        public int AlternatePriority
        {
            get { return alternatePriority; }
            set { alternatePriority = value; }
        }

        public string ChangeDestination
        {
            get { return changeDestination; }
            set { changeDestination = value; }
        }

        public string CompleteJobWhenStored
        {
            get { return completeJobWhenStored; }
            set { completeJobWhenStored = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alternateStorage{");
            sb.Append("id=").Append(this.Id);
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
