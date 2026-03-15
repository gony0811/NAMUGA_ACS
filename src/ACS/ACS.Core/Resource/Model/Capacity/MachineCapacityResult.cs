using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Capacity
{
    public class MachineCapacityResult
    {
        protected string machineName = "";
        protected int occupied;
        protected int disabled;
        protected int used;
        protected int total;
        protected int usable;
        protected bool useZoneData = false;
        protected bool usedValid = false;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public int Occupied
        {
            get { return occupied; }
            set { occupied = value; }
        }

        public int Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        public int Used
        {
            get { return used; }
            set { used = value; }
        }

        public int Total
        {
            get { return total; }
            set { total = value; }
        }

        public int Usable
        {
            get { return usable; }
            set { usable = value; }
        }

        public bool IsUseZoneData
        {
            get { return useZoneData; }
            set { useZoneData = value; }
        }

        public bool IsUsedValid
        {
            get { return usedValid; }
            set { usedValid = value; }
        }

        public bool CalculateUsed
        {
            get
            {
                bool result = true;
                int used = !this.useZoneData ? this.occupied + this.disabled : this.total - this.usable;
                Used = used;
                IsUsedValid = true;

                return result;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machineUsedCapacityResult{");
            sb.Append("machineName=").Append(this.machineName);
            if (!this.useZoneData)
            {
                sb.Append(", occupied=").Append(this.occupied);
                sb.Append(", disabled=").Append(this.disabled);
                sb.Append(", used=").Append(this.used);
            }
            else
            {
                sb.Append(", total=").Append(this.total);
                sb.Append(", usable=").Append(this.usable);
                sb.Append(", used=").Append(this.used);
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
