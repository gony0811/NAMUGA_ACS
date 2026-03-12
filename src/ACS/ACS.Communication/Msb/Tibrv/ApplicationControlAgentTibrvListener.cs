using ACS.Framework.Application;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Control;
using ACS.Utility;
using ACS.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public class ApplicationControlAgentTibrvListener : AbstractTibrvCmListener
    {
        private IWorkflowManager workflowManager;
        protected internal string xpathOfMessageName = "/MESSAGE/HEADER/MESSAGENAME";
        protected internal IApplicationControlManager applicationControlManager;
        protected internal IMessageManagerEx messageManager;

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

        public IApplicationControlManager ApplicationControlManager
        {
            get
            {
                return this.applicationControlManager;
            }
            set
            {
                this.applicationControlManager = value;
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

        public IMessageManagerEx MessageManager
        {
            get
            {
                return this.messageManager;
            }
            set
            {
                this.messageManager = value;
            }
        }

     

        /// <summary>
        /// @deprecated
        /// </summary>
        public override void onMessage(XmlDocument document, string dest)
        {
            ControlMessageEx controlMessage = this.messageManager.CreateControlMessage(document);

            this.applicationControlManager.Control(controlMessage);
            if (controlMessage.MessageName.Equals("CONTROL-HEARTBEAT"))
            {
                reply(controlMessage);
            }
            else
            {
                string messageName = XmlUtility.GetDataFromXml(document, xpathOfMessageName);
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
        }

        public void executeWorkflow(string transactionId, string messageName, XmlDocument document)
        {
            this.workflowManager.Execute(transactionId, messageName, document);
        }

        public override void onMessage(string transactionId, string messageName, XmlDocument document)
        {
            ControlMessageEx controlMessage = this.messageManager.CreateControlMessage(document);
            this.applicationControlManager.Control(controlMessage);
            if (controlMessage.MessageName.Equals("CONTROL-HEARTBEAT"))
            {
                reply(controlMessage);
            }

        }

        public override void onMessage(AbstractMessage abstractMessage)
        {
            //logger.info(abstractMessage);
        }

        public override void onAwakeMessage(string conversationId, string messageName, XmlDocument document)
        {
            //logger.info(XmlUtils.toStringPrettyFormat(document) + ", conversationId{" + conversationId + "}, messageName{" + mess ageName + "}");
        }

        public override void onAwakeMessage(AbstractMessage abstractMessage)
        {
            //logger.info(abstractMessage);
        }

        public virtual void reply(ControlMessage controlMessage)
        {
            XmlDocument document = (XmlDocument)controlMessage.ReceivedMessage;
            string message = document.InnerXml;

            try
            {
                string dest = controlMessage.OriginatedName;

                //TibrvMsg tibrvMsg = new TibrvMsg();
                TIBCO.Rendezvous.Message tibrvMsg = new TIBCO.Rendezvous.Message();

                tibrvMsg.UpdateField(this.dataFieldName, message);
                tibrvMsg.SendSubject = dest;
                Transport.TibrvTransport.Send(tibrvMsg);

                //logger.Info("message{" + message + "} was sent to " + dest);
            }
            catch (RendezvousException e)
            {
                logger.Error("failed to send message{" + message + "}", e);
            }
            catch (Exception e)
            {
                logger.Error("failed to send message{" + message + "}", e);
            }
            finally
            {

            }
        }
    }

}
