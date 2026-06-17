using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// Host → Trans 프로세스로 전송되는 ACTIONCMD JSON 메시지 모델.
    /// MES에서 수신한 ACTIONCMD XML을 JSON으로 변환하여 RabbitMQ로 전달.
    /// </summary>
    public class ActionCmdMessage
    {
        [JsonPropertyName("header")]
        public ActionCmdHeader Header { get; set; }

        [JsonPropertyName("data")]
        public ActionCmdData Data { get; set; }
    }

    public class ActionCmdHeader
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

    public class ActionCmdData
    {
        [JsonPropertyName("acsId")]
        public string AcsId { get; set; }

        [JsonPropertyName("targetLoc")]
        public string TargetLoc { get; set; }

        [JsonPropertyName("targetPort")]
        public string TargetPort { get; set; }

        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        [JsonPropertyName("materialType")]
        public string MaterialType { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("actionType")]
        public string ActionType { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }
    }
}
