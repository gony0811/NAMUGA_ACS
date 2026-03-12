using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using ACS.Framework.Scheduling.Model;
using ACS.Workflow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Control.Scheduling
{
    class UiApplicationManagerJob : AbstractJob
    {
        public static string GROUP_UIAPPLICATIONMANAGER = "GROUP-UIAPPLICATIONMANAGER";
        public static string TRIGGER_UIAPPLICATIONMANAGER = "TRIGGER_UIAPPLICATIONMANAGER";
        protected IApplicationManager applicationManager;
        //protected ITransferManagerEx transferManager;
        protected IMessageManagerEx messageManager;
        //protected IMessageAgent messageAgent;
        protected IWorkflowManager workflowManager;

        public override void Execute(Quartz.JobExecutionContext context)
        {
            this.applicationManager = ((IApplicationManager)context.MergedJobDataMap.Get("applicationManager"));
            this.messageManager = ((IMessageManagerEx)context.MergedJobDataMap.Get("messageManager"));
            this.workflowManager = ((IWorkflowManager)context.MergedJobDataMap.Get("workflowManager"));

            IList queueList = this.applicationManager.GetQueuedUiApplicationManagers();

            if (queueList != null)
            {
                if (queueList.Count != 0)
                {
                    foreach (UiApplicationManager uiApplicationMgr in queueList)
                    {
                        AbstractMessage message = new AbstractMessage();
                        if (uiApplicationMgr.COMMAND == "CONTROL-START")
                        {
                            message.MessageName = "CONTROL-START";
                            XmlDocument document = this.messageManager.CreateDocument(message);
                            
                            XmlElement data = document.DocumentElement["DATA"];

                            XmlNode applicationElenemt = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
                            applicationElenemt.InnerText = uiApplicationMgr.ID;
                            data.AppendChild(applicationElenemt);

                            XmlNode applicationTypeElement = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");
                            applicationTypeElement.InnerText = uiApplicationMgr.TYPE;
                            data.AppendChild(applicationTypeElement);

                            this.applicationManager.UpdateApplicationManagersState(uiApplicationMgr.ID, "PROCESSING");
                            this.workflowManager.Execute(uiApplicationMgr.COMMAND, document);
                           
                        }
                        else if (uiApplicationMgr.COMMAND == "CONTROL-STOP")
                        {
                            message.MessageName = "CONTROL-STOP";
                            XmlDocument document = this.messageManager.CreateDocument(message);

                            XmlElement data = document.DocumentElement["DATA"];

                            XmlNode applicationElenemt = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
                            applicationElenemt.InnerText = uiApplicationMgr.ID;
                            data.AppendChild(applicationElenemt);

                            XmlNode applicationTypeElement = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");
                            applicationTypeElement.InnerText = uiApplicationMgr.TYPE;
                            data.AppendChild(applicationTypeElement);

                            this.applicationManager.UpdateApplicationManagersState(uiApplicationMgr.ID, "PROCESSING");
                            this.workflowManager.Execute(uiApplicationMgr.COMMAND, document);
                            
                        }
                        else if (uiApplicationMgr.COMMAND == "CONTROL-KILL")
                        {
                            message.MessageName = "CONTROL-KILL";
                            XmlDocument document = this.messageManager.CreateDocument(message);

                            XmlElement data = document.DocumentElement["DATA"];

                            XmlNode applicationElenemt = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
                            applicationElenemt.InnerText = uiApplicationMgr.ID;
                            data.AppendChild(applicationElenemt);

                            XmlNode applicationTypeElement = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");
                            applicationTypeElement.InnerText = uiApplicationMgr.TYPE;
                            data.AppendChild(applicationTypeElement);

                            this.applicationManager.UpdateApplicationManagersState(uiApplicationMgr.ID, "PROCESSING");
                            this.workflowManager.Execute(uiApplicationMgr.COMMAND, document);
                            
                        }
                        //else if (uiApplicationMgr.COMMAND == "CONTROL-RELOAD")
                        //{
                        //    message.MessageName = "CONTROL-RELOAD";
                        //    XmlDocument document = this.messageManager.CreateDocument(message);

                        //    XmlElement data = document.DocumentElement["DATA"];

                        //    XmlNode applicationElenemt = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
                        //    applicationElenemt.InnerText = uiApplicationMgr.ID;
                        //    data.AppendChild(applicationElenemt);

                        //    XmlNode applicationTypeElement = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");
                        //    applicationTypeElement.InnerText = uiApplicationMgr.TYPE;
                        //    data.AppendChild(applicationTypeElement);

                        //    this.applicationManager.UpdateApplicationManagersState(uiApplicationMgr.ID, "PROCESSING");
                        //    this.WorkflowManager.Execute(uiApplicationMgr.COMMAND, document);                          
                        //}
                        else
                        {
                            this.applicationManager.UpdateApplicationManagersReply(uiApplicationMgr.ID, "NAK", "FAIL");                           
                        }
                    }
                }
            }
        }
    }
}
