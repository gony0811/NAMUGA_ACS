using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.History.Model
{
    public class VehicleHistoryEx : AbstractHistoryEx
    {
        public VehicleHistoryEx()
        {
            this.PartitionId = CreatePartitionIdByDate();
        }

        public VehicleHistoryEx(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public virtual string VehicleId { get; set; }
        public virtual string BayId { get; set; }
        public virtual string CarrierType { get; set; }
        public virtual string ConnectionState { get; set; }
        public virtual string AlarmState { get; set; }
        public virtual string ProcessingState { get; set; }
        public virtual string CurrentNodeId { get; set; }
        public virtual string TransportCommandId { get; set; }
        public virtual string Path { get; set; }
        public virtual DateTime? NodeCheckTime { get; set; }
        public virtual string State { get; set; }
        public virtual string Installed { get; set; }
        public virtual string TransferState { get; set; }
        public virtual string RunState { get; set; }
        public virtual string FullState { get; set; }
        public virtual string MessageName { get; set; }
        public virtual string AcsDestNodeId { get; set; }
        public virtual string VehicleDestNodeId { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("vehicleHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", carrierType=").Append(this.CarrierType);
            sb.Append(", connectionState=").Append(this.ConnectionState);
            sb.Append(", alarmState=").Append(this.AlarmState);
            sb.Append(", processingState=").Append(this.ProcessingState);
            sb.Append(", currentNodeId=").Append(this.CurrentNodeId);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", path=").Append(this.Path);
            sb.Append(", nodeCheckTime=").Append(this.NodeCheckTime);
            sb.Append(", state=").Append(this.State);
            sb.Append(", installed=").Append(this.Installed);
            sb.Append(", transferState=").Append(this.TransferState);
            sb.Append(", runState=").Append(this.RunState);
            sb.Append(", fullState=").Append(this.FullState);
            sb.Append(", time=").Append(this.Time);
            sb.Append(", messageName=").Append(this.MessageName);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
