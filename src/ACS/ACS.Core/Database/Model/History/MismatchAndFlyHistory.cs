using System.Text;

namespace ACS.Core.Database.Model.History;

public class MismatchAndFlyHistory : AbstractHistory
{
    public MismatchAndFlyHistory()
    {
        this.PartitionId = CreatePartitionIdByDate();
    }

    public MismatchAndFlyHistory(int partitionId)
    {
        this.PartitionId = partitionId;
    }

    public virtual string VehicleId { get; set; }
    public virtual string CurrentNodeId { get; set; }
    public virtual string NgNode { get; set; }
    public virtual string Type { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("MismatchAndFlyHistory{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", vehicleId=").Append(this.VehicleId);
        sb.Append(", currentNodeId=").Append(this.CurrentNodeId);
        sb.Append(", nodeNg=").Append(this.NgNode);
        sb.Append(", type=").Append(this.Type);
        sb.Append(", time=").Append(this.Time);
        sb.Append(", partitionId=").Append(this.PartitionId);
        sb.Append("}");
        return sb.ToString();
    }
}
