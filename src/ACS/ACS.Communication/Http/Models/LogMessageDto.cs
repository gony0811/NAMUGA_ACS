using System;

namespace ACS.Communication.Http.Models
{
    /// <summary>
    /// NA_L_LOGMESSAGE 한 건을 표현하는 전송 DTO.
    /// <see cref="Time"/>은 항상 UTC(Kind=Utc)로 직렬화되며, 로컬 시간 변환은 클라이언트가 담당한다.
    /// </summary>
    public class LogMessageDto
    {
        public string Id { get; set; }
        public DateTime? Time { get; set; }   // UTC
        public string LogLevel { get; set; }
        public string ProcessName { get; set; }
        public string MessageName { get; set; }
        public string CommunicationMessageName { get; set; }
        public string TransactionId { get; set; }
        public string TransportCommandId { get; set; }
        public string OperationName { get; set; }
        public string ThreadName { get; set; }
        public string CarrierName { get; set; }
        public string MachineName { get; set; }
        public string UnitName { get; set; }
        public string Text { get; set; }

        /// <summary>
        /// 본문 Text가 비어 있어 NA_L_LARGELOGMESSAGE에 분할 저장되었을 가능성 표시.
        /// 실제 전체 텍스트는 /api/logs/{id}/text로 조회한다.
        /// </summary>
        public bool HasLargeText { get; set; }
    }
}
