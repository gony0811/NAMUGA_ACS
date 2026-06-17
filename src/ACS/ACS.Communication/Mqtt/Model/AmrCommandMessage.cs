using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// ACS가 AMR에 발행하는 명령 메시지 (amr/{id}/command 토픽).
    /// docs/ACS-AMR_mqtt_movecmd.md 사양 준수.
    /// </summary>
    public class AmrCommandMessage
    {
        /// <summary>명령 일련번호 (년월일_시분초_일련번호)</summary>
        [JsonPropertyName("cmdId")]
        public string CmdId { get; set; }

        /// <summary>명령 종류 (moveCmd, actionCmd 등)</summary>
        [JsonPropertyName("command")]
        public string Command { get; set; }

        /// <summary>명령 대상 노드 ID</summary>
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }

        /// <summary>포트 위치 (LEFT / RIGHT)</summary>
        [JsonPropertyName("port")]
        public string Port { get; set; }

        /// <summary>목적지에 도착해서 할 일 (LOAD / UNLOAD / EXCHANGE)</summary>
        [JsonPropertyName("jobType")]
        public string JobType { get; set; }

        /// <summary>포트 유형 (LocationEx.Type 값: EQP / BUFFER / INPUT / OUTPUT / CHARGE / VBUFFER). EI는 도메인 값을 그대로 송신하며 AMR이 분기 처리.</summary>
        [JsonPropertyName("portType")]
        public string PortType { get; set; }

        /// <summary>모델명 (MOVECMD.MODEL 값). 비어있을 수 있음.</summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>AMR 슬롯 번호 (1~4, 기본 1)</summary>
        [JsonPropertyName("amrSlot")]
        public int AmrSlot { get; set; } = 1;
    }
}
