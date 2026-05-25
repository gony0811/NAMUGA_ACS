namespace ACS.UI.Models;

/// <summary>
/// SignalR VehicleHub의 "VehicleUpdate" 이벤트로 전달되는 차량 실시간 텔레메트리.
/// 서버 PoseTelemetrySubscriber가 RAIL-VEHICLEUPDATE에서 추출해 발행한다.
/// POSE(X/Y/Angle)는 미수신 시 null이며, 이 경우 UI는 위치를 갱신하지 않는다.
/// AlarmState/State/ProcessingState 등은 이 메시지에 포함되지 않는다(REST로만 갱신).
/// </summary>
public class VehicleUpdateDto
{
    public string VehicleId { get; set; }

    /// <summary>MQTT 식별자. VehicleId 매칭 실패 시 fallback 키.</summary>
    public string CommId { get; set; }

    // --- POSE (미수신 시 null) ---
    public float? PoseX { get; set; }
    public float? PoseY { get; set; }
    public float? PoseAngle { get; set; }

    // --- 상태 (RAIL-VEHICLEUPDATE 포함 필드) ---
    public string RunState { get; set; }
    public int BatteryRate { get; set; }
    public float BatteryVoltage { get; set; }
    public string CurrentNodeId { get; set; }
    public string VehicleDestNodeId { get; set; }
    public string ConnectionState { get; set; }

    public DateTime EventTime { get; set; }
}
