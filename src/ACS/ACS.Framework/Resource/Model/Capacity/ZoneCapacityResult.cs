using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Capacity
{
    public class ZoneCapacityResult
    {
        protected string machineName;
        protected string zoneName;
        protected string type;
        protected string zoneType;
        protected int total;
        protected int occupied;
        protected int disabled;
        protected int reserved;
        protected int used;
        protected int empty;
        protected int usable;
        protected bool valid = false;
        protected bool usedValid = false;
        protected bool emptyValid = false;
        protected bool usableValid = false;

        public ZoneCapacityResult()
        {
            this.valid = false;

            this.machineName = "";
            this.zoneName = "";
            this.type = "";
            this.zoneType = "";

            this.total = 0;
            this.occupied = 0;
            this.disabled = 0;
            this.reserved = 0;
        }

        public ZoneCapacityResult(string machineName, string zoneName, string type, string zoneType, long total, long occupied, long disabled)
        {
            this.valid = true;

            this.machineName = machineName;
            this.zoneName = zoneName;
            this.type = type;
            this.zoneType = zoneType;
            this.total = Convert.ToInt32(total);
            this.occupied = Convert.ToInt32(occupied);
            this.disabled = Convert.ToInt32(disabled);
            this.reserved = 0;

            calculateUsed();
            calculateEmpty();
        }

        public ZoneCapacityResult(string machineName, string zoneName, string type, string zoneType, long total, long occupied, long disabled, long reserved)
        {
            this.valid = true;

            this.machineName = machineName;
            this.zoneName = zoneName;
            this.type = type;
            this.zoneType = zoneType;
            this.total = Convert.ToInt32(total);
            this.occupied = Convert.ToInt32(occupied);
            this.disabled = Convert.ToInt32(disabled);
            this.reserved = Convert.ToInt32(reserved);

            calculateUsed();
            calculateEmpty();
            calculateUsable();
        }

        public bool calculateUsed()
        {
            bool result = true;

            this.used = (this.occupied + this.disabled);
            this.usedValid = true;

            return result;
        }

        public bool calculateEmpty()
        {
            bool result = true;

            this.empty = (this.total - (this.occupied + this.disabled));
            this.emptyValid = true;

            return result;
        }

        public bool calculateUsable()
        {
            bool result = true;

            this.usable = (this.total - (this.occupied + this.disabled + this.reserved));
            this.usableValid = true;

            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneCapacityResult{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", zoneType=").Append(this.zoneType);
            sb.Append(", valid=").Append(this.valid);
            sb.Append(", total=").Append(this.total);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", disabled=").Append(this.disabled);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", usedValid=").Append(this.usedValid);
            sb.Append(", used=").Append(this.used);
            sb.Append(", emptyValid=").Append(this.emptyValid);
            sb.Append(", empty=").Append(this.empty);
            sb.Append(", usableValid=").Append(this.usableValid);
            sb.Append(", usable=").Append(this.usable);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
