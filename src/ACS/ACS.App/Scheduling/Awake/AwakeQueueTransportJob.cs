using System;
using System.Collections;
using System.Text.Json;
using ACS.Core.Transfer;
using ACS.Communication.Msb;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;

namespace ACS.Scheduling
{
    public class AwakeQueueTransportJob : PeriodicBackgroundService
    {
        private readonly ITransferManagerEx _transferManager;
        private readonly IMessageAgent _messageAgent;
        private readonly IResourceManagerEx _resourceManager;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(20);

        public AwakeQueueTransportJob(
            ITransferManagerEx transferManager,
            IMessageAgent messageAgent,
            IResourceManagerEx resourceManager)
        {
            _transferManager = transferManager;
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
                            string bayId = ((BayEx)bay).BayId;
                            IList queueList = _transferManager.GetQueuedTransportCommandsByBayId(bayId);

                            if (queueList != null && queueList.Count != 0)
                            {
                                var message = new DaemonScheduleMessage
                                {
                                    Header = new DaemonScheduleHeader
                                    {
                                        MessageName = "SCHEDULE-QUEUEJOB",
                                        TransactionId = Guid.NewGuid().ToString(),
                                        Timestamp = DateTime.UtcNow,
                                        Sender = "Daemon"
                                    },
                                    Data = new DaemonScheduleData
                                    {
                                        BayId = bayId
                                    }
                                };

                                string json = JsonSerializer.Serialize(message);
                                _messageAgent.Send((object)json);
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
