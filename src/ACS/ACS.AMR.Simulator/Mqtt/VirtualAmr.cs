using ACS.Communication.Mqtt.Model;

namespace ACS.AMR.Simulator.Mqtt;

/// <summary>가상 차량 상태머신 상태</summary>
public enum VirtualAmrState
{
    Idle,       // 명령 대기
    Accepted,   // 명령 수락 (ACCEPTED reply 발행 직후)
    Moving,     // 목표 노드로 pose 보간 이동 중
    Arrived,    // 목표 노드 도착 (pose 스냅 완료)
    Working     // 도킹/작업 중 (완료 시 COMPLETED reply)
}

/// <summary>실패 주입 모드</summary>
public enum FailureInjection
{
    None,       // 정상 진행
    Reject,     // 명령 수신 즉시 REJECTED reply
    Fail        // 작업 완료 대신 FAILED reply
}

/// <summary>
/// MQTT 가상 차량 1대의 상태머신 + pose 직선 보간.
/// MQTT/UI 에 의존하지 않으며 reply/로그는 이벤트로 위임한다 (테스트 가능한 코어).
/// 수신 스레드(OnCommand)·틱 스레드(Tick)·UI 버튼(Manual*) 3곳에서 접근하므로 단일 lock 으로 보호.
/// </summary>
public class VirtualAmr
{
    private readonly object _sync = new();

    public string VehicleId { get; }
    public string CommId { get; }

    // ── 상태 (lock 하에 갱신) ──
    public VirtualAmrState State { get; private set; } = VirtualAmrState.Idle;
    public double X { get; private set; }
    public double Y { get; private set; }
    public bool IsFull { get; private set; }
    public AmrCommandMessage CurrentCommand { get; private set; }

    // ── 설정 (UI 에서 변경 가능) ──
    /// <summary>자동 모드: 수신 즉시 이동→도착→작업완료까지 자동 진행</summary>
    public bool AutoMode { get; set; } = true;
    public double SpeedMetersPerSec { get; set; } = 1.0;
    public int WorkingTimeMs { get; set; } = 3000;
    public float BatteryLevel { get; set; } = 80f;
    public FailureInjection Failure { get; set; } = FailureInjection.None;
    /// <summary>실패 주입 시 사용할 resultCode (0 이외)</summary>
    public int FailureResultCode { get; set; } = 900;

    // ── 이벤트 (매니저/VM 이 구독) ──
    /// <summary>reply 발행 요청 (MQTT 발행은 매니저 책임)</summary>
    public event Action<VirtualAmr, AmrReplyMessage> ReplyRequested;
    /// <summary>상태 변화 알림 (UI 갱신용)</summary>
    public event Action<VirtualAmr> StateChanged;
    public event Action<VirtualAmr, string> Log;

    private double _targetX;
    private double _targetY;
    private double _workingElapsedMs;

    public VirtualAmr(string vehicleId, string commId, double startX, double startY)
    {
        VehicleId = vehicleId;
        CommId = commId;
        X = startX;
        Y = startY;
    }

