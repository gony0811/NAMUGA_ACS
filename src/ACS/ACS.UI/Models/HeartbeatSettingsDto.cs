namespace ACS.UI.Models;

/// <summary>
/// 백엔드 /api/heartbeat-settings 응답/요청 (control heartbeat 설정 미러).
/// FailWhenProcessDown/Hang: 0=없음, 1=상태표시만, 2=재시작.
/// </summary>
public class HeartbeatSettingsDto
{
    public bool UseHeartBeat { get; set; }
    public long HeartBeatInterval { get; set; }
    public long HeartBeatStartDelay { get; set; }
    public long HeartBeatStartupGrace { get; set; }
    public long HeartBeatTimeout { get; set; }
    public int HeartBeatRetryCount { get; set; }
    public long HeartBeatRetryTimeout { get; set; }
    public int HeartBeatFailWhenProcessDown { get; set; }
    public int HeartBeatFailWhenProcessHang { get; set; }
}
