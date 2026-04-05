using System;
using System.Text.Json;
using ACS.Communication.Msb;
using ACS.Communication.Mqtt.Model;

namespace ACS.Scheduling
{
    public class AwakeCheckServerTimeJob : PeriodicBackgroundService
    {
        private readonly IMessageAgent _messageAgent;

        protected override TimeSpan Interval => TimeSpan.FromMinutes(5);

        public AwakeCheckServerTimeJob(
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
                        MessageName = "SCHEDULE-CHECKSERVERTIME",
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
