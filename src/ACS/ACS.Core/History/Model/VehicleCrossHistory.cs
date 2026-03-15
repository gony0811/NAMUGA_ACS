using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.History.Model
{
    public class VehicleCrossHistory : PartitionedEntity
    {
        //private static long serialVersionUID = 4969518740488848233L;


        public String VehicleId { get; set; }
        public String StartNodeId { get; set; }
        public String StartTime { get; set; }
        public String EndNodeId { get; set; }
        public String EndTime { get; set; }


        public VehicleCrossHistory()
        {
            PartitionId = base.CreatePartitionIdByDate();
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("VehicleCrossHistory [");
            sb.Append("endNodeId=").Append(EndNodeId);
            sb.Append(", endTime=").Append(EndTime);
            sb.Append(", startNodeId=").Append(StartNodeId);
            sb.Append(", startTime=").Append(StartTime);
            sb.Append(", vehicleID=").Append(VehicleId);
            sb.Append("]");
            return sb.ToString();
        }
    }
}
