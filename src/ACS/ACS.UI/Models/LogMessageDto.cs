using System;

namespace ACS.UI.Models;

/// <summary>
/// /api/logs 응답 한 건. <see cref="Time"/>은 서버가 UTC로 내려준다(역직렬화 시 Kind=Utc).
/// 화면 표시 시 ToLocalTime()으로 변환한다.
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
    public bool HasLargeText { get; set; }
}
