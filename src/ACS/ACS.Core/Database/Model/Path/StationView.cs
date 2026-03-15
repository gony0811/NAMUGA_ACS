using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Path;

public class StationView : Entity
{
    public virtual string LinkId { get; set; }
    public virtual string ParentNode { get; set; }
    public virtual string NextNode { get; set; }
    public virtual string Type { get; set; }
    public virtual int Distance { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("station{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", linkId=").Append(this.LinkId);
        sb.Append(", parentNode=").Append(this.ParentNode);
        sb.Append(", nextNode=").Append(this.NextNode);
        sb.Append(", type=").Append(this.Type);
        sb.Append(", distance=").Append(this.Distance);
        sb.Append("}");
        return sb.ToString();
    }
}
