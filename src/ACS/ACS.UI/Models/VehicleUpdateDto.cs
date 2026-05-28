namespace ACS.UI.Models;

/// <summary>
/// SignalR VehicleHub의 "VehicleUpdate" 이벤트로 전달되는 차량 실시간 텔레메트리.
/// 서버 PoseTelemetrySubscriber가 Trans 권위 상태 스냅샷에서 추출해 발행한다.
/// POSE(X/Y/Angle)는 미수신 시 null이며, 이 경우 UI는 위치를 갱신하지 않는다.
/// ProcessingState/State/AcsDestNodeId/TransportCommandId/Path 등 상태 필드는
/// Trans가 forward 직전 vehicle 권위값으로 채워 보내므로 실시간으로 갱신된다.
/// (AlarmState는 별도 메시지 RAIL-VEHICLEALARM 경로이므로 여기에 포함되지 않는다.)
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

    // --- 상태 (Trans 권위 스냅샷) ---
    public string RunState { get; set; }
    public string ProcessingState { get; set; }
    public string State { get; set; }
    public string TransferState { get; set; }
    public int BatteryRate { get; set; }
    public float BatteryVoltage { get; set; }
    public string CurrentNodeId { get; set; }
    public string AcsDestNodeId { get; set; }
    public string VehicleDestNodeId { get; set; }
    public string TransportCommandId { get; set; }
    public string Path { get; set; }
    public string ConnectionState { get; set; }

    public DateTime EventTime { get; set; }
}
