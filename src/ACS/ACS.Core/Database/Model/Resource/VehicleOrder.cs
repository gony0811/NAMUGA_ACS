using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class VehicleOrder : Entity
{
    public static string TYPE_ORDER = "ORDER";
    public static string ORDER_NODE_TURN_LEFT_RUN = "ORDER_LF";
    public static string ORDER_NODE_TURN_RIGHT_RUN = "ORDER_RF";
    public static string ORDER_NODE_TURN_LEFT_BACK = "ORDER_LB";
    public static string ORDER_NODE_TURN_RIGHT_BACK = "ORDER_RB";
    public static string ORDER_NODE_CHANGE_LINE_LEFT = "ORDER_CL";
    public static string ORDER_NODE_CHANGE_LINE_RIGHT = "ORDER_CR";

    public virtual string Reply { get; set; }
    public virtual string VehicleId { get; set; }
    public virtual DateTime? OrderTime { get; set; }
    public virtual string OrderNode { get; set; }

    public VehicleOrder()
    {
        Id = Guid.NewGuid().ToString();
        VehicleId = "";
        OrderTime = new DateTime();
        OrderNode = "";
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("VehicleOrderACS [id=");
        builder.Append(Id);
        builder.Append(", orderNode=");
        builder.Append(OrderNode);
        builder.Append(", orderTime=");
        builder.Append(OrderTime);
        builder.Append(", vehicleId=");
        builder.Append(VehicleId);
        builder.Append("]");
        return builder.ToString();
    }
}
