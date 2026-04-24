using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEACQUIRECOMPLETED JSON 메시지 모델.
    /// AMR이 Source 포트에서 UNLOAD(설비 입장에서 UNLOAD, AMR 입장에서 acquire)를 완료했을 때
    /// EI가 Trans에 보고. Trans는 이 시점에 LOAD CARRIERTRANSFER를 Dest로 이어 전송한다.
    /// </summary>
    public class RailVehicleAcquireCompletedMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleAcquireCompletedHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleAcquireCompletedData Data { get; set; }
    }

    public class RailVehicleAcquireCompletedHeader
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

    public class RailVehicleAcquireCompletedData
    {
        /// <summary>TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>AMR Vehicle ID</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>작업 결과 (OK / FAIL)</summary>
        [JsonPropertyName("resultCode")]
        public string ResultCode { get; set; }

        /// <summary>오류 코드 (정상 시 빈 문자열 또는 0)</summary>
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>오류 메시지 (정상 시 빈 문자열)</summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
