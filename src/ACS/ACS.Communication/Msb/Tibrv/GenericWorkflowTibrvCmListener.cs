using ACS.Core.Message.Model;
using ACS.Utility;
using ACS.Core.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.Tibrv
{
    public class GenericWorkflowTibrvCmListener : AbstractTibrvCmListener
    {
        private IWorkflowManager workflowManager;
        protected internal string xpathOfMessageName = "/MESSAGE/HEADER/MESSAGENAME";
        protected internal string xpathOfTransactionId = "/MESSAGE/HEADER/TRANSACTIONID";
        protected internal string xpathOfConversationid = "/MESSAGE/HEADER/CONVERSATIONID";

        public IWorkflowManager WorkflowManager
        {
            get
            {
                return this.workflowManager;
            }
            set
            {
                this.workflowManager = value;
            }
        }


        public string XpathOfMessageName
        {
            get
            {
                return this.xpathOfMessageName;
            }
            set
            {
                this.xpathOfMessageName = value;
            }
        }


        public string XpathOfTransactionId
        {
            get
            {
                return this.xpathOfTransactionId;
            }
            set
            {
                this.xpathOfTransactionId = value;
            }
        }


        public string XpathOfConversationid
        {
            get
            {
                return this.xpathOfConversationid;
            }
            set
            {
                this.xpathOfConversationid = value;
            }
        }


        /// <summary>
        /// @deprecated
        /// </summary>
        public void executeWorkflow(string messageName, XmlDocument document)
        {
            this.workflowManager.Execute(messageName, document);
        }

        /// <summary>
        /// @deprecated
        /// </summary>
        public void executeWorkflow(string messageName, object @object)
        {
            this.workflowManager.Execute(messageName, @object);
        }

        public void executeWorkflow(string transactionId, string messageName, XmlDocument document)
        {
            this.workflowManager.Execute(transactionId, messageName, document);
        }

        public void executeWorkflow(string transactionId, string messageName, object @object)
        {
            this.workflowManager.Execute(transactionId, messageName, @object);
        }

        public void awakeWorkflow(object correlationId, string messageName, XmlDocument document)
        {
            //bool result = this.workflowManager.Awake(correlationId, document);
            //if (!result)
            //{
            //    //logger.error("failed to awake workflow, correlation{" + correlationId + "}, messageName{" + messageName + "}");
            //}
        }

        public void awakeWorkflow(object correlationId, string messageName, object @object)
        {
            //bool result = this.workflowManager.Awake(correlationId, @object);
            //if (!result)
            //{
            //    //logger.error("failed to awake workflow, correlation{" + correlationId + "}, messageName{" + messageName + "}");
            //}
        }

        /// <summary>
        /// @deprecated
        /// </summary>
        public override void onMessage(XmlDocument document, string dest)
        {
            //string messageName = XmlUtility.GetDataFromXml(document, this.xpathOfMessageName);
            //string transactionId = XmlUtility.GetDataFromXml(document, this.xpathOfTransactionId);

            //executeWorkflow(transactionId, messageName, document);

            string messageName = XmlUtility.GetDataFromXml(document, this.xpathOfMessageName);
            string messageType = "";
            string commandName = messageName;

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

            executeWorkflow(transactionId, commandName, document);

        }

        public override void onMessage(string transactionId, string messageName, XmlDocument document)
        {
            executeWorkflow(transactionId, messageName, document);
        }

        public override void onMessage(AbstractMessage abstractMessage)
        {
            string messageName = abstractMessage.MessageName;
            string transactionId = abstractMessage.TransactionId;

            executeWorkflow(transactionId, messageName, abstractMessage);
        }

        public override void onAwakeMessage(string conversationId, string messageName, XmlDocument document)
        {
            awakeWorkflow(conversationId, messageName, document);
        }

        public override void onAwakeMessage(AbstractMessage abstractMessage)
        {
            string messageName = abstractMessage.MessageName;
            string conversationId = abstractMessage.ConversationId;

            awakeWorkflow(conversationId, messageName, abstractMessage);
        }

    }

}
