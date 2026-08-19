using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// ACS가 AMR에 발행하는 명령 메시지 (amr/{id}/command 토픽).
    /// docs/ACS-AMR_mqtt_movecmd.md 사양 준수.
    /// EXCHANGE 확장(exchangeCmd/actionCmd 게이트/cancelCmd returnNode)은
    /// docs/ACS-AMR_mqtt_exchangecmd.docx v0.2 기준 — 신규 필드는 null 시 직렬화 생략되어
    /// 기존 moveCmd/actionCmd 페이로드에 영향 없음.
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

<<<<<<< Updated upstream
        /// <summary>
        /// ACS Job ID (= TC JobId = cmdId). actionCmd/cancelCmd 에서 진행 중 job 과의 대조용으로 실어 보낸다.
        /// null 이면 직렬화 생략 (moveCmd 출력 불변).
        /// </summary>
=======
        // ── EXCHANGE 확장 (ACS-AMR_mqtt_exchangecmd.docx §4/§6/§7) ──

        /// <summary>ACS Exchange Job ID (= MES EXCHANGECMD JobID). exchangeCmd/actionCmd 게이트/cancelCmd 에서 사용</summary>
>>>>>>> Stashed changes
        [JsonPropertyName("jobId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string JobId { get; set; }

<<<<<<< Updated upstream
        /// <summary>
        /// actionCmd 액션 종류 (UNLOAD=기존 취출 허가 / LOAD=신규 투입 허가). jobType 과 동일 값의 별칭.
        /// null 이면 직렬화 생략.
        /// </summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Type { get; set; }
=======
        /// <summary>exchangeCmd: 신규 매거진 픽업 위치 NodeId (Loc→NodeId 변환은 ACS 담당 — 협의 #1 확정)</summary>
        [JsonPropertyName("loadSourceNode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LoadSourceNode { get; set; }

        /// <summary>exchangeCmd: 대상 설비 NodeId</summary>
        [JsonPropertyName("equipNode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string EquipNode { get; set; }

        /// <summary>exchangeCmd: 기존 매거진 반납 위치 NodeId</summary>
        [JsonPropertyName("unloadDestNode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string UnloadDestNode { get; set; }

        /// <summary>exchangeCmd: 신규 매거진 AMR 슬롯 (1|2, ACS 자동배정 결과)</summary>
        [JsonPropertyName("loadSlot")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? LoadSlot { get; set; }

        /// <summary>exchangeCmd: 회수 매거진 AMR 슬롯 (3|4, ACS 자동배정 결과)</summary>
        [JsonPropertyName("unloadSlot")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? UnloadSlot { get; set; }

        /// <summary>exchangeCmd: 픽업지 포트 유형 (기본 MATERIAL)</summary>
        [JsonPropertyName("loadSourcePortType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LoadSourcePortType { get; set; }

        /// <summary>exchangeCmd: 반납지 포트 유형 (기본 MATERIAL)</summary>
        [JsonPropertyName("unloadDestPortType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string UnloadDestPortType { get; set; }

        /// <summary>actionCmd 게이트 허가: UNLOAD=기존 매거진 취출(게이트1) / LOAD=신규 매거진 투입(게이트2)</summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Type { get; set; }

        /// <summary>cancelCmd: 적재 후 취소(C3) 시 복귀 노드. 생략 시 AMR 자동충전 노드 사용 (협의 #3)</summary>
        [JsonPropertyName("returnNode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ReturnNode { get; set; }
>>>>>>> Stashed changes
    }
}
