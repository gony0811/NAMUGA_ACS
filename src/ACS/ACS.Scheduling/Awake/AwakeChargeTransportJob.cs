using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Spring.Scheduling.Quartz;
using Spring.Core;
using ACS.Framework.Message;
using ACS.Communication.Msb;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.Message.Model;
using System.Xml;
using System.Collections;

namespace ACS.Scheduling
{
    public class AwakeChargeTransportJob : QuartzJobObject //: AbstractJob
    {
        //protected static final Logger logger = Logger.getLogger(AwakeChargeTransportJob.class);
        protected IMessageManagerEx messageManager;
        protected IMessageAgent messageAgent;
        protected IResourceManagerEx resourceManager;

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
            //logger.info("AwakeChargeTransportJob will be invoked");
            
            this.messageManager = ((IMessageManagerEx)context.MergedJobDataMap.Get("MessageManager"));
            this.messageAgent = ((IMessageAgent)context.MergedJobDataMap.Get("MessageAgent"));
            this.resourceManager = ((IResourceManagerEx)context.MergedJobDataMap.Get("ResourceManager"));
            
            IList listBays = this.resourceManager.GetBays();
            if (listBays != null)
            {
                foreach (var listbay in listBays)
                {
                    BayEx bay = (BayEx)listbay;

                    AbstractMessage message = new AbstractMessage();

                    message.MessageName = "SCHEDULE-CHARGEJOB";
                    XmlDocument document = this.messageManager.CreateDocument(message);

                    XmlElement data = document.DocumentElement["DATA"];

                    XmlNode element = document.CreateNode(XmlNodeType.Element, "BAYID", "");
                    element.InnerText = bay.Id;
                    data.AppendChild(element);

                    //logger.info(XmlUtils.toStringWithoutDeclaration(document));
                    this.messageAgent.Send(document);
                }
            }
        }
    }
}
