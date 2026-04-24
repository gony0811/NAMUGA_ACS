using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEDEPOSITCOMPLETED JSON 메시지 모델.
    /// AMR이 Dest 포트에서 LOAD(설비 입장에서 LOAD, AMR 입장에서 deposit)를 완료했을 때
    /// EI가 Trans에 보고.
    /// </summary>
    public class RailVehicleDepositCompletedMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleDepositCompletedHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleDepositCompletedData Data { get; set; }
    }

    public class RailVehicleDepositCompletedHeader
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

    public class RailVehicleDepositCompletedData
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
