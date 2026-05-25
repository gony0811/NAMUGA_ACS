using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using ACS.Core.Workflow;
using ACS.Core.Message.Model;
using ACS.Core.Logging;
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
            // 처리 흐름 전체(워크플로우 활동·서비스 포함)에 로그 컨텍스트를 깔아 DB 로그 컬럼을 자동 보강.
            using (LogContext.Push(BuildLogContext(transactionId, messageName, null)))
            {
                this.workflowManager.Execute(transactionId, messageName, document);
            }
        }

        public void ExecuteWorkflow(string transactionId, string messageName, object obj)
        {
            using (LogContext.Push(BuildLogContext(transactionId, messageName, obj)))
            {
                this.workflowManager.Execute(transactionId, messageName, obj);
            }
        }

        /// <summary>
        /// 메시지에서 가능한 로그 컨텍스트(transactionId/messageName + 가능하면 carrier/command/machine/unit)를 수집한다.
        /// </summary>
        private LogContextData BuildLogContext(string transactionId, string messageName, object obj)
        {
            var data = new LogContextData
            {
                TransactionId = transactionId,
                MessageName = messageName,
                CommunicationMessageName = messageName
            };

            if (obj is AbstractMessage am)
            {
                if (string.IsNullOrEmpty(data.MessageName)) data.MessageName = am.MessageName;
                if (string.IsNullOrEmpty(data.TransactionId)) data.TransactionId = am.TransactionId;
                data.MachineName = am.CurrentMachineName;
                data.UnitName = am.CurrentUnitName;
            }

            if (obj is BaseMessage bm)
            {
                data.CarrierName = bm.CarrierName;
                data.TransportCommandId = bm.TransportCommandId;
            }
            else if (obj is TransferMessageEx tm)
            {
                data.CarrierName = tm.CarrierName;
                data.TransportCommandId = tm.TransportCommandId;
            }

            return data;
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
                using (LogContext.Push(BuildLogContext(transactionId, messageName, null)))
                {
                    this.workflowManager.Execute(transactionId, messageName, (object)jsonMessage);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process JSON message: " + ex.Message, ex);
            }
        }
    }
}
