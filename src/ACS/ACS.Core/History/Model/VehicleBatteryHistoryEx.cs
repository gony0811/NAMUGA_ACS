using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class VehicleBatteryHistoryEx : AbstractHistoryEx
    {
        public virtual string VehicleId { get; set; }
        public virtual int BatteryRate { get; set; }
        public virtual float BatteryVoltage { get; set; }

        public virtual string ProcessingState { get; set; }


        public VehicleBatteryHistoryEx()
        {
            this.PartitionId = CreatePartitionIdByDate();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("vehicleHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", runState=").Append(this.BatteryRate);
            sb.Append(", fullState=").Append(this.BatteryVoltage);
            sb.Append(", processingState=").Append(this.ProcessingState);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
