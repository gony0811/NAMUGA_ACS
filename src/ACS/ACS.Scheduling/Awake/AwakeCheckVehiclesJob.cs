//using ACS.Framework.Scheduling.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Spring.Scheduling.Quartz;
using ACS.Framework.Message;
using ACS.Communication.Msb;
using ACS.Framework.Message.Model;
using System.Xml;
using ACS.Framework.Logging;

namespace ACS.Scheduling
{
    public class AwakeCheckVehiclesJob : QuartzJobObject//:AbstractJob
    {
        protected Logger logger = Logger.GetLogger("SCHEDULING_LOG");
        protected IMessageManagerEx messageManager;
        protected IMessageAgent messageAgent;

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

        protected override void ExecuteInternal(JobExecutionContext context)
        {
            try
            {
                //logger.info("AwakeCheckVehiclesJob will be invoked");

                this.messageManager = ((IMessageManagerEx)context.MergedJobDataMap.Get("MessageManager"));
                this.messageAgent = ((IMessageAgent)context.MergedJobDataMap.Get("MessageAgent"));

                AbstractMessage message = new AbstractMessage();

                message.MessageName = "SCHEDULE-CHECKVEHICLES";
                XmlDocument document = this.messageManager.CreateDocument(message);

                XmlElement data = document.DocumentElement["DATA"];

                XmlNode element = document.CreateNode(XmlNodeType.Element, "NAME", "");
                element.InnerText = message.MessageName;
                data.AppendChild(element);

                //logger.info(XmlUtils.toStringWithoutDeclaration(document));
                this.messageAgent.Send(document);
            }
            catch (NullReferenceException nullEx)
            {
                logger.Error(nullEx.StackTrace, nullEx);
                return;
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }
        }
    }
}
