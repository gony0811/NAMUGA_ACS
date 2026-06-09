using System;

namespace ACS.UI.Models;

/// <summary>
/// /api/history/transport-commands 응답 한 건. <see cref="Time"/>은 서버가 UTC로 내려준다.
/// 화면 표시 시 ToLocalTime()으로 변환한다.
/// </summary>
public class TransportCommandHistoryDto
{
    public string Id { get; set; }
    public DateTime? Time { get; set; }   // UTC
    public int PartitionId { get; set; }

    public string JobId { get; set; }
    public int Priority { get; set; }
    public string State { get; set; }
    public string VehicleId { get; set; }
    public string VehicleEvent { get; set; }
    public string CarrierId { get; set; }
    public string Source { get; set; }
    public string Dest { get; set; }
    public string Path { get; set; }
    public string JobType { get; set; }
    public string BayId { get; set; }
    public string EqpId { get; set; }
    public string PortId { get; set; }
    public string AgvName { get; set; }
    public string MidLoc { get; set; }
    public string MidPortId { get; set; }
    public string OriginLoc { get; set; }
    public string Reason { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public string AdditionalInfo { get; set; }

    public DateTime? CreateTime { get; set; }
    public DateTime? QueuedTime { get; set; }
    public DateTime? AssignedTime { get; set; }
    public DateTime? StartedTime { get; set; }
    public DateTime? LoadArrivedTime { get; set; }
    public DateTime? LoadedTime { get; set; }
    public DateTime? UnloadArrivedTime { get; set; }
    public DateTime? UnloadedTime { get; set; }
    public DateTime? LoadingTime { get; set; }
    public DateTime? UnloadingTime { get; set; }
    public DateTime? CompletedTime { get; set; }
}
