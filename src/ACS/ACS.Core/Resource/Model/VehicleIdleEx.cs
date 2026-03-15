using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model
{
    public class VehicleIdleEx 
    {
        public VehicleIdleEx()
        {
            Id = Guid.NewGuid().ToString();
            IdleTime = DateTime.Now;
        }

        public virtual string Id { get; set; }
        public virtual string BayId { get; set; }
        public virtual DateTime IdleTime { get; set; }
        public virtual string VehicleId { get; set; }
     
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicleIdle{");
            sb.Append("bayId=").Append(this.BayId);
            sb.Append(", id=").Append(this.Id);
            sb.Append(", idleTime=").Append(this.IdleTime);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
