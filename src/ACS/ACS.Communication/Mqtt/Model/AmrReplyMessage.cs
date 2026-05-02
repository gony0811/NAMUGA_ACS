using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// AMR이 command에 대한 진행/완료를 알리는 reply 메시지 (amr/{id}/reply 토픽).
    /// status=ARRIVED 시 ACS는 Trans로 RAIL-VEHICLEARRIVED를,
    /// status=COMPLETED 시 jobType에 따라
    /// RAIL-VEHICLEACQUIRECOMPLETED(UNLOAD) 또는 RAIL-VEHICLEDEPOSITCOMPLETED(LOAD)를 발송한다.
    /// </summary>
    public class AmrReplyMessage
    {
        /// <summary>원 command의 cmdId (TC JobId와 동일하게 set되어야 Trans에서 TC 조회 가능)</summary>
        [JsonPropertyName("cmdId")]
        public string CmdId { get; set; }

        /// <summary>ACCEPTED / REJECTED / EXECUTING / ARRIVED / COMPLETED / FAILED</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>0: 성공, 기타: 에러 코드</summary>
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        /// <summary>상세 사유</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>LOAD / UNLOAD / EXCHANGE (command와 동일)</summary>
        [JsonPropertyName("jobType")]
        public string JobType { get; set; }

        /// <summary>도착한 노드 ID (status=ARRIVED 시 사용). 비어있으면 EI가 cmdId로 TC 조회해 보완.</summary>
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>토픽에서 파싱한 AMR vehicleId (payload에 없으므로 handler가 채움)</summary>
        [JsonIgnore]
        public string VehicleId { get; set; }
    }
}
