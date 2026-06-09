using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEABNORMAL JSON 메시지 모델.
    /// AMR status 의 abnormal 블록이 감지되면 ES 가 type 무관하게 전송하고,
    /// Trans 측 RailVehicleAbnormalWorkflow 가 type 별로 분기 처리한다.
    /// 현재 처리 대상: OPERATOR_ABORT (TC 삭제 + Vehicle 할당 초기화 + IDLE/NOTASSIGNED 전이).
    /// </summary>
    public class RailVehicleAbnormalMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleAbnormalHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleAbnormalData Data { get; set; }
    }

    public class RailVehicleAbnormalHeader
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

    public class RailVehicleAbnormalData
    {
        public const string TYPE_OPERATOR_ABORT = "OPERATOR_ABORT";
        public const string CODE_OPERATOR_ABORT = "200";

        /// <summary>DB PK (VehicleEx.VehicleId)</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>MQTT vehicleId (CommId)</summary>
        [JsonPropertyName("commId")]
        public string CommId { get; set; }

        /// <summary>비정상 유형 이름 (예: OPERATOR_ABORT)</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>비정상 코드 (예: "200"). AMR 이 코드만 보내거나 이름만 보내는 경우 모두 매칭하기 위함.</summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }

        /// <summary>AMR 이 보고한 발생 노드</summary>
        [JsonPropertyName("node")]
        public string Node { get; set; }

        /// <summary>AMR 이 보고한 발생 시각</summary>
        [JsonPropertyName("abnormalTime")]
        public DateTime AbnormalTime { get; set; }

        /// <summary>ES 가 메시지를 보내는 시각</summary>
        [JsonPropertyName("eventTime")]
        public DateTime EventTime { get; set; }
    }
}
