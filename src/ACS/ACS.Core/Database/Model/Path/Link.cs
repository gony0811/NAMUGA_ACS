using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Path;

public class Link : Entity
{
    public static string AVAILABILITY_AVAILABLE = "0";
    public static string AVAILABILITY_UNAVAILABLE = "1";
    public static string AVAILABILITY_BANNED = "2";
    public static string AVAILABLE = "AVAILABLE";
    public static string UNAVAILABLE = "UNAVAILABLE";

    public virtual string FromNodeId { get; set; }
    public virtual string ToNodeId { get; set; }
    public virtual string Availability { get; set; }
    public virtual int Length { get; set; }
    public virtual int Speed { get; set; }
    public virtual string AgvType { get; set; }
    public virtual int Load { get; set; }
    public virtual int LeftBranch { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("link{");
        sb.Append(", id=").Append(this.Id);
        sb.Append(", fromNodeId=").Append(this.FromNodeId);
        sb.Append(", toNodeId=").Append(this.ToNodeId);
        sb.Append(", availability=").Append(this.Availability);
        sb.Append(", length=").Append(this.Length);
        sb.Append(", load=").Append(this.Load);
        sb.Append(", speed=").Append(this.Speed);
        sb.Append(", leftBranch=").Append(this.LeftBranch);
        sb.Append("}");
        return sb.ToString();
    }
}
