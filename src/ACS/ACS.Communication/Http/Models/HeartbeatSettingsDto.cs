namespace ACS.Communication.Http.Models
{
    /// <summary>
    /// control 프로세스 heartbeat 설정 (NA_X_OPTION 8001~8009에 영구 저장).
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
}
