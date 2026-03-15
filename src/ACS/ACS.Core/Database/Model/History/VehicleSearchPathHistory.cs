using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.History;

public class VehicleSearchPathHistory : PartitionedEntity
{
    public static string TYPE_ORIGINAL = "Original";

    public VehicleSearchPathHistory()
    {
        this.PartitionId = this.CreatePartitionIdByDate();
    }

    public virtual string TransferState { get; set; }
    public virtual int Distance { get; set; }
    public virtual string TrCmd { get; set; }
    public virtual string Type { get; set; }
    public virtual string VehicleId { get; set; }
    public virtual string BayId { get; set; }
    public virtual string CurrentNodeId { get; set; }
    public virtual string Path { get; set; }

    public override string ToString()
    {
        return "VehicleSeachPath [BayId=" + BayId + ", currentNodeId="
                + CurrentNodeId + ", path=" + Path + ", vehicleId=" + VehicleId
                + "]";
    }
}
