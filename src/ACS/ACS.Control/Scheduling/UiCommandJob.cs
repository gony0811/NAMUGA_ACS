using ACS.Communication.Msb;
using ACS.Framework.Message;
using ACS.Framework.Scheduling.Model;
using ACS.Framework.Transfer;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Socket.Model;
using ACS.Framework.Message.Model;
using System.Xml;
using ACS.Workflow;
using ACS.Communication.Socket;
using ACS.Framework.Message.Model.Control;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Transfer;

//200622 Change NIO Logic About ES.exe does not restart
namespace ACS.Control.Scheduling
{
    class UiCommandJob : AbstractJob
    {
        public static string GROUP_UICOMMAND = "GROUP-UICOMMAND";
        public static string TRIGGER_UICOMMAND = "TRIGGER_UICOMMAND";
        protected ITransferManagerExs transferManager;
        protected IMessageManagerEx messageManager;
        protected IMessageAgent messageAgent;
        protected IWorkflowManager workflowManager;

        //protected NioInterfaceManager nioInterfaceManager;

        public override void Execute(Quartz.JobExecutionContext context)
        {
            try
            {
                this.transferManager = ((ITransferManagerExs)context.MergedJobDataMap.Get("transferManager"));
                this.messageManager = ((IMessageManagerEx)context.MergedJobDataMap.Get("messageManager"));
                this.messageAgent = ((IMessageAgent)context.MergedJobDataMap.Get("messageAgent"));
                this.workflowManager = ((IWorkflowManager)context.MergedJobDataMap.Get("workflowManager"));
                //this.nioInterfaceManager = ((NioInterfaceManager)context.MergedJobDataMap.Get("nioInterfaceManager"));

                IList queueList = this.transferManager.GetEventUiCommand();

                if (queueList != null)
                {
                    if (queueList.Count != 0)
                    {
                        foreach (UiCommand uiCommandEvent in queueList)
                        {
                            AbstractMessage message = new AbstractMessage();

                            if (uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_STARTNIOCONTROLLABLE, StringComparison.OrdinalIgnoreCase)            // SOCKET-START
                             || uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_STOPNIOCONTROLLABLE, StringComparison.OrdinalIgnoreCase)            // SOCKET-STOP
                             || uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_LOADNIOCONTROLLABLE, StringComparison.OrdinalIgnoreCase)            // SOCKET-LOAD
                             || uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_UNLOADNIOCONTROLLABLE, StringComparison.OrdinalIgnoreCase)            // SOCKET-UNLOAD
                             || uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_REFRESHCACHE, StringComparison.OrdinalIgnoreCase)
                             || uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_RELOADSERVICE, StringComparison.OrdinalIgnoreCase)
                             || uiCommandEvent.MESSAGENAME.Trim().Equals(ControlMessageEx.MESSAGENAME_CONTROL_RELOADWORKFLOW, StringComparison.OrdinalIgnoreCase))           // REFRESH CACHE
                            {
                                message.MessageName = uiCommandEvent.MESSAGENAME.Trim();                     //"CONTROL_STARTNIOCONTROLLABLE";
                                XmlDocument document = this.messageManager.CreateDocument(message);

                                XmlElement data = document.DocumentElement["DATA"];

                                XmlNode idElement = document.CreateNode(XmlNodeType.Element, "ID", "");
                                idElement.InnerText = uiCommandEvent.ID;
                                data.AppendChild(idElement);

                                XmlNode messageNameElement = document.CreateNode(XmlNodeType.Element, "MESSAGENAME", "");
                                messageNameElement.InnerText = uiCommandEvent.MESSAGENAME;
                                data.AppendChild(messageNameElement);

                                XmlNode applicationNameElement = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
                                applicationNameElement.InnerText = uiCommandEvent.APPLICATIONNAME;
                                data.AppendChild(applicationNameElement);

                                XmlNode applicationTypeElement = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");
                                applicationTypeElement.InnerText = uiCommandEvent.APPLICATIONTYPE;
                                data.AppendChild(applicationTypeElement);


                                XmlNode nioNameElement = document.CreateNode(XmlNodeType.Element, "NIONAME", "");
                                nioNameElement.InnerText = uiCommandEvent.ID;
                                data.AppendChild(nioNameElement);

                                this.workflowManager.Execute(message.MessageName, document);
                            }
                            else
                            {
                                logger.Fatal("class UiCommandJob : AbstractJob - Error MessageName - " + message.MessageName);
                            }

                            //삭제
                            this.transferManager.DeleteUiCommandById(uiCommandEvent.ID, message.MessageName, uiCommandEvent.APPLICATIONNAME);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Fatal("class UiCommandJob : AbstractJob - " + e.ToString());
            }
        }
    }
}
