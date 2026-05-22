using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// Trans → EI 프로세스로 전송되는 RAIL-ACTIONCMD JSON 메시지 모델.
    /// MES ACTIONCMD 를 받은 Trans 가 jobId → vehicle 매핑을 풀어 해당 AMR 의 EI 큐로 forward 한다.
    /// </summary>
    public class RailActionCmdMessage
    {
        [JsonPropertyName("header")]
        public RailActionCmdHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailActionCmdData Data { get; set; }
    }

    public class RailActionCmdHeader
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

    public class RailActionCmdData
    {
        /// <summary>TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>할당된 Vehicle ID</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>목적지 노드 ID (LocationEx.StationId)</summary>
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }

        /// <summary>포트 (TargetPort)</summary>
        [JsonPropertyName("port")]
        public string Port { get; set; }

        /// <summary>ACTIONCMD ActionType (MES 원본)</summary>
        [JsonPropertyName("actionType")]
        public string ActionType { get; set; }

        /// <summary>JobType (TC.JobType 또는 ActionType 매핑)</summary>
        [JsonPropertyName("jobType")]
        public string JobType { get; set; }

        /// <summary>포트 종류 (LocationEx.Type: EQP / BUFFER / INPUT / OUTPUT / CHARGE / VBUFFER)</summary>
        [JsonPropertyName("portType")]
        public string PortType { get; set; }
    }
}
