using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using ACS.Core.Workflow;
using ACS.Core.Message.Model;
using ACS.Utility;

namespace ACS.Communication.Msb.RabbitMQ
{
    public class GenericWorkflowRabbitMQListener : AbstractRabbitMQListener
    {
        private IWorkflowManager workflowManager;
        protected string xpathOfMessageName = "/MESSAGE/HEADER/MESSAGENAME";
        protected string xpathOfTransactionId = "/MESSAGE/HEADER/TRANSACTIONID";
        protected string xpathOfConversationid = "/MESSAGE/HEADER/CONVERSATIONID";

        public delegate void ExecuteDocumentWorkflowMassageHandler(string transactionId, string messageName, XmlDocument document);
        public delegate void ExecuteAbstractMessageWorkflowMassageHandler(string transactionId, string messageName, object obj);

        public ExecuteDocumentWorkflowMassageHandler ExecuteDocumentWorkflow { get; set; }
        public ExecuteAbstractMessageWorkflowMassageHandler ExecuteAbstractMessageWorkflow { get; set; }

        
        public IWorkflowManager WorkflowManager
        {
            get { return workflowManager; }
            set { workflowManager = value; }
        }

        public string XpathOfMessageName
        {
            get { return xpathOfMessageName; }
            set { xpathOfMessageName = value; }
        }

        public string XpathOfTransactionId
        {
            get { return xpathOfTransactionId; }
            set { xpathOfTransactionId = value; }
        }

        public string XpathOfConversationid
        {
            get { return xpathOfConversationid; }
            set { xpathOfConversationid = value; }
        }
        

        public void ExecuteWorkflow(string transactionId, string messageName, XmlDocument document)
        {
            this.workflowManager.Execute(transactionId, messageName, document);
        }

        public void ExecuteWorkflow(string transactionId, string messageName, object obj)
        {
            this.workflowManager.Execute(transactionId, messageName, obj);
        }

        public override void OnMessage(XmlDocument document, string dest)
        {
            string messageName = XmlUtility.GetDataFromXml(document, xpathOfMessageName);
            string messageType = "";
            string commandName = messageName;

            Console.WriteLine(messageName);

            if (string.IsNullOrEmpty(messageName))
            {
                messageName = XmlUtility.GetDataFromXml(document, "/Msg/Command");

                if (messageName.Equals("TRSJOBREQ"))
                {
                    messageType = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/CmdType");
                    commandName = messageType;
                }
                else
                {
                    commandName = messageName;
                }
            }


            if (string.IsNullOrEmpty(commandName)) commandName = messageName;

            string transactionId = XmlUtility.GetDataFromXml(document, "/Msg/TransactionID");

            ExecuteWorkflow(transactionId, commandName, document);

        }

        public override void OnMessage(AbstractMessage abstractMessage)
        {
            string messageName = abstractMessage.MessageName;
            string transactionId = abstractMessage.TransactionId;

            ExecuteWorkflow(transactionId, messageName, abstractMessage);
        }

        /// <summary>
        /// JSON 메시지를 수신하여 header.messageName으로 워크플로우 라우팅.
        /// JSON 형식: { "header": { "messageName": "...", "transactionId": "..." }, "data": { ... } }
        /// </summary>
        public override void OnJsonMessage(string jsonMessage, string dest)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonMessage);
                var root = doc.RootElement;

                string messageName = "";
                string transactionId = "";

                if (root.TryGetProperty("header", out var header))
                {
                    if (header.TryGetProperty("messageName", out var mn))
                        messageName = mn.GetString() ?? "";
                    if (header.TryGetProperty("transactionId", out var tid))
                        transactionId = tid.GetString() ?? "";
                }

                if (string.IsNullOrEmpty(messageName))
                {
                    logger.Error("JSON message has no header.messageName: " + jsonMessage);
                    return;
                }

                // JSON 문자열을 object로 워크플로우에 전달
                this.workflowManager.Execute(transactionId, messageName, (object)jsonMessage);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process JSON message: " + ex.Message, ex);
            }
        }
    }
}
