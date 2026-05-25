using System;

namespace ACS.UI.Models;

/// <summary>
/// 로그 조회 필터. From/To는 <b>로컬(컴퓨터) 시간</b>으로 보관하며,
/// API 전송 시 AcsApiService에서 UTC로 변환한다.
/// </summary>
public class LogQueryFilter
{
    public DateTime? FromLocal { get; set; }
    public DateTime? ToLocal { get; set; }
    public string Level { get; set; }
    public string Keyword { get; set; }
    public string ProcessName { get; set; }
    public string MessageName { get; set; }
    public string TransactionId { get; set; }
    public int Limit { get; set; } = 1000;
}