    /// <summary>
    /// command 토픽 수신 처리. 목표 노드 좌표는 매니저가 NA_R_NODE 캐시에서 조회해 전달한다.
    /// targetPos 가 null 이면 노드 좌표를 찾지 못한 것 → FAILED 회신.
    /// </summary>
    public void OnCommand(AmrCommandMessage cmd, (double X, double Y)? targetPos)
    {
        lock (_sync)
        {
            // cancelCmd (v0.3): 진행 중 명령 폐기 → 현 위치 정지 후 Idle 복귀, CANCELED reply 발행.
            //  - 진행 중 명령이 있고 jobId(없으면 cmdId) 가 일치 → CANCELED(0)
            //  - Idle 이거나 jobId 불일치 → CANCELED(40 CANCEL_REJECTED)
            if ("cancelCmd".Equals(cmd.Command, StringComparison.OrdinalIgnoreCase))
            {
                string targetJob = cmd.JobId ?? cmd.CmdId;
                string currentJob = CurrentCommand?.JobId ?? CurrentCommand?.CmdId;
                bool active = State != VirtualAmrState.Idle && CurrentCommand != null;
                bool match = active && (string.IsNullOrEmpty(targetJob)
                                        || string.Equals(targetJob, currentJob, StringComparison.OrdinalIgnoreCase));
                if (match)
                {
                    RaiseLog($"취소 명령 수신: jobId={targetJob} — 현재 명령(cmdId={CurrentCommand?.CmdId}, 상태={State}) 폐기 후 Idle 복귀 → CANCELED(0)");
                    ToIdle();
                    SendReply("CANCELED", 0, "canceled by simulator", cmdIdOverride: cmd.CmdId, jobIdOverride: targetJob);
                }
                else
                {
                    RaiseLog($"취소 명령 수신: jobId={targetJob} — 진행 중 명령 없음/불일치(현재 {currentJob}, 상태={State}) → CANCELED(40 CANCEL_REJECTED)");
                    SendReply("CANCELED", 40, "cancel rejected: no matching active job", cmdIdOverride: cmd.CmdId, jobIdOverride: targetJob);
                }
                StateChanged?.Invoke(this);
                return;
            }

            if (State != VirtualAmrState.Idle)
                RaiseLog($"경고: {State} 상태에서 새 명령 수신 — 기존 cmdId={CurrentCommand?.CmdId} 를 버리고 교체");

            CurrentCommand = cmd;

            if (Failure == FailureInjection.Reject)
            {
                RaiseLog($"REJECTED 주입: cmdId={cmd.CmdId} resultCode={FailureResultCode}");
                SendReply("REJECTED", FailureResultCode, "rejected by simulator");
                ToIdle();
                return;
            }

            if (targetPos == null)
            {
                RaiseLog($"노드 좌표 없음: nodeId={cmd.NodeId} → FAILED 회신");
                SendReply("FAILED", 404, $"unknown node {cmd.NodeId}");
                ToIdle();
                return;
            }

            _targetX = targetPos.Value.X;
            _targetY = targetPos.Value.Y;

            SendReply("ACCEPTED", 0, null);
            State = VirtualAmrState.Accepted;
            RaiseLog($"명령 수락: cmdId={cmd.CmdId} command={cmd.Command} nodeId={cmd.NodeId} jobType={cmd.JobType}");

            SendReply("EXECUTING", 0, null);
            State = VirtualAmrState.Moving;

            // 이미 목표 위 (0.1m 이내)면 즉시 도착 처리
            if (DistanceToTarget() < 0.1)
                ArriveInternal();

            StateChanged?.Invoke(this);
        }
    }

    /// <summary>매니저의 주기 틱 (기본 100ms). 자동 모드에서 이동/작업 진행.</summary>
    public void Tick(double deltaSec)
    {
        bool changed = false;
        lock (_sync)
        {
            switch (State)
            {
                case VirtualAmrState.Moving when AutoMode:
                    double dist = DistanceToTarget();
                    double step = SpeedMetersPerSec * deltaSec;
                    if (dist <= step)
                    {
                        ArriveInternal();
                    }
                    else
                    {
                        X += (_targetX - X) / dist * step;
                        Y += (_targetY - Y) / dist * step;
                    }
                    changed = true;
                    break;

                case VirtualAmrState.Arrived when AutoMode:
                    State = VirtualAmrState.Working;
                    _workingElapsedMs = 0;
                    changed = true;
                    break;

                case VirtualAmrState.Working when AutoMode:
                    _workingElapsedMs += deltaSec * 1000;
                    if (_workingElapsedMs >= WorkingTimeMs)
                    {
                        CompleteInternal();
                        changed = true;
                    }
                    break;
            }
        }
        if (changed) StateChanged?.Invoke(this);
    }

    /// <summary>수동 모드: 도착 버튼 — pose 를 목표 노드 좌표로 스냅</summary>
    public void ManualArrive()
    {
        lock (_sync)
        {
            if (State != VirtualAmrState.Moving) { RaiseLog($"도착 불가: 상태={State}"); return; }
            ArriveInternal();
            State = VirtualAmrState.Working;
        }
        StateChanged?.Invoke(this);
    }

    /// <summary>수동 모드: 완료 버튼 — COMPLETED reply</summary>
    public void ManualComplete()
    {
        lock (_sync)
        {
            if (State != VirtualAmrState.Arrived && State != VirtualAmrState.Working)
            { RaiseLog($"완료 불가: 상태={State}"); return; }
            CompleteInternal();
        }
        StateChanged?.Invoke(this);
    }

    /// <summary>수동/즉시 실패 — FAILED reply 후 Idle 복귀</summary>
    public void ManualFail()
    {
        lock (_sync)
        {
            if (State == VirtualAmrState.Idle || CurrentCommand == null)
            { RaiseLog("실패 불가: 진행 중인 명령 없음"); return; }
            RaiseLog($"FAILED: cmdId={CurrentCommand.CmdId} resultCode={FailureResultCode}");
            SendReply("FAILED", FailureResultCode, "failed by simulator");
            ToIdle();
        }
        StateChanged?.Invoke(this);
    }

