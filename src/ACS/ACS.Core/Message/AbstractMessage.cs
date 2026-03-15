using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ACS.Core.Base;
using ACS.Core.Resource.Model;
using ACS.Core.Resource.Model.Factory.Machine;

namespace ACS.Core.Message.Model
{
    [Serializable]
    public class AbstractMessage : ICloneable
    {
        private static long serialVersionUID = -478316570044465269L;
        public static string NAME_MESSAGE = "MESSAGE";
        public static string NAME_HEADER = "HEADER";
        public static string NAME_MESSAGENAME = "MESSAGENAME";
        public static string NAME_TRANSACTIONID = "TRANSACTIONID";
        public static string NAME_CONVERSATIONID = "CONVERSATIONID";
        public static string NAME_TIME = "TIME";
        public static string XPATH_NAME_HEADER = "/MESSAGE/HEADER";
        public static string XPATH_NAME_MESSAGENAME = "/MESSAGE/HEADER/MESSAGENAME";
        public static string XPATH_NAME_TRANSACTIONID = "/MESSAGE/HEADER/TRANSACTIONID";
        public static string XPATH_NAME_CONVERSATIONID = "/MESSAGE/HEADER/CONVERSATIONID";
        public static string XPATH_NAME_TIME = "/MESSAGE/HEADER/TIME";
        public static string NAME_ORIGINATED = "ORIGINATED";
        public static string NAME_ORIGINATEDTYPE = "ORIGINATEDTYPE";
        public static string NAME_ORIGINATEDNAME = "ORIGINATEDNAME";
        public static string NAME_MACHINENAME = "MACHINENAME";
        public static string NAME_CONNECTIONID = "CONNECTIONID";
        public static string NAME_USERNAME = "USERNAME";
        public static string XPATH_NAME_ORIGINATED = "/MESSAGE/ORIGINATED";
        public static string XPATH_NAME_ORIGINATEDTYPE = "/MESSAGE/ORIGINATED/ORIGINATEDTYPE";
        public static string XPATH_NAME_ORIGINATEDNAME = "/MESSAGE/ORIGINATED/ORIGINATEDNAME";
        public static string XPATH_NAME_MACHINENAME = "/MESSAGE/ORIGINATED/MACHINENAME";
        public static string XPATH_NAME_CONNECTIONID = "/MESSAGE/ORIGINATED/CONNECTIONID";
        public static string XPATH_NAME_USERNAME = "/MESSAGE/ORIGINATED/USERNAME";
        public static string NAME_DATA = "DATA";
        public static string NAME_TAIL = "TAIL";
        public static string NAME_SECSII = "SECSII";
        public static string XPATH_NAME_DATA = "/MESSAGE/DATA";
        public static string XPATH_NAME_TAIL = "/MESSAGE/TAIL";
        public static string XPATH_NAME_SECSII = "/MESSAGE/TAIL/SECSII";
        public static string NAME_RESULTMESSAGE = "RESULTMESSAGE";
        public static string XPATH_NAME_RESULTMESSAGE = "/MESSAGE/DATA/RESULTMESSAGE";

        private string messageName = "";
        private string transactionId = "";
        private string conversationId = "";
        private string time;
        private string currentMachineName = "";
        private string currentUnitName = "";
        private string originatedType = "N";
        private string originatedName = "";
        private string connectionId = "";
        private string userName = "";
        private string originatedMachineName = "";
        private Machine currentMachine;

        private XmlDocument document;
        private Object sendingMessage;
        private XmlDocument sendingMessageDocument;
        private Object receivedMessage;
        private string receivedMessageName = "";
        private string cause = "";
        private Dictionary<string, string> additionalInfoMap = new Dictionary<string, string>();

        public string MessageName
        {
            get {return messageName;}
            set {messageName = value;}
        }
        public string TransactionId 
        {
            get {return transactionId;}
            set {transactionId = value;}
        }
        public string ConversationId
        {
            get {return conversationId;}
            set {conversationId = value;}
        }

        public Object SendingMessage
        {
            get { return sendingMessage; }
            set { sendingMessage = value; }
        }
        public string Time
        {
            get {return time;}
            set {time = value;}
        }
        public string CurrentMachineName
        {
            get {return currentMachineName;}
            set {currentMachineName = value;}
        }

        public string CurrentUnitName
        {
            get { return currentUnitName; }
            set { currentUnitName = value; }
        }
        public string OriginatedType
        {
            get {return originatedType;}
            set {originatedType = value;}
        }
        public string OriginatedName
        {
            get {return originatedName;}
            set {originatedName = value;}
        }
        public string ConnectionId
        {
            get {return connectionId;}
            set {connectionId = value;}
        }
        public string UserName 
        {
            get {return userName;}
            set {userName = value;}
        }
        public string OriginatedMachineName
        {
            get {return originatedMachineName;}
            set {originatedMachineName = value;}
        }
        public Machine CurrentMachine
        {
            get { return currentMachine; }
            set { currentMachine = value; }
        }
      
        public XmlDocument Document
        {
            get
            {
                return document;
            }

            set
            {
                document = value;
            }
        }

        protected XmlDocument SendingMessageDocument
        {
            get {return sendingMessageDocument;}
            set {sendingMessageDocument = value;}
        }
        public object ReceivedMessage
        {
            get {return receivedMessage;}
            set {receivedMessage = value;}
        }
        public string ReceivedMessageName
        {
            get {return receivedMessageName;}
            set {receivedMessageName = value;}
        }
        public string Cause
        {
            get {return cause;}
            set {cause = value;}
        }
        public Dictionary<string, string> AdditionalInfoMap
        {
            get { return additionalInfoMap; }
            set { additionalInfoMap = value; }
        }

        public string ReceivedMessageToString()
        {
            if (this.receivedMessage != null)
                return this.receivedMessage.ToString();
            else
                return string.Empty;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("abstractMessage{");
            sb.Append("messageName=").Append(this.messageName);
            sb.Append(", receivedMessageName=").Append(this.receivedMessageName);
            sb.Append(", conversationId=").Append(this.conversationId);
            sb.Append(", transactionId=").Append(this.transactionId);
            sb.Append(", currentMachineName=").Append(this.currentMachineName);
            sb.Append(", originatedMachineName=").Append(this.originatedMachineName);
            sb.Append(", originatedType=").Append(this.originatedType);
            sb.Append(", originatedName=").Append(this.originatedName);
            sb.Append(", connectionId=").Append(this.connectionId);
            sb.Append(", userName=").Append(this.userName);
            sb.Append(", cause=").Append(this.cause);
            sb.Append(", time=").Append(this.time);
            sb.Append(", additionalInfoMap=").Append(this.additionalInfoMap);
            sb.Append("}");
            return sb.ToString();
        }

        public object Clone()
        {
            try
            {
                return base.MemberwiseClone();
            }
            catch
            {
                throw new NotImplementedException();
            }         
        }
    }
}
