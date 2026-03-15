using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class OrderPairNode : Entity
{
    public virtual string OrderGroup { get; set; }
    public virtual string Status { get; set; }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("OrderPairNodeACS [id=");
        builder.Append(Id);
        builder.Append(", orderGroup=");
        builder.Append(OrderGroup);
        builder.Append(", status=");
        builder.Append(Status);
        builder.Append("]");
        return builder.ToString();
    }
}
