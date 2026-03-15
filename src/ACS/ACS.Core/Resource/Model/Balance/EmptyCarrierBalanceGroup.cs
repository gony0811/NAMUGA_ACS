using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Balance
{
    public class EmptyCarrierBalanceGroup : TimedEntity
    {
        private string massStorageMachineName;
        private int requiredCount;
        private string oppositeStorageMachineName;
        private int oppositeRequiredCount;
        private string used = "T";

        public int RequiredCount
        {
            get { return requiredCount; }
            set { requiredCount = value; }
        }

        public int OppositeRequiredCount
        {
            get { return oppositeRequiredCount; }
            set { oppositeRequiredCount = value; }
        }

        public string Used
        {
            get { return used; }
            set { used = value; }
        }

        public string StorageMachineName
        {
            get { return massStorageMachineName; }
            set { massStorageMachineName = value; }
        }

        public string OppositeStorageMachineName
        {
            get { return oppositeStorageMachineName; }
            set { oppositeStorageMachineName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("emptyCarrierBalance{");
            sb.Append("storageMachineName=").Append(this.massStorageMachineName);
            sb.Append("requiredCount=").Append(this.requiredCount);
            sb.Append(", oppositeStorageMachineName=").Append(this.oppositeStorageMachineName);
            sb.Append(", oppositeRequiredCount=").Append(this.oppositeRequiredCount);
            sb.Append(", used=").Append(this.used);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
