using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEJOBFAILED JSON 메시지 모델.
    /// AMR reply(status=FAILED) 중 사양 확정분 — EXCHANGE origin 픽업 실패
    /// (MAGAZINE_NOT_FOUND 즉시 종결, "취소·오류" 시트 §2) — 을 Trans 로 라우팅한다.
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
    }
}
