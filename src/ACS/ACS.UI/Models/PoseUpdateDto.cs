namespace ACS.UI.Models;

/// <summary>
/// SignalR VehicleHub의 PoseUpdate 이벤트로 전달되는 차량 위치 텔레메트리.
/// 서버 PoseTelemetrySubscriber가 RailVehicleUpdateMessage에서 추출해 발행한다.
/// </summary>
public class PoseUpdateDto
{
    public string VehicleId { get; set; }
    public string CommId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
    public DateTime EventTime { get; set; }
}
