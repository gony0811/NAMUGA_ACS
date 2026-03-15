using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Material;

public class Carrier : Entity
{
    public static string UNKNOWN_TYPE_UNK = "UNK";
    public static string CARRIER_TYPE_TRAY = "TRAY";

    public virtual string Type { get; set; }
    public virtual string CarrierLoc { get; set; }
    public virtual DateTime? CreateTime { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("carrier{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", type=").Append(this.Type);
        sb.Append(", carrierLoc=").Append(this.CarrierLoc);
        sb.Append(", createTime=").Append(this.CreateTime);
        sb.Append("}");
        return sb.ToString();
    }
}
