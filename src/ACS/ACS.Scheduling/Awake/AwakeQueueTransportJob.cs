//using ACS.Framework.Scheduling.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Spring.Scheduling.Quartz;
using ACS.Framework.Transfer;
using ACS.Framework.Message;
using ACS.Communication.Msb;
using ACS.Framework.Material;
using ACS.Framework.Application;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Logging;
using ACS.Utility;
using System.Xml;
using System.Collections;
using log4net;
namespace ACS.Scheduling
{
    public class AwakeQueueTransportJob : QuartzJobObject //: AbstractJob
    {

        protected Logger logger = Logger.GetLogger("SCHEDULING_LOG");
        protected ITransferManagerEx transferManager;
        protected IMessageManagerEx messageManager;
        protected IMessageAgent messageAgent;
        protected IMaterialManagerEx materialManager;
        protected IApplicationManager applicationManager;
        protected IResourceManagerEx resourceManager;

        public IApplicationManager ApplicationManager
        {
            get { return applicationManager; }
            set { applicationManager = value; }
        }
        public IMaterialManagerEx MaterialManager
        {
            get { return materialManager; }
            set { materialManager = value; }
        }
        public ITransferManagerEx TransferManager
        {
            get { return transferManager; }
            set { transferManager = value; }
        }
        public IMessageManagerEx MessageManager
        {
            get { return messageManager; }
            set { messageManager = value; }
        }

        public IMessageAgent MessageAgent
        {
            get { return messageAgent; }
            set { messageAgent = value; }
        }
        public IResourceManagerEx ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        protected override void ExecuteInternal(JobExecutionContext context)
        {
            try
            {
                //logger.info("AwakeQueueTransportJob will be invoked");
                //logger.Debug("==========================================================================");
                //logger.Debug("DS AwakeQueueTransportJob Start");
                this.transferManager = ((ITransferManagerEx)context.MergedJobDataMap.Get("TransferManager"));
                this.messageManager = ((IMessageManagerEx)context.MergedJobDataMap.Get("MessageManager"));
                this.messageAgent = ((IMessageAgent)context.MergedJobDataMap.Get("MessageAgent"));
                this.resourceManager = ((IResourceManagerEx)context.MergedJobDataMap.Get("ResourceManager"));

                IList listBays = this.resourceManager.GetBays();
           

                if (listBays != null)
                {
                    if (listBays.Count != 0)
                    {
                        foreach (var listbay in listBays)
                        {
                            BayEx bay = (BayEx)listbay;
                            String bayId = bay.Id;

                            IList queueList = this.transferManager.GetQueuedTransportCommandsByBayId(bayId);

                            if (queueList != null)
                            {
                                if (queueList.Count != 0)
                                {
                                    {
                                        //logger.info("Send awake Queued Job in " + bayId);

                                        AbstractMessage message = new AbstractMessage();

                                        message.MessageName = "SCHEDULE-QUEUEJOB";
                                        XmlDocument document = this.messageManager.CreateDocument(message);

                                        XmlElement data = document.DocumentElement["DATA"];


                                        XmlNode element = document.CreateNode(XmlNodeType.Element, "BAYID", "");
                                        element.InnerText = bay.Id;
                                        data.AppendChild(element);

                                        //logger.info(XmlUtils.toStringWithoutDeclaration(document));
                                        this.messageAgent.Send(document);//SCHEDULE_QUEUEJOB message
                                        // logger.Info("daemon message send : " + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }    
        }
    }
}
      
        
    
 

       
