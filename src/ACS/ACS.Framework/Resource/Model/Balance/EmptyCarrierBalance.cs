using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Balance
{
    public class EmptyCarrierBalance : TimedEntity
    {
        private string storageGroupName;
        private string massStorageMachineName;
        private int requiredEmptyCarrierCount;
        private string used = "T";

        public string StorageGroupName
        {
            get { return storageGroupName; }
            set { storageGroupName = value; }
        }

        public string StorageMachineName
        {
            get { return massStorageMachineName; }
            set { massStorageMachineName = value; }
        }

        public int RequiredEmptyCarrier
        {
            get { return requiredEmptyCarrierCount; }
            set { requiredEmptyCarrierCount = value; }
        }

        public string Used
        {
            get { return used; }
            set { used = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("emptyCarrierBalance{");
            sb.Append("storageGroupName=").Append(this.storageGroupName);
            sb.Append("storageMachineName=").Append(this.massStorageMachineName);
            sb.Append("requiredEmptyCarrierCount=").Append(this.requiredEmptyCarrierCount);
            sb.Append(", used=").Append(this.used);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
