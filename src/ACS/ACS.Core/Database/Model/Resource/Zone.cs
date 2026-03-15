using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class Zone : Entity
{
    public virtual string BayId { get; set; }
    public virtual string Description { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("zone{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", bayId=").Append(this.BayId);
        sb.Append(", description=").Append(this.Description);
        sb.Append("}");
        return sb.ToString();
    }
}
