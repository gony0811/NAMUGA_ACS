using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Path;

public class WaitPointView : Entity
{
    public static string TYPE_SWAIT_P = "S_WAIT_P";
    public static string TYPE_AWAIT_P = "A_WAIT_P";
    public static string TYPE_ABNORMAL = "B_WAIT_P";

    public virtual string Type { get; set; }
    public virtual string ZoneId { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("WaitPointView{");
        sb.Append("type=").Append(this.Type);
        sb.Append(", zoneId=").Append(this.ZoneId);
        sb.Append("}");
        return sb.ToString();
    }
}
