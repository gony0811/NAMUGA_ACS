namespace ACS.UI.Models;

public class VehicleDto
{
    public string VehicleId { get; set; }
    public string State { get; set; }
    public string ConnectionState { get; set; }
    public string ProcessingState { get; set; }
    public string RunState { get; set; }
    public string AlarmState { get; set; }
    public string TransferState { get; set; }
    public int BatteryRate { get; set; }
    public float BatteryVoltage { get; set; }
    public string CurrentNodeId { get; set; }
    public string AcsDestNodeId { get; set; }
    public string VehicleDestNodeId { get; set; }
    public string TransportCommandId { get; set; }
    public string BayId { get; set; }
    public string CarrierType { get; set; }

    /// <summary>SignalR로 수신한 실시간 X 좌표 (meters). 미수신 시 null.</summary>
    public float? PoseX { get; set; }

    /// <summary>SignalR로 수신한 실시간 Y 좌표 (meters). 미수신 시 null.</summary>
    public float? PoseY { get; set; }

    /// <summary>SignalR로 수신한 실시간 각도 (radian). 미수신 시 null.</summary>
    public float? PoseAngle { get; set; }
}
