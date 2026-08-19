using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// Trans → EI 프로세스로 전송되는 RAIL-CANCELCMD JSON 메시지 모델.
    /// JOBCANCEL 판정(C2/C3) 시 진행 중 AMR 명령을 중단시키기 위해 Trans 가
    /// vehicle 의 EI destination 으로 forward 한다 (docs/mqtt_interface.md §cancelCmd).
    /// </summary>
    public class RailCancelCmdMessage
    {
        [JsonPropertyName("header")]
        public RailCancelCmdHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailCancelCmdData Data { get; set; }
    }

    public class RailCancelCmdHeader
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

    public class RailCancelCmdData
    {
        /// <summary>취소 대상 TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>할당된 Vehicle ID</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>
        /// 적재 후 취소(C3) 시 AMR 복귀 노드 (ACS-AMR_mqtt_exchangecmd.docx §7).
        /// 생략(null) 시 AMR 이 자동충전 노드로 복귀 — 협의 #3.
        /// </summary>
        [JsonPropertyName("returnNode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ReturnNode { get; set; }
    }
}
