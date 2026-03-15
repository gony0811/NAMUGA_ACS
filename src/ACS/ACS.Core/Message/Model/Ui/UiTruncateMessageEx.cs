using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message.Model;

namespace ACS.Core.Message.Model.Ui
{
    public class UiTruncateMessageEx : AbstractMessage
    {
        private string tableName = "";
        private string partitionId = "";
        public static string MESSAGE_NAME = "UI-TRUNCATE";
        public static string NAME_TABLENAME = "TABLENAME";
        public static string NAME_PARTITIONID = "PARTITIONID";
        public static string XPATH_NAME_TABLENAME = "/MESSAGE/DATA/TABLENAME";
        public static string XPATH_NAME_PARTITIONID = "/MESSAGE/DATA/PARTITIONID";

        public string TableName { get{ return tableName;} set { tableName = value;}}
        public string PartitionId { get { return partitionId; } set { partitionId = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("uiTruncateMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", receivedMessageName=").Append(this.ReceivedMessageName);
            sb.Append(", conversationId=").Append(this.ConversationId);
            sb.Append(", transactionId=").Append(this.TransactionId);
            sb.Append(", tableName=").Append(this.tableName);
            sb.Append(", partitionId=").Append(this.partitionId);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", cause=").Append(this.Cause);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
