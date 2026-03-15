using System;
using System.Collections;
using System.Xml;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Communication.Msb;
using ACS.Core.Message.Model;

namespace ACS.Scheduling
{
    public class AwakeCallVehicleStopWaitJob : PeriodicBackgroundService
    {
        private readonly IMessageManagerEx _messageManager;
        private readonly IMessageAgent _messageAgent;
        private readonly IResourceManagerEx _resourceManager;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(5);

        public AwakeCallVehicleStopWaitJob(
            IMessageManagerEx messageManager,
            IMessageAgent messageAgent,
            IResourceManagerEx resourceManager)
        {
            _messageManager = messageManager;
            _messageAgent = messageAgent;
            _resourceManager = resourceManager;
        }

        protected override void ExecuteOnce()
        {
            IList listBays = _resourceManager.GetBays();

            if (listBays != null)
            {
                foreach (var listbay in listBays)
                {
                    BayEx bay = (BayEx)listbay;
                    AbstractMessage message = new AbstractMessage();
                    message.MessageName = "SCHEDULE-CALLIDLEVEHICLE";

                    XmlDocument document = _messageManager.CreateDocument(message);
                    XmlElement data = document.DocumentElement["DATA"];

                    XmlNode element = document.CreateNode(XmlNodeType.Element, "BAYID", "");
                    element.InnerText = bay.Id;
                    data.AppendChild(element);

                    _messageAgent.Send(document);
                }
            }
        }
    }
}
