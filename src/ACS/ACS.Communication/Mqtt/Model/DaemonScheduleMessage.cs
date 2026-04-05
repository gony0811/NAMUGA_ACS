using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// Daemon → Trans 프로세스로 전송되는 공통 스케줄 JSON 메시지 모델.
    /// SCHEDULE-QUEUEJOB, SCHEDULE-CHARGEJOB, SCHEDULE-CALLIDLEVEHICLE,
    /// SCHEDULE-CHECKVEHICLES, SCHEDULE-CHECKSERVERTIME, SCHEDULE-CHECKCROSSNODE 공용.
    /// </summary>
    public class DaemonScheduleMessage
    {
        [JsonPropertyName("header")]
        public DaemonScheduleHeader Header { get; set; }

        [JsonPropertyName("data")]
        public DaemonScheduleData Data { get; set; }
    }

    public class DaemonScheduleHeader
    {
        [JsonPropertyName("messageName")]
        public string MessageName { get; set; }

        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("sender")]
        public string Sender { get; set; }
    }

    public class DaemonScheduleData
    {
        [JsonPropertyName("bayId")]
        public string BayId { get; set; }
    }
}
