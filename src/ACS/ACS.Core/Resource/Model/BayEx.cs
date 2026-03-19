using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model
{
    public class BayEx
    {
        public virtual long Id { get; set; }
        public virtual string BayId { get; set; }
        public virtual int Floor { get; set; }
        public virtual string Description { get; set; }
        public virtual string AgvType { get; set; }       //20200310 LYS Add on Function for Parking Station
        public virtual float ChargeVoltage { get; set; }
        public virtual float LimitVoltage { get; set; }
        public virtual int IdleTime { get; set; }

        public virtual string ZoneMove { get; set; }
        public virtual string Traffic { get; set; }
        public virtual string StopOut { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("bay{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", description=").Append(this.Description);
            sb.Append(", floor=").Append(this.Floor);
            sb.Append(", agvType=").Append(this.AgvType);
            sb.Append(", chargeVoltage=").Append(this.ChargeVoltage);
            sb.Append(", limitVoltage=").Append(this.LimitVoltage);
            sb.Append(", idleTime=").Append(this.IdleTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
