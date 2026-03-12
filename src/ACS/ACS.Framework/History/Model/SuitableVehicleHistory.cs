using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.History.Model
{
    public class SuitableVehicleHistory : PartitionedEntity
    {
        public String BayId { get; set; }
        public String VehicleId { get; set; }
        public String TransportCommandId { get; set; }
        public String SourceNodeId { get; set; }
        public String SourcePortId { get; set; }
        public String SelectedVehicleId { get; set; }
        public String CurrentNodeId { get; set; }
        public String Cost { get; set; }

        public SuitableVehicleHistory()
        {
            PartitionId = base.CreatePartitionIdByDate();
        }

        public SuitableVehicleHistory(int partitionId)
        {
            base.PartitionId = partitionId;
        }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();

            sb.Append("suitableVehicleHistory{");
            sb.Append("id=").Append(Id);
            sb.Append(", time=").Append(this.Time);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", selectedVehicleId=").Append(this.SelectedVehicleId);
            sb.Append(", sourceNodeId=").Append(this.SourceNodeId);
            sb.Append(", sourcePortId=").Append(this.SourcePortId);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", currentNodeId=").Append(this.CurrentNodeId);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
