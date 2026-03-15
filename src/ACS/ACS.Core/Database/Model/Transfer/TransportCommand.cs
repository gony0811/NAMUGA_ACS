using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Transfer;

public class TransportCommand : Entity
{
    public static int DEFAULT_PRIORITY = 3;
    public static string STATE_CREATED = "CREATED";
    public static string STATE_QUEUED = "QUEUED";
    public static string STATE_WAITING = "WAITING";
    public static string STATE_PREASSIGNED = "PREASSIGNED";
    public static string STATE_ASSIGNED = "ASSIGNED";
    public static string STATE_ARRIVED_SOURCE = "ARRIVED_SOURCE";
    public static string STATE_ARRIVED_DEST = "ARRIVED_DEST";
    public static string STATE_TRANSFERRING_SOURCE = "TRANSFERRING_SOURCE";
    public static string STATE_TRANSFERRING_DEST = "TRANSFERRING_DEST";
    public static string STATE_COMPLETED = "COMPLETED";
    public static string STATE_CANCELING = "CANCELING";
    public static string STATE_CANCELED = "CANCELED";
    public static string STATE_ABORTING = "ABORTING";
    public static string STATE_ABORTED = "ABORTED";
    public static string STATE_COMPLETEFAILED = "COMPLETEFAILED";
    public static string STATE_CHARGE_COMPLETED = "CHARGECOMPLETED";
    public static string STATE_CHANGE_VEHICLE = "CHANGEVEHICLE";
    public static string TYPE_CMDREPLY = "MOVECMD_REP";
    public static string TYPE_CMDUPDATEREPLY = "MOVEUPDATE_REP";
    public static string TYPE_JOBSTART = "JOBSTART";
    public static string TYPE_PICKUP = "PICKUP";
    public static string TYPE_START = "START";
    public static string TYPE_COMPLETE = "COMPLETE";
    public static string TYPE_CANCEL = "CANCEL";
    public static string TYPE_RECEIVE = "RECEIVE";
    public static string TYPE_ARRIVED = "ARRIVED";
    public static string TYPE_PICKUPED = "PICKUPED";
    public static string TYPE_SOURCE_ARRIVED = "SOURCEARRIVED";
    public static string TYPE_DEST_ARRIVED = "DESTARRIVED";
    public static string JOBTYPE_AUTOCALL = "AUTOCALL";
    public static string JOBTYPE_ACSCALL = "ACSCALL";
    public static string JOBTYPE_ACSMOVE = "ACSMOVE";
    public static string JOBTYPE_CHARGEMOVE = "CHARGEMOVE";
    public static string JOBTYPE_STOCK_STATION = "STOCKSTATION";
    public static string CAUSE_REQ_TIMEOUT = "TRANSPORTCOMMANDREQUESTTIMEOUT";
    public static string CAUSE_MOVECANCEL = "MOVECANCEL";
    public static string CAUSE_COMPLETE = "COMPLETE";

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

    public TransportCommand()
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
