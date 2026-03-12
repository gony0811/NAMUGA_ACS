using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.History.Model;

namespace ACS.Framework.History.Model
{
    public class VehicleCrossWaitHistoryEx : AbstractHistoryEx
    {
        public VehicleCrossWaitHistoryEx()
        {
            this.PartitionId = CreatePartitionIdByDate();
        }

        public VehicleCrossWaitHistoryEx(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public virtual string VehicleId { get; set; }
        public virtual string NodeId { get; set; }

        public virtual string State { get; set; }

        public virtual DateTime CreateTime { get; set; }

        public virtual DateTime PermitTime { get; set; }
        public virtual int CrossWaitSeconds { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("vehicleCrossWaitHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", nodeId=").Append(this.NodeId);
            sb.Append(", state=").Append(this.State);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", permitTime=").Append(this.PermitTime);
            sb.Append(", crossWaitSeconds=").Append(this.CrossWaitSeconds);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");

            return sb.ToString();
        }

    }
}
