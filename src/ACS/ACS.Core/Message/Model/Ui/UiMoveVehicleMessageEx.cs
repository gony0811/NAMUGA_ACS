using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message.Model;

namespace ACS.Core.Message.Model.Ui
{
    public class UiMoveVehicleMessageEx : AbstractMessage
    {
        private static long serialVersionUID = 1L;
        public static string MESSAGE_NAME_UIMOVEVEHICLE = "UI-MOVEVEHICLE";
        public static string TRUE = "T";
        public static string FALSE = "F";
        private string vehicleId;
        private string nodeId;
        private string requestId;
        private string keyData;

        public string NodeId { get{ return nodeId;} set { nodeId = value;}}
        public string RequestId { get{ return requestId;} set { requestId = value;}}
        public string VehicleId { get{ return vehicleId;} set { vehicleId = value;}}
        public string KeyData { get { return keyData; } set { keyData = value; } }
        public string Cause { get { return base.Cause; } set { base.Cause = value; } }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UiTransportMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", vehicleId=").Append(this.vehicleId);
            sb.Append(", nodeId=").Append(this.nodeId);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", requestId=").Append(this.requestId);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
