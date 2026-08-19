using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// AMR이 command에 대한 진행/완료를 알리는 reply 메시지 (amr/{id}/reply 토픽).
    /// status:
    ///  - COMPLETED / STEP_COMPLETE(별칭, step 필수) → jobType(또는 TC 역추정)에 따라 Trans 로
    ///    RAIL-VEHICLEACQUIRECOMPLETED(UNLOAD) / RAIL-VEHICLEDEPOSITCOMPLETED(LOAD) / RAIL-VEHICLEEXCHANGECOMPLETED(EXCHANGE)
    ///  - ARRIVED → RAIL-VEHICLEARRIVED (pose 기반 도착 판정과 OR)
    ///  - FAILED / REJECTED → EXCHANGE TC 면 RAIL-VEHICLEJOBFAILED
    ///  - CANCELED → 로그만 (Trans 는 reply 대기 없이 취소 처리 완료)
    ///  - ACCEPTED / EXECUTING → 무시
    /// jobId/step/stepName/carrierSlot 은 선택 필드 (docs/ACS-AMR_mqtt_exchange.md v0.3).
    /// </summary>
    public class AmrReplyMessage
    {
        /// <summary>원 command의 cmdId (TC JobId와 동일하게 set되어야 Trans에서 TC 조회 가능)</summary>
        [JsonPropertyName("cmdId")]
        public string CmdId { get; set; }

        /// <summary>ACCEPTED / REJECTED / EXECUTING / ARRIVED / STEP_COMPLETE / COMPLETED / FAILED / CANCELED</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>0: 성공, 기타: 에러 코드 (REJECTED 2/10/11/20/21/22, FAILED 30/31/32/99, CANCELED 40)</summary>
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        /// <summary>상세 사유</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>LOAD / UNLOAD / EXCHANGE (command와 동일)</summary>
        [JsonPropertyName("jobType")]
        public string JobType { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>ACS Job ID (선택). 있으면 cmdId 대신 TC 조회 키로 우선 사용.</summary>
        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        /// <summary>EXCHANGE 단계 코드 10/20/30/40/50/60 (선택). STEP_COMPLETE 에서는 필수.</summary>
        [JsonPropertyName("step")]
        public int? Step { get; set; }

        /// <summary>단계명 PICKUP_NEW / MOVE_TO_EQUIP / UNLOAD_OLD / LOAD_NEW / RETURN_OLD / DONE (선택)</summary>
        [JsonPropertyName("stepName")]
        public string StepName { get; set; }

        /// <summary>해당 단계에서 조작한 AMR 슬롯 1~4 (선택)</summary>
        [JsonPropertyName("carrierSlot")]
        public int? CarrierSlot { get; set; }

        /// <summary>토픽에서 파싱한 AMR vehicleId (payload에 없으므로 handler가 채움)</summary>
        [JsonIgnore]
        public string VehicleId { get; set; }

        // ── EXCHANGE 단계 보고 확장 (ACS-AMR_mqtt_exchangecmd.docx §5) ──
        //  기존 moveCmd 응답에는 없는 필드 — 미수신 시 null/0 유지.
        //  status 신규 값: STEP_COMPLETE(단계 완료), CANCELED(취소 처리 완료)

        /// <summary>exchangeCmd 의 jobId 그대로 반환</summary>
        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        /// <summary>단계 코드 (10/20/30/40/50/60 — MES 사양과 동일 값)</summary>
        [JsonPropertyName("step")]
        public int? Step { get; set; }

        /// <summary>PICKUP_NEW / MOVE_TO_EQUIP / UNLOAD_OLD / LOAD_NEW / RETURN_OLD / DONE</summary>
        [JsonPropertyName("stepName")]
        public string StepName { get; set; }

        /// <summary>해당 단계에서 사용한 AMR 슬롯 (STEP_COMPLETE 30/40/50 에서 필수)</summary>
        [JsonPropertyName("carrierSlot")]
        public int? CarrierSlot { get; set; }
    }
}
