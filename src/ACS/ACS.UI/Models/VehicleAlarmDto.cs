namespace ACS.UI.Models;

/// <summary>
/// SignalR VehicleHub의 "VehicleAlarm" 이벤트로 전달되는 AMR 알람 SET/RESET 전이.
/// 서버 PoseTelemetrySubscriber가 Trans forward RAIL-VEHICLEALARM에서 추출해 발행한다.
/// 상태 전이 시에만 수신되므로 (1Hz 반복 아님) 수신 즉시 UI에 반영해야 한다.
/// </summary>
public class VehicleAlarmDto
{
    public string VehicleId { get; set; }

    /// <summary>MQTT 식별자. VehicleId 매칭 실패 시 fallback 키.</summary>
    public string CommId { get; set; }

    /// <summary>"SET" (알람 발생) 또는 "RESET" (알람 해소)</summary>
    public string Type { get; set; }

    /// <summary>AMR error code (SET 시 nonzero, RESET 시 0)</summary>
    public int ErrorCode { get; set; }

    /// <summary>AMR error message (SET 시 사유, RESET 시 비어있을 수 있음)</summary>
    public string ErrorMessage { get; set; }

    public DateTime EventTime { get; set; }
}
