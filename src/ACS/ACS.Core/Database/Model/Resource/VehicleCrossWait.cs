using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class VehicleCrossWait : Entity
{
    public static string STATE_WAIT = "WAIT";
    public static string STATE_GOING = "GOING";

    public virtual string NodeId { get; set; }
    public virtual string VehicleId { get; set; }
    public virtual string State { get; set; }
    public virtual DateTime CreatedTime { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("vehicleCrossWait{");
        sb.Append(", vehicleId=").Append(this.VehicleId);
        sb.Append(", nodeId=").Append(this.NodeId);
        sb.Append(", state=").Append(this.State);
        sb.Append(", createdTime=").Append(this.CreatedTime);
        sb.Append("}");
        return sb.ToString();
    }
}
