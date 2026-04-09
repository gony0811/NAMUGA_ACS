using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// ACS가 AMR에 발행하는 명령 메시지 (amr/{id}/command 토픽).
    /// mqtt_interface.md 스펙 준수.
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
    }
}
