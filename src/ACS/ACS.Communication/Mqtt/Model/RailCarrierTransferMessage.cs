using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// Trans → EI 프로세스로 전송되는 RAIL-CARRIERTRANSFER JSON 메시지 모델.
    /// Vehicle에 반송 명령(Source/Dest)을 전달한다.
    /// </summary>
    public class RailCarrierTransferMessage
    {
        [JsonPropertyName("header")]
        public RailCarrierTransferHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailCarrierTransferData Data { get; set; }
    }

    public class RailCarrierTransferHeader
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

    public class RailCarrierTransferData
    {
        /// <summary>TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>할당된 Vehicle ID</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>목적지 포트 ID (eqpId:portId)</summary>
        [JsonPropertyName("destPortId")]
        public string DestPortId { get; set; }

        /// <summary>목적지 노드 ID (Station ID)</summary>
        [JsonPropertyName("destNodeId")]
        public string DestNodeId { get; set; }

        /// <summary>우선순위</summary>
        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        /// <summary>캐리어 타입</summary>
        [JsonPropertyName("carrierType")]
        public string CarrierType { get; set; }

        /// <summary>포트 위치 (LEFT / RIGHT)</summary>
        [JsonPropertyName("port")]
        public string Port { get; set; }

        /// <summary>작업 유형 (LOAD / UNLOAD / EXCHANGE)</summary>
        [JsonPropertyName("jobType")]
        public string JobType { get; set; }

        /// <summary>결과 코드 (초기 전송 시 빈 문자열)</summary>
        [JsonPropertyName("resultCode")]
        public string ResultCode { get; set; }
    }
}
