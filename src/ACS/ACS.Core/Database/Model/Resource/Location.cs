using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class Location : Entity
{
    public static string TYPE_CHARGE = "CHARGE";
    public static string TYPE_STOCK = "STOCK";

    public virtual string PortId { get; set; }
    public virtual string StationId { get; set; }
    public virtual string Type { get; set; }
    public virtual string CarrierType { get; set; }
    public virtual string State { get; set; }
    public virtual string Direction { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("location{");
        sb.Append("portId=").Append(this.PortId);
        sb.Append(", stationId=").Append(this.StationId);
        sb.Append(", type=").Append(this.Type);
        sb.Append(", carrierType=").Append(this.CarrierType);
        sb.Append(", state=").Append(this.State);
        sb.Append(", direction=").Append(this.Direction);
        sb.Append("}");
        return sb.ToString();
    }
}
