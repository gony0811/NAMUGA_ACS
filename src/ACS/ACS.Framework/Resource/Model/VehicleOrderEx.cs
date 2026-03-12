using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model
{
    public class VehicleOrderEx : Entity
    {
        public static String TYPE_ORDER = "ORDER";
        public static String ORDER_NODE_TURN_LEFT_RUN = "ORDER_LF"; // 07
        public static String ORDER_NODE_TURN_RIGHT_RUN = "ORDER_RF"; // 06
        public static String ORDER_NODE_TURN_LEFT_BACK = "ORDER_LB"; // 09
        public static String ORDER_NODE_TURN_RIGHT_BACK = "ORDER_RB";// 08
        public static String ORDER_NODE_CHANGE_LINE_LEFT = "ORDER_CL"; // 10
        public static String ORDER_NODE_CHANGE_LINE_RIGHT = "ORDER_CR";// 11
        public virtual string Reply { get; set; }

        public virtual string Id { get; set; }
        public virtual string VehicleId { get; set; }
        public virtual DateTime? OrderTime { get; set; }
        public virtual string OrderNode { get; set; }


        #region Public Constructor
        public VehicleOrderEx()
        {
            Id = Guid.NewGuid().ToString();
            VehicleId = "";
            OrderTime = new DateTime();
            OrderNode = "";
        }
        #endregion

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
}
