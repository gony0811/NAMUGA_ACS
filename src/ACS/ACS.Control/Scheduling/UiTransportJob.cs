using ACS.Communication.Msb;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using ACS.Framework.Scheduling.Model;
using ACS.Framework.Transfer;
using ACS.Framework.Transfer.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Control.Scheduling
{
    class UiTransportJob : AbstractJob
    {
        public static string GROUP_UITRANSPORT = "GROUP-UITRNASPORT";
        public static string TRIGGER_UITRNASPROT = "TRIGGER_UITRNASPROT";
        protected ITransferManagerEx transferManager;
        protected IMessageManagerEx messageManager;
        protected IMessageAgent messageAgent;

        public override void Execute(Quartz.JobExecutionContext context)
        {
            this.transferManager = ((ITransferManagerEx)context.MergedJobDataMap.Get("transferManager"));
            this.messageManager = ((IMessageManagerEx)context.MergedJobDataMap.Get("messageManager"));
            this.messageAgent = ((IMessageAgent)context.MergedJobDataMap.Get("messageAgent"));
            IList queueList = this.transferManager.GetQueuedUiTransportCommands();
            if (queueList != null)
            {
                if (queueList.Count != 0)
                {
                    foreach (UiTransport uiTransportCmd in queueList)
                    {
                        AbstractMessage message = new AbstractMessage();
                        if (uiTransportCmd.MESSAGENAME == "UI_TRANSPORT")
                        {
                            message.MessageName = "UI-TRANSPORT";
                            XmlDocument document = this.messageManager.CreateDocument(message);                          

                            XmlElement data = document.DocumentElement["DATA"];

                            XmlNode sourceElenemt = document.CreateNode(XmlNodeType.Element, "SOURCEPORTID", "");
                            sourceElenemt.InnerText = uiTransportCmd.SOURCEPORTID;
                            data.AppendChild(sourceElenemt);

                            XmlNode destportidElement = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
                            destportidElement.InnerText = uiTransportCmd.DESTPORTID;
                            data.AppendChild(destportidElement);

                            XmlNode requestElement = document.CreateNode(XmlNodeType.Element, "REQUESTID", "");
                            requestElement.InnerText = uiTransportCmd.REQUESTID;
                            data.AppendChild(requestElement);

                            XmlNode useridElement = document.CreateNode(XmlNodeType.Element, "USERID", "");
                            useridElement.InnerText = uiTransportCmd.USERID;
                            data.AppendChild(useridElement);
                            this.messageAgent.Send(document);
                        }
                        else if(uiTransportCmd.MESSAGENAME == "UI_MOVEVEHICLE")
                        {
                            message.MessageName = "UI-MOVEVEHICLE";
                            XmlDocument document = this.messageManager.CreateDocument(message);

                            XmlElement data = document.DocumentElement["DATA"];

                            XmlNode vehicleElenemt = document.CreateNode(XmlNodeType.Element, "VEHICLEID", "");
                            vehicleElenemt.InnerText = uiTransportCmd.VEHICLEID;
                            data.AppendChild(vehicleElenemt);

                            XmlNode destnodeidElement = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
                            destnodeidElement.InnerText = uiTransportCmd.DESTNODEID;
                            data.AppendChild(destnodeidElement);

                            XmlNode requestElement = document.CreateNode(XmlNodeType.Element, "REQUESTID", "");
                            requestElement.InnerText = uiTransportCmd.REQUESTID;
                            data.AppendChild(requestElement);

                            XmlNode useridElement = document.CreateNode(XmlNodeType.Element, "USERID", "");
                            useridElement.InnerText = uiTransportCmd.USERID;
                            data.AppendChild(useridElement);

                            this.messageAgent.Send(document);
                        }
                        else if(uiTransportCmd.MESSAGENAME == "UI_TRANSPORT_DELETE")
                        {                    
                            message.MessageName = "UI-TRANSPORT_DELETE";
                            XmlDocument document = this.messageManager.CreateDocument(message);

                            XmlElement data = document.DocumentElement["DATA"];

                            XmlNode transcmdElenemt = document.CreateNode(XmlNodeType.Element, "TRANSPORTCOMMANDID", "");
                            transcmdElenemt.InnerText = uiTransportCmd.TRANSPORTCOMMANDID;
                            data.AppendChild(transcmdElenemt);                            

                            XmlNode requestElement = document.CreateNode(XmlNodeType.Element, "REQUESTID", "");
                            requestElement.InnerText = uiTransportCmd.REQUESTID;
                            data.AppendChild(requestElement);

                            XmlNode useridElement = document.CreateNode(XmlNodeType.Element, "USERID", "");
                            useridElement.InnerText = uiTransportCmd.USERID;
                            data.AppendChild(useridElement);
                            this.messageAgent.Send(document);
                        }
                        else
                        {
                            //fail log
                        }

                        //한버삭제기능
                        this.transferManager.DeleteUiTransportById(uiTransportCmd.ID);
                    }
                }                
            }
        }
    }
}