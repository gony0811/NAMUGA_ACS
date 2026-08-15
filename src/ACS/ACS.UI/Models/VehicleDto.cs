namespace ACS.UI.Models;

public class VehicleDto
{
    public string VehicleId { get; set; }
    public string CommType { get; set; }

    /// <summary>MQTT 식별자 (VehicleEx.CommId). SignalR PoseUpdate 매칭 fallback 키.</summary>
    public string CommId { get; set; }

    public string Vendor { get; set; }
    public string Version { get; set; }
    public string PlcVersion { get; set; }
    public string State { get; set; }
    public string Installed { get; set; }
    public string ConnectionState { get; set; }
    public string ProcessingState { get; set; }
    public string RunState { get; set; }
    public string FullState { get; set; }
    public string AlarmState { get; set; }
    public string TransferState { get; set; }
    public int BatteryRate { get; set; }
    public float BatteryVoltage { get; set; }
    public string CurrentNodeId { get; set; }
    public string AcsDestNodeId { get; set; }
    public string VehicleDestNodeId { get; set; }
    public string TransportCommandId { get; set; }
    public string Path { get; set; }
    public DateTime? NodeCheckTime { get; set; }
    public DateTime? EventTime { get; set; }
    public DateTime? LastChargeTime { get; set; }
    public float LastChargeBattery { get; set; }
    public string BayId { get; set; }
    public string CarrierType { get; set; }

    /// <summary>SignalR로 수신한 실시간 X 좌표 (meters). 미수신 시 null.</summary>
    public float? PoseX { get; set; }

    /// <summary>SignalR로 수신한 실시간 Y 좌표 (meters). 미수신 시 null.</summary>
    public float? PoseY { get; set; }

    /// <summary>SignalR로 수신한 실시간 각도 (radian). 미수신 시 null.</summary>
    public float? PoseAngle { get; set; }

    /// <summary>EXCHANGE(v2): 차량 슬롯 상태 (NA_R_VEHICLE_SLOT, slotNo 오름차순 4행) — Vehicle View 행 선택 상세(RowDetails)에서 표시</summary>
    public List<VehicleSlotDto> Slots { get; set; } = new();
}

/// <summary>차량 슬롯 1행 (NA_R_VEHICLE_SLOT) — 서버 VehicleSlotDto 미러.</summary>
public class VehicleSlotDto
{
    public int SlotNo { get; set; }
    public string Role { get; set; }
    public string State { get; set; }
    public string JobId { get; set; }
    public string Phase { get; set; }
    public DateTime? UpdatedTime { get; set; }
}
