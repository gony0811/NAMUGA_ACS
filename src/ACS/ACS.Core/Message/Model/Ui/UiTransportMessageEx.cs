using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message.Model;

namespace ACS.Core.Message.Model.Ui
{
    public class UiTransportMessageEx : AbstractMessage
    {
        private static long serialVersionUID = 1L;
        public static string MESSAGE_NAME_UITRANSPORT = "UI-TRANSPORT";
        public static string TRUE = "T";
        public static string FALSE = "F";
        private string sourcePortId;
        private string destPortId;
        private string requestId;
        private string keyData;

        public string SourcePortId { get{ return sourcePortId;} set { sourcePortId = value;}}
        public string DestPortId { get{ return destPortId;} set { destPortId = value;}}
        public string RequestId { get{ return requestId;} set { requestId = value;}}
        public string KeyData { get { return keyData; } set { keyData = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UiTransportMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", sourcePortId=").Append(this.sourcePortId);
            sb.Append(", destPortId=").Append(this.destPortId);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", requestId=").Append(this.requestId);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
