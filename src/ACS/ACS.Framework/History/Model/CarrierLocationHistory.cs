using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.History.Model
{
    public class CarrierLocationHistory: AbstractHistory
    {
        private string transportCommandId = "";
        private string carrierName = "";
        private string lotId = "";
        private string currentMachineName = "";
        private string currentUnitName = "";
        private string state = "";

        public string TransportCommandId { get {return transportCommandId; } set { transportCommandId = value; } }
        public string CarrierName { get {return carrierName; } set { carrierName = value; } }
        public string LotId { get {return lotId; } set { lotId = value; } }
        public string CurrentMachineName { get {return currentMachineName; } set { currentMachineName = value; } }
        public string CurrentUnitName { get {return currentUnitName; } set { currentUnitName = value; } }
        public string State { get { return state; } set { state = value; } }

        public CarrierLocationHistory()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }

        public CarrierLocationHistory(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public int GetPartitionId()
        {
            return base.PartitionId;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carrierLocationHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", transportCommandId=").Append(this.transportCommandId);
            sb.Append(", carrierName=").Append(this.carrierName);
            sb.Append(", lotId=").Append(this.lotId);
            sb.Append(", currentMachineName=").Append(this.currentMachineName);
            sb.Append(", currentUnitName=").Append(this.currentUnitName);
            sb.Append(", state=").Append(this.state);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
