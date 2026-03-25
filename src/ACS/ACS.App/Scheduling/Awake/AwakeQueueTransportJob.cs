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
                        foreach (var bay in listBays)
                        {

                            IList queueList = _transferManager.GetQueuedTransportCommandsByBayId(((BayEx)bay).BayId);

                            if (queueList != null)
                            {
                                if (queueList.Count != 0)
                                {
                                    {
                                        AbstractMessage message = new AbstractMessage();

                                        message.MessageName = "SCHEDULE-QUEUEJOB";
                                        XmlDocument document = _messageManager.CreateDocument(message);

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
