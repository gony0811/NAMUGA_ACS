using System;

namespace ACS.UI.Models;

/// <summary>
/// Vehicle History 조회 필터. From/To는 <b>로컬(컴퓨터) 시간</b>으로 보관하며,
/// API 전송 시 AcsApiService에서 UTC로 변환한다.
/// </summary>
public class VehicleHistoryQueryFilter
{
    public DateTime? FromLocal { get; set; }
    public DateTime? ToLocal { get; set; }
    public string VehicleId { get; set; }
    public string BayId { get; set; }
    public string State { get; set; }
    public string TransportCommandId { get; set; }
    public string MessageName { get; set; }
    public int Limit { get; set; } = 1000;
}
