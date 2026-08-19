using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEJOBFAILED JSON 메시지 모델.
    /// AMR reply(status=FAILED / REJECTED) 를 EXCHANGE TC 에 한해 Trans 로 라우팅한다.
    /// 처리 정책(AmrReplyPolicy)은 Trans 측 RailVehicleJobfailedWorkflow 가 결정한다:
    ///  FAILED@STEP10 → MAGAZINE_NOT_FOUND 종결, REJECTED@STEP10 → EXCHANGE_QUEUED 롤백, 그 외 로그.
    /// (RailVehicleAcquireCompletedMessage 미러)
    /// </summary>
    public class RailVehicleJobFailedMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleJobFailedHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleJobFailedData Data { get; set; }
    }

    public class RailVehicleJobFailedHeader
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

    public class RailVehicleJobFailedData
    {
        /// <summary>실패한 TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>Vehicle ID (DB VehicleId)</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>AMR reply resultCode</summary>
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>AMR reply message</summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>AMR reply status (FAILED / REJECTED)</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>AMR reply resultCode (정수 원값)</summary>
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        /// <summary>AMR reply step (선택)</summary>
        [JsonPropertyName("step")]
        public int? Step { get; set; }
    }
}
