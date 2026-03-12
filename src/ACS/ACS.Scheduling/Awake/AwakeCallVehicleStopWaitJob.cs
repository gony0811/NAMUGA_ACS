//using ACS.Framework.Scheduling.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Communication.Msb;
using ACS.Framework.Logging;
using ACS.Framework.Message.Model;
using ACS.Utility;
using System.Xml;
using Quartz;
using Spring.Scheduling.Quartz;
using System.Collections;

namespace ACS.Scheduling
{
    public class AwakeCallVehicleStopWaitJob : QuartzJobObject //: AbstractJob
    {
        protected Logger logger = Logger.GetLogger("SCHEDULING_LOG");
        private IMessageManagerEx messageManager;
        private IMessageAgent messageAgent;
        private IResourceManagerEx resourceManager;

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
            // logger.info("AwakeCallVehicleStopWaitJob will be invoked");
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
                    message.MessageName = "SCHEDULE-CALLIDLEVEHICLE";

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
