using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Transfer.Model
{
    public class TransportCommandEx
    {
        public static int DEFAULT_PRIORITY = 3;
        public static String STATE_CREATED = "CREATED";
        public static String STATE_QUEUED = "QUEUED";
        public static String STATE_WAITING = "WAITING";
        //KSB PREASSIGN 추가
        public static String STATE_PREASSIGNED = "PREASSIGNED";

        public static String STATE_ASSIGNED = "ASSIGNED";
        public static String STATE_ARRIVED_SOURCE = "ARRIVED_SOURCE";
        public static String STATE_ARRIVED_DEST = "ARRIVED_DEST";
        public static String STATE_TRANSFERRING_SOURCE = "TRANSFERRING_SOURCE";
        public static String STATE_TRANSFERRING_DEST = "TRANSFERRING_DEST";
        public static String STATE_COMPLETED = "COMPLETED";
        public static String STATE_CANCELING = "CANCELING";
        public static String STATE_CANCELED = "CANCELED";
        public static String STATE_ABORTING = "ABORTING";
        public static String STATE_ABORTED = "ABORTED";
        public static String STATE_COMPLETEFAILED = "COMPLETEFAILED";
        public static String STATE_CHARGE_COMPLETED = "CHARGECOMPLETED";
        public static String STATE_CHANGE_VEHICLE = "CHANGEVEHICLE";
        public static String TYPE_CMDREPLY = "MOVECMD_REP";
        public static String TYPE_CMDUPDATEREPLY = "MOVEUPDATE_REP";
        public static String TYPE_JOBSTART = "JOBSTART";
        public static String TYPE_PICKUP = "PICKUP";
        public static String TYPE_START = "START";
        public static String TYPE_COMPLETE = "COMPLETE";
        public static String TYPE_CANCEL = "CANCEL";
        public static String TYPE_RECEIVE = "RECEIVE";
        public static String TYPE_ARRIVED = "ARRIVED";
        public static String TYPE_PICKUPED = "PICKUPED";
        public static String TYPE_SOURCE_ARRIVED = "SOURCEARRIVED";
        public static String TYPE_DEST_ARRIVED = "DESTARRIVED";
        public static String JOBTYPE_AUTOCALL = "AUTOCALL";
        public static String JOBTYPE_ACSCALL = "ACSCALL";
        public static String JOBTYPE_ACSMOVE = "ACSMOVE";
        public static String JOBTYPE_CHARGEMOVE = "CHARGEMOVE";
        public static String JOBTYPE_STOCK_STATION = "STOCKSTATION";
        public static String JOBTYPE_LOAD = "LOAD";
        public static String JOBTYPE_UNLOAD = "UNLOAD";
        public static String CAUSE_REQ_TIMEOUT = "TRANSPORTCOMMANDREQUESTTIMEOUT";
        public static String CAUSE_MOVECANCEL = "MOVECANCEL";
        public static String CAUSE_COMPLETE = "COMPLETE";

        public virtual long Id { get; set; }
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
        public virtual DateTime? LoadArrivedTime { get; set; }
        public virtual DateTime? LoadedTime { get; set; }
        public virtual DateTime? UnloadArrivedTime { get; set; }
        public virtual DateTime? UnloadedTime { get; set; }
        public virtual DateTime? LoadingTime { get; set; }
        public virtual DateTime? UnloadingTime { get; set; }
        public virtual DateTime? CompletedTime { get; set; }
        public virtual DateTime? StartedTime { get; set; }
        public virtual string EqpId { get; set; }
        public virtual string PortId { get; set; }
        public virtual string AgvName { get; set; }
        public virtual string JobType { get; set; }
        public virtual string MidLoc { get; set; }
        public virtual string MidPortId { get; set; }
        public virtual string OriginLoc { get; set; }
        public virtual string Description { get; set; }
        public virtual string BayId { get; set; }

        public TransportCommandEx()
        {
            CreateTime = DateTime.Now;
            QueuedTime = DateTime.Now;
            AssignedTime = DateTime.Now;
            LoadArrivedTime = DateTime.Now;
            LoadedTime = DateTime.Now;
            UnloadArrivedTime = DateTime.Now;
            UnloadedTime = DateTime.Now;
            LoadingTime = DateTime.Now;
            UnloadingTime = DateTime.Now;
            CompletedTime = DateTime.Now;
            StartedTime = DateTime.Now;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportCommand{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", jobId=").Append(this.JobId);
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
            sb.Append("}");
            return sb.ToString();
        }
    }
}
