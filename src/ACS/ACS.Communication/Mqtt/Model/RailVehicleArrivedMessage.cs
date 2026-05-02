using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEARRIVED JSON 메시지 모델.
    /// AMR이 moveCmd 목적지 노드에 도달했을 때(LOAD/UNLOAD 동작 시작 직전) EI가 Trans에 보고.
    /// reply 라이프사이클: ACCEPTED → EXECUTING → ARRIVED → COMPLETED.
    /// </summary>
    public class RailVehicleArrivedMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleArrivedHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleArrivedData Data { get; set; }
    }

    public class RailVehicleArrivedHeader
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

    public class RailVehicleArrivedData
    {
        /// <summary>TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }
        
        [JsonPropertyName("JobType")]
        public string JobType { get; set; }

        /// <summary>AMR Vehicle ID</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>도착한 노드 ID</summary>
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }

        /// <summary>작업 결과 (OK / FAIL)</summary>
        [JsonPropertyName("resultCode")]
        public string ResultCode { get; set; }

        /// <summary>오류 코드 (정상 시 빈 문자열)</summary>
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>오류 메시지 (정상 시 빈 문자열)</summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
