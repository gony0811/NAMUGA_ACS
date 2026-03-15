using System;
using System.Collections;
using System.Xml;
using ACS.Core.Transfer;
using ACS.Core.Message;
using ACS.Communication.Msb;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Message.Model;

namespace ACS.Scheduling
{
    public class AwakeQueueTransportJob : PeriodicBackgroundService
    {
        private readonly ITransferManagerEx _transferManager;
        private readonly IMessageManagerEx _messageManager;
        private readonly IMessageAgent _messageAgent;
        private readonly IResourceManagerEx _resourceManager;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(20);

        public AwakeQueueTransportJob(
            ITransferManagerEx transferManager,
            IMessageManagerEx messageManager,
            IMessageAgent messageAgent,
            IResourceManagerEx resourceManager)
        {
            _transferManager = transferManager;
            _messageManager = messageManager;
            _messageAgent = messageAgent;
            _resourceManager = resourceManager;
        }

        protected override void ExecuteOnce()
        {
            try
            {
                IList listBays = _resourceManager.GetBays();

                if (listBays != null)
                {
                    if (listBays.Count != 0)
                    {
                        foreach (var listbay in listBays)
                        {
                            BayEx bay = (BayEx)listbay;
                            String bayId = bay.Id;

                            IList queueList = _transferManager.GetQueuedTransportCommandsByBayId(bayId);

                            if (queueList != null)
                            {
                                if (queueList.Count != 0)
                                {
                                    {
                                        AbstractMessage message = new AbstractMessage();

                                        message.MessageName = "SCHEDULE-QUEUEJOB";
                                        XmlDocument document = _messageManager.CreateDocument(message);

                                        XmlElement data = document.DocumentElement["DATA"];


                                        XmlNode element = document.CreateNode(XmlNodeType.Element, "BAYID", "");
                                        element.InnerText = bay.Id;
                                        data.AppendChild(element);

                                        _messageAgent.Send(document);//SCHEDULE_QUEUEJOB message
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
