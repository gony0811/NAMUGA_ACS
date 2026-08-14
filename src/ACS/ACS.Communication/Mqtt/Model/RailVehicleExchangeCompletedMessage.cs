using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEEXCHANGECOMPLETED JSON 메시지 모델 (EXCHANGE v2 S5).
    /// AMR이 설비(mid)에서 교체 작업(구자재 회수 UNLOAD_OLD + 신자재 투입 LOAD_NEW)을
    /// 완료했을 때 EI가 Trans에 보고. Trans는 이 시점에 반납(dest)행 LOAD CARRIERTRANSFER를 이어 전송한다.
    /// RailVehicleAcquireCompletedMessage 미러.
    /// </summary>
    public class RailVehicleExchangeCompletedMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleExchangeCompletedHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleExchangeCompletedData Data { get; set; }
    }

    public class RailVehicleExchangeCompletedHeader
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

    public class RailVehicleExchangeCompletedData
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
