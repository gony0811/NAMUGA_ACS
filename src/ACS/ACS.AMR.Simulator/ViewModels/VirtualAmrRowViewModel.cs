using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.AMR.Simulator.Mqtt;

namespace ACS.AMR.Simulator.ViewModels;

/// <summary>
/// MQTT 가상 차량 1대의 행 ViewModel — VirtualAmr 상태를 UI 에 반영하고
/// 수동 모드 버튼(도착/완료/실패)과 실패 주입 설정을 제공한다.
/// </summary>
public partial class VirtualAmrRowViewModel : ObservableObject
{
    public VirtualAmr Amr { get; }

    public string VehicleId => Amr.VehicleId;
    public string CommId => Amr.CommId;

    [ObservableProperty] private string _state = "Idle";
    [ObservableProperty] private string _pose = "(0.00, 0.00)";
    [ObservableProperty] private string _lastCmdId = "";
    [ObservableProperty] private string _lastJobType = "";
    [ObservableProperty] private bool _autoMode = true;
    [ObservableProperty] private double _speedMetersPerSec;
    [ObservableProperty] private int _workingTimeMs;
    [ObservableProperty] private float _batteryLevel;
    [ObservableProperty] private string _failureMode = "None";
    [ObservableProperty] private int _failureResultCode = 900;

    public string[] FailureModes { get; } = { "None", "Reject", "Fail" };

    public VirtualAmrRowViewModel(VirtualAmr amr)
    {
        Amr = amr;
        _autoMode = amr.AutoMode;
        _speedMetersPerSec = amr.SpeedMetersPerSec;
        _workingTimeMs = amr.WorkingTimeMs;
        _batteryLevel = amr.BatteryLevel;

        amr.StateChanged += OnAmrStateChanged;
        OnAmrStateChanged(amr);
    }

    private void OnAmrStateChanged(VirtualAmr amr)
    {
        Dispatcher.UIThread.Post(() =>
        {
            State = amr.State.ToString();
            Pose = $"({amr.X:F2}, {amr.Y:F2})";
            LastCmdId = amr.CurrentCommand?.CmdId ?? "";
            LastJobType = amr.CurrentCommand?.JobType ?? "";
        });
    }

    // ── 설정 변경을 VirtualAmr 에 반영 ──
    partial void OnAutoModeChanged(bool value) => Amr.AutoMode = value;
    partial void OnSpeedMetersPerSecChanged(double value) => Amr.SpeedMetersPerSec = value;
    partial void OnWorkingTimeMsChanged(int value) => Amr.WorkingTimeMs = value;
    partial void OnBatteryLevelChanged(float value) => Amr.BatteryLevel = value;
    partial void OnFailureResultCodeChanged(int value) => Amr.FailureResultCode = value;
    partial void OnFailureModeChanged(string value) =>
        Amr.Failure = value switch
        {
            "Reject" => FailureInjection.Reject,
            "Fail" => FailureInjection.Fail,
            _ => FailureInjection.None
        };

    // ── 수동 모드 버튼 ──
    [RelayCommand] private void Arrive() => Amr.ManualArrive();
    [RelayCommand] private void Complete() => Amr.ManualComplete();
    [RelayCommand] private void Fail() => Amr.ManualFail();
}
