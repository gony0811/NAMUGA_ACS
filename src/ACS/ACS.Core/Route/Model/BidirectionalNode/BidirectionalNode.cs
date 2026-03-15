using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model.BidirectionalNode
{
    public class BidirectionalNode : Entity
    {
        public static string DIRECTION_FORWARD = "FORWARD";
        public static string DIRECTION_BACKWARD = "BACKWARD";
        public static string DIRECTION_CHANGING = "CHANGING";
        public static string TYPE_NOT_APPLICABLE = "N/A";
        public static string TYPE_BETWEEN_STORAGE_AND_RAIL = "BETWEENSTORAGEANDRAIL";
        /**
         * @deprecated
         */
        public static string TYPE_BETWEEN_RAIL = "BETWEENRAIL";
        public static string TYPE_BETWEEN_RAIL_MAIN_CONTROLLED = "BETWEENRAILMAINCONTROLLED";
        public static string TYPE_BETWEEN_RAIL_EACH_CONTROLLED = "BETWEENRAILEACHCONTROLLED";
        public static string TYPE_BETWEEN_STORAGE_NOT_CONTROLLED = "BETWEENSTORAGENOTCONTROLLED";
        public static string TYPE_BETWEEN_STORAGE_MAIN_CONTROLLED = "BETWEENSTORAGEMAINCONTROLLED";
        public static string TYPE_BETWEEN_STORAGE_EACH_CONTROLLED = "BETWEENSTORAGEEACHCONTROLLED";
        private string fromTransportMachineName = "";
        private string fromLinkedUnitName = "";
        private string transportMachineName = "";
        private string toTransportMachineName = "";
        private string toLinkedUnitName = "";
        private string mainControllerUnitName = "";
        private int maxCapacity = -1;
        private int forwardInterval = 120000;
        private int backwardInterval = 120000;
        private DateTime directionChangedTime;
        private string direction = "FORWARD";
        private string used = "T";
        private string scheduling = "F";
        private string type = "N/A";

        public string FromTransportMachineName { get { return fromTransportMachineName; } set { fromTransportMachineName = value; } }
        public string FromLinkedUnitName { get { return fromLinkedUnitName; } set { fromLinkedUnitName = value; } }
        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public string ToTransportMachineName { get { return toTransportMachineName; } set { toTransportMachineName = value; } }
        public string ToLinkedUnitName { get { return toLinkedUnitName; } set { toLinkedUnitName = value; } }
        public string MainControllerUnitName { get { return mainControllerUnitName; } set { mainControllerUnitName = value; } }
        public int MaxCapacity { get { return maxCapacity; } set { maxCapacity = value; } }
        public int ForwardInterval { get { return forwardInterval; } set { forwardInterval = value; } }
        public int BackwardInterval { get { return backwardInterval; } set { backwardInterval = value; } }
        public DateTime DirectionChangedTime { get { return directionChangedTime; } set { directionChangedTime = value; } }
        public string Direction { get { return direction; } set { direction = value; } }
        public string Used { get { return used; } set { used = value; } }
        public string Scheduling { get { return scheduling; } set { scheduling = value; } }
        public string Type { get { return type; } set { type = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("bidirectionalNode{");
            sb.Append("fromTransportMachineName=").Append(this.fromTransportMachineName);
            sb.Append(", fromLinkedUnitName=").Append(this.fromLinkedUnitName);
            sb.Append(", transportMachineName=").Append(this.transportMachineName);
            sb.Append(", toTransportMachineName=").Append(this.toTransportMachineName);
            sb.Append(", toLinkedUnitName=").Append(this.toLinkedUnitName);
            sb.Append(", maxCapacity=").Append(this.maxCapacity);

            sb.Append(", forwardInterval=").Append(this.forwardInterval);
            sb.Append(", backwardInterval=").Append(this.backwardInterval);
            sb.Append(", direction=").Append(this.direction);
            sb.Append(", directionChangedTime=").Append(this.directionChangedTime);
            sb.Append(", scheduling=").Append(this.scheduling);

            sb.Append(", used=").Append(this.used);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
