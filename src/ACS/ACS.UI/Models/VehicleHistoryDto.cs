using System;

namespace ACS.UI.Models;

/// <summary>
/// /api/history/vehicles 응답 한 건. <see cref="Time"/>은 서버가 UTC로 내려준다.
/// 화면 표시 시 ToLocalTime()으로 변환한다.
/// </summary>
public class VehicleHistoryDto
{
    public string Id { get; set; }
    public DateTime? Time { get; set; }   // UTC
    public int PartitionId { get; set; }

    public string VehicleId { get; set; }
    public string BayId { get; set; }
    public string CarrierType { get; set; }
    public string ConnectionState { get; set; }
    public string AlarmState { get; set; }
    public string ProcessingState { get; set; }
    public string CurrentNodeId { get; set; }
    public string TransportCommandId { get; set; }
    public string Path { get; set; }
    public DateTime? NodeCheckTime { get; set; }
    public string State { get; set; }
    public string Installed { get; set; }
    public string TransferState { get; set; }
    public string RunState { get; set; }
    public string FullState { get; set; }
    public string MessageName { get; set; }
    public string AcsDestNodeId { get; set; }
    public string VehicleDestNodeId { get; set; }
}
