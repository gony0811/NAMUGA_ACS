using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Transfer.Model
{
    public class TransportCommandRequestEx : Entity
    {
      
        public TransportCommandRequestEx()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public virtual string MessageName { get; set; }
        public virtual string JobId { get; set; }
        public virtual string VehicleId { get; set; }
        public virtual string Dest { get; set; }
        public virtual string Description { get; set; }
        public virtual DateTime? CreateTime { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportCommandRequest{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", jobId=").Append(this.JobId);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", description=").Append(this.Description);
            sb.Append(", dest=").Append(this.Dest);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
