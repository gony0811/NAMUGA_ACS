using System;
using System.Collections;
using System.Text.Json;
using ACS.Communication.Msb;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;

namespace ACS.Scheduling
{
    public class AwakeCallVehicleStopWaitJob : PeriodicBackgroundService
    {
        private readonly IMessageAgent _messageAgent;
        private readonly IResourceManagerEx _resourceManager;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(5);

        public AwakeCallVehicleStopWaitJob(
            IMessageAgent messageAgent,
            IResourceManagerEx resourceManager)
        {
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

                    var message = new DaemonScheduleMessage
                    {
                        Header = new DaemonScheduleHeader
                        {
                            MessageName = "SCHEDULE-CALLIDLEVEHICLE",
                            TransactionId = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow,
                            Sender = "Daemon"
                        },
                        Data = new DaemonScheduleData
                        {
                            BayId = bay.BayId
                        }
                    };

                    string json = JsonSerializer.Serialize(message);
                    _messageAgent.Send((object)json);
                }
            }
        }
    }
}
