using System;
using System.Text.Json;
using ACS.Communication.Msb;
using ACS.Communication.Mqtt.Model;

namespace ACS.Scheduling
{
    public class AwakeCheckVehiclesJob : PeriodicBackgroundService
    {
        private readonly IMessageAgent _messageAgent;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(10);

        public AwakeCheckVehiclesJob(
            IMessageAgent messageAgent)
        {
            _messageAgent = messageAgent;
        }

        protected override void ExecuteOnce()
        {
            try
            {
                var message = new DaemonScheduleMessage
                {
                    Header = new DaemonScheduleHeader
                    {
                        MessageName = "SCHEDULE-CHECKVEHICLES",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "Daemon"
                    }
                };

                string json = JsonSerializer.Serialize(message);
                _messageAgent.Send((object)json);
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }
        }
    }
}
