using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ACS.Core.Workflow;
using ACS.Core.Message.Model;
using ACS.Utility;


namespace ACS.Communication.Msb.Highway101
{
    public class GenericWorkflowHighway101Listener : AbstractHighway101Listener
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

            if(string.IsNullOrEmpty(messageName))
            {
                messageName = XmlUtility.GetDataFromXml(document, "/Msg/Command");

                if(messageName.Equals("TRSJOBREQ"))
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
    }
}
