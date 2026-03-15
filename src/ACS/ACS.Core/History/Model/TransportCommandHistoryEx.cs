using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class TransportCommandHistoryEx : AbstractHistoryEx
    {      
        public TransportCommandHistoryEx()
        {
            this.PartitionId = base.CreatePartitionIdByDate();
        }

        public virtual string JobId { get; set; }
        public virtual int Priority { get; set; }
        public virtual string State { get; set; }
        public virtual string VehicleId { get; set; }
        public virtual string VehicleEvent { get; set; }
        public virtual string CarrierId { get; set; }
        public virtual string Source { get; set; }
        public virtual string Dest { get; set; }
        public virtual string Path { get; set; }
        public virtual string AdditionalInfo { get; set; }
        public virtual DateTime? CreateTime { get; set; }
        public virtual DateTime? QueuedTime { get; set; }
        public virtual DateTime? AssignedTime { get; set; }
        public virtual DateTime? StartedTime { get; set; }
        public virtual DateTime? LoadArrivedTime { get; set; }
        public virtual DateTime? LoadedTime { get; set; }
        public virtual DateTime? UnloadArrivedTime { get; set; }
        public virtual DateTime? UnloadedTime { get; set; }
        public virtual DateTime? LoadingTime { get; set; }
        public virtual DateTime? UnloadingTime { get; set; }
        public virtual DateTime? CompletedTime { get; set; }
        public virtual string EqpId { get; set; }
        public virtual string PortId { get; set; }
        public virtual string AgvName { get; set; }
        public virtual string JobType { get; set; }
        public virtual string MidLoc { get; set; }
        public virtual string MidPortId { get; set; }
        public virtual string OriginLoc { get; set; }
        public virtual string Description { get; set; }
        public virtual string BayId { get; set; }
        public virtual string Reason { get; set; }
        public virtual string Code { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportCommand{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", priority=").Append(this.Priority);
            sb.Append(", state=").Append(this.State);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", vehicleEvent=").Append(this.VehicleEvent);
            sb.Append(", carrierId=").Append(this.CarrierId);
            sb.Append(", source=").Append(this.Source);
            sb.Append(", dest=").Append(this.Dest);
            sb.Append(", path=").Append(this.Path);
            sb.Append(", additionalInfo=").Append(this.AdditionalInfo);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", queuedTime=").Append(this.QueuedTime);
            sb.Append(", assignedTime=").Append(this.AssignedTime);
            sb.Append(", loadArrivedTime=").Append(this.LoadArrivedTime);
            sb.Append(", loadingTime=").Append(this.LoadingTime);
            sb.Append(", loadedTime=").Append(this.LoadedTime);
            sb.Append(", unloadArrivedTime=").Append(this.UnloadArrivedTime);
            sb.Append(", unloadingTime=").Append(this.UnloadingTime);
            sb.Append(", unloadedTime=").Append(this.UnloadedTime);
            sb.Append(", completedTime=").Append(this.CompletedTime);
            sb.Append(", eqpId=").Append(this.EqpId);
            sb.Append(", portId=").Append(this.PortId);
            sb.Append(", agvName=").Append(this.AgvName);
            sb.Append(", jobType=").Append(this.JobType);
            sb.Append(", midLoc=").Append(this.MidLoc);
            sb.Append(", midPortId=").Append(this.MidPortId);
            sb.Append(", originLoc=").Append(this.OriginLoc);
            sb.Append(", description=").Append(this.Description);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", code=").Append(this.Code);
            sb.Append(", reason=").Append(this.Reason);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
