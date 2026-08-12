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
    /// <summary>
    /// EXCHANGE(v2) S4: EXCHANGE_QUEUED TC 가 있는 Bay 에 SCHEDULE-EXCHANGEJOB 메시지를 발행.
    /// AwakeQueueTransportJob 미러 — 기존 QUEUED 배차와 병렬 경로 (D4/D5).
    /// </summary>
    public class AwakeExchangeTransportJob : PeriodicBackgroundService
    {
        private readonly ITransferManagerEx _transferManager;
        private readonly IMessageAgent _messageAgent;
        private readonly IResourceManagerEx _resourceManager;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(10);

        public AwakeExchangeTransportJob(
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
                            IList queueList = _transferManager.GetExchangeQueuedTransportCommandsByBayId(bayId);

                            if (queueList != null && queueList.Count != 0)
                            {
                                var message = new DaemonScheduleMessage
                                {
                                    Header = new DaemonScheduleHeader
                                    {
                                        MessageName = "SCHEDULE-EXCHANGEJOB",
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
