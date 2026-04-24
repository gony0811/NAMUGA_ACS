using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-CARRIERTRANSFERREPLY JSON 메시지 모델.
    /// RAIL-CARRIERTRANSFER 처리 결과를 회신한다.
    /// </summary>
    public class RailCarrierTransferReplyMessage
    {
        [JsonPropertyName("header")]
        public RailCarrierTransferHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailCarrierTransferReplyData Data { get; set; }
    }

    public class RailCarrierTransferReplyData
    {
        /// <summary>TransportCommand JobId</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>결과 코드 ("OK" 또는 "FAIL")</summary>
        [JsonPropertyName("resultCode")]
        public string ResultCode { get; set; }
    }
}
