using System;
using System.Collections;
using System.Xml;
using ACS.Framework.Message;
using ACS.Communication.Msb;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.Message.Model;

namespace ACS.Scheduling
{
    public class AwakeChargeTransportJob : PeriodicBackgroundService
    {
        private readonly IMessageManagerEx _messageManager;
        private readonly IMessageAgent _messageAgent;
        private readonly IResourceManagerEx _resourceManager;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(20);

        public AwakeChargeTransportJob(
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

                    message.MessageName = "SCHEDULE-CHARGEJOB";
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
