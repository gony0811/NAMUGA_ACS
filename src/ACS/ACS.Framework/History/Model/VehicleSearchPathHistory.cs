using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.History.Model
{
    public class VehicleSearchPathHistory : PartitionedEntity
    {
        public static string TYPE_ORIGINAL = "Original";

        public VehicleSearchPathHistory()
        {
            this.PartitionId = this.CreatePartitionIdByDate();
        
        }

        public virtual String TransferState { get; set; }

        public virtual int Distance { get; set; }
        public virtual String TrCmd { get; set; }

        public virtual String Type { get; set; }


        public virtual String VehicleId { get; set; }

        public virtual String BayId { get; set; }

        public virtual String CurrentNodeId { get; set; }

        public virtual String Path { get; set; }
                
        public override String ToString()
        {
            return "VehicleSeachPath [BayId=" + BayId + ", currentNodeId="
                    + CurrentNodeId + ", path=" + Path + ", vehicleId=" + VehicleId
                    + "]";
        }
    }
}