    /// <summary>현재 상태 스냅샷으로 status 메시지 생성 (1Hz 발행용)</summary>
    public AmrStatusMessage BuildStatus()
    {
        lock (_sync)
        {
            bool moving = State == VirtualAmrState.Moving;
            return new AmrStatusMessage
            {
                State = new AmrState
                {
                    // EI MapRunState/MapFullState 는 대소문자 정확 매칭 — "Run"/"Stop", "Full"/"Empty" 고정
                    RunState = moving ? "Run" : "Stop",
                    FullState = IsFull ? "Full" : "Empty",
                    WorkState = State switch
                    {
                        VirtualAmrState.Moving => "Moving",
                        VirtualAmrState.Arrived or VirtualAmrState.Working => "Docking",
                        _ => "Idle"
                    },
                    VehicleDestNode = (State is VirtualAmrState.Moving or VirtualAmrState.Arrived or VirtualAmrState.Working)
                        ? CurrentCommand?.NodeId ?? "" : ""
                },
                Pose = new AmrPose { X = (float)X, Y = (float)Y, Angle = 0f },
                Error = new AmrError { Code = 0, Message = "" },
                Battery = new AmrBattery
                {
                    LevelPercent = BatteryLevel,
                    Voltage = 26f,
                    Current = 0f,
                    TemperatureCelsius = 30f,
                    // EI ParseAmrStatusActivity 가 ChargingState.ToUpper() 호출 — null 금지
                    ChargingState = "Discharging"
                },
                Abnormal = null
            };
        }
    }

    // ── 내부 (lock 보유 상태에서만 호출) ──

    private void ArriveInternal()
    {
        // NearestNodeFinder 임계(2m) 이내 보장을 위해 목표 좌표에 정확히 스냅
        X = _targetX;
        Y = _targetY;
        State = VirtualAmrState.Arrived;
        _workingElapsedMs = 0;
        RaiseLog($"도착: nodeId={CurrentCommand?.NodeId} pose=({X:F2},{Y:F2}) → ARRIVED");
        // v0.3: 명시적 도착 보고 (ACS 는 pose 기반 판정과 OR 로 처리, 중복 보고는 ACS 가 방어)
        SendReply("ARRIVED", 0, null);
    }

    private void CompleteInternal()
    {
        if (Failure == FailureInjection.Fail)
        {
            RaiseLog($"FAILED 주입: cmdId={CurrentCommand?.CmdId} resultCode={FailureResultCode}");
            SendReply("FAILED", FailureResultCode, "failure injected");
            ToIdle();
            return;
        }

        // jobType 에 따라 적재 상태 갱신: UNLOAD=source 에서 집음→Full, LOAD=dest 에 내림→Empty
        switch (CurrentCommand?.JobType?.ToUpperInvariant())
        {
            case "UNLOAD": IsFull = true; break;
            case "LOAD": IsFull = false; break;
            case "EXCHANGE": IsFull = !IsFull; break;
        }

        RaiseLog($"작업 완료: cmdId={CurrentCommand?.CmdId} jobType={CurrentCommand?.JobType} → COMPLETED");
        SendReply("COMPLETED", 0, null);
        ToIdle();
    }

    private void ToIdle()
    {
        // CurrentCommand 는 표시용(마지막 cmdId/jobType)으로 유지
        State = VirtualAmrState.Idle;
    }

    private void SendReply(string status, int resultCode, string message,
        string cmdIdOverride = null, string jobIdOverride = null)
    {
        var cmd = CurrentCommand;
        var reply = new AmrReplyMessage
        {
            CmdId = cmdIdOverride ?? cmd?.CmdId,
            Status = status,
            ResultCode = resultCode,
            Message = message ?? "",
            JobType = cmd?.JobType ?? "",
            Timestamp = DateTime.UtcNow,
            // v0.3 선택 필드: jobId(=command.jobId ?? cmdId), carrierSlot(=command.amrSlot, 완료 계열만). step 은 시뮬레이터가 알 수 없어 미기재.
            JobId = jobIdOverride ?? cmd?.JobId ?? cmd?.CmdId,
            CarrierSlot = ("COMPLETED".Equals(status, StringComparison.OrdinalIgnoreCase) && cmd != null) ? cmd.AmrSlot : (int?)null
        };
        ReplyRequested?.Invoke(this, reply);
    }

    private double DistanceToTarget()
    {
        double dx = _targetX - X, dy = _targetY - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void RaiseLog(string msg) => Log?.Invoke(this, $"[{VehicleId}] {msg}");
}
