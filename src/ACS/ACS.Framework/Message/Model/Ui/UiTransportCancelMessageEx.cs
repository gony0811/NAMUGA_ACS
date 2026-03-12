using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model;

namespace ACS.Framework.Message.Model.Ui
{
    public class UiTransportCancelMessageEx : AbstractMessage
    {
        private static long serialVersionUID = 1L;
        public static string MESSAGE_NAME_UITRANSPORTCANCEL = "UI-TRANSPORT-CANCEL";
        public static string TRUE = "T";
        public static string FALSE = "F";
        private string transportCommandId;
        private string requestId;
        private string keyData;

        public string TransportCommandId { get{ return transportCommandId;} set { transportCommandId = value;}}
        public string RequestId { get{ return requestId;} set { requestId = value;}}
        public string KeyData { get { return keyData; } set { keyData = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UiTransportMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", transportCommandId=").Append(this.transportCommandId);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", requestId=").Append(this.requestId);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
