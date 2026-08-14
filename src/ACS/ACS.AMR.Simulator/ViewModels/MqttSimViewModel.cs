using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using ACS.AMR.Simulator.Mqtt;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource.Model;
using ACS.Database;

namespace ACS.AMR.Simulator.ViewModels;

/// <summary>
/// "MQTT 가상 차량" 탭 ViewModel — 브로커 설정(NA_C_MQTT 로드 + 수동 편집),
/// CommType=MQTT 차량 목록 로드, 가상 차량 시작/정지를 담당한다.
/// </summary>
public partial class MqttSimViewModel : ObservableObject
{
    private readonly Func<AcsDbContext> _dbFactory;
    private readonly Action<string> _log;
    private readonly VirtualAmrManager _manager = new();

    // ── 브로커 설정 ──
    [ObservableProperty] private string _brokerIp = "localhost";
    [ObservableProperty] private int _brokerPort = 1883;
    [ObservableProperty] private string _topicPrefix = "amr/";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _mqttStatus = "미연결";

    // ── 차량 목록 ──
    public ObservableCollection<VehicleExs> MqttVehicles { get; } = new();
    [ObservableProperty] private VehicleExs _selectedVehicle;

    public ObservableCollection<VirtualAmrRowViewModel> RunningAmrs { get; } = new();
    [ObservableProperty] private VirtualAmrRowViewModel _selectedAmr;

    // ── 전역 기본 설정 (appsettings Simulator 섹션) ──
    private readonly double _defaultSpeed;
    private readonly int _defaultWorkingTimeMs;
    private readonly float _defaultBatteryLevel;
    private readonly int _statusIntervalMs;

    public MqttSimViewModel(IConfiguration configuration, Func<AcsDbContext> dbFactory, Action<string> log)
    {
        _dbFactory = dbFactory;
        _log = log;

        var sim = configuration.GetSection("Simulator");
        _defaultSpeed = sim.GetValue("SpeedMetersPerSec", 1.0);
        _defaultWorkingTimeMs = sim.GetValue("WorkingTimeMs", 3000);
        _defaultBatteryLevel = sim.GetValue("BatteryLevel", 80f);
        _statusIntervalMs = sim.GetValue("StatusIntervalMs", 1000);

        _manager.Log += msg => _log(msg);
        _manager.ConnectionChanged += connected =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsConnected = connected;
                MqttStatus = connected ? $"연결됨 ({BrokerIp}:{BrokerPort})" : "미연결";
            });
        };
    }

    /// <summary>NA_C_MQTT 첫 레코드에서 브로커 설정 로드 (없으면 수동 입력 유지)</summary>
    [RelayCommand]
    private void LoadBrokerConfig()
    {
        try
        {
            using var db = _dbFactory();
            var cfg = db.Set<MqttConfig>().FirstOrDefault();
            if (cfg == null)
            {
                _log("NA_C_MQTT 레코드 없음 — 브로커 주소를 수동 입력하세요.");
                return;
            }
            BrokerIp = cfg.BrokerIp;
            BrokerPort = cfg.BrokerPort;
            TopicPrefix = cfg.TopicPrefix ?? "amr/";
            _log($"NA_C_MQTT 로드: broker={BrokerIp}:{BrokerPort} prefix={TopicPrefix} (Name={cfg.Name})");
        }
        catch (Exception ex)
        {
            _log($"NA_C_MQTT 로드 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            _manager.BrokerIp = BrokerIp;
            _manager.BrokerPort = BrokerPort;
            _manager.TopicPrefix = string.IsNullOrWhiteSpace(TopicPrefix) ? "amr/" : TopicPrefix;
            _manager.StatusIntervalMs = _statusIntervalMs;

            LoadNodePositions();
            await _manager.ConnectAsync();
        }
        catch (Exception ex)
        {
            _log($"MQTT 연결 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try { await _manager.DisconnectAsync(); }
        catch (Exception ex) { _log($"MQTT 해제 실패: {ex.Message}"); }
    }

    /// <summary>NA_R_VEHICLE 에서 CommType=MQTT 차량 목록 로드</summary>
    [RelayCommand]
    private void LoadMqttVehicles()
    {
        try
        {
            using var db = _dbFactory();
            var list = db.Set<VehicleExs>().Where(v => v.CommType == "MQTT").ToList();
            MqttVehicles.Clear();
            foreach (var v in list) MqttVehicles.Add(v);
            _log($"CommType=MQTT 차량 {list.Count}대 로드");
        }
        catch (Exception ex)
        {
            _log($"차량 로드 실패: {ex.Message}");
        }
    }

    /// <summary>선택 차량으로 가상 AMR 시작 (command 구독 + status 발행 개시)</summary>
    [RelayCommand]
    private async Task StartSelectedAsync()
    {
        if (SelectedVehicle == null) { _log("차량을 선택하세요."); return; }
        if (string.IsNullOrWhiteSpace(SelectedVehicle.CommId))
        { _log($"차량 {SelectedVehicle.VehicleId}: CommId 가 비어 있음 (NA_C_MQTT.Name 과 일치해야 EI 가 인식)"); return; }
        if (RunningAmrs.Any(r => r.CommId == SelectedVehicle.CommId))
        { _log($"이미 실행 중: {SelectedVehicle.CommId}"); return; }

        var amr = await _manager.AddVirtualAmrAsync(
            SelectedVehicle.VehicleId, SelectedVehicle.CommId, SelectedVehicle.CurrentNodeId);
        amr.SpeedMetersPerSec = _defaultSpeed;
        amr.WorkingTimeMs = _defaultWorkingTimeMs;
        amr.BatteryLevel = _defaultBatteryLevel;

        var row = new VirtualAmrRowViewModel(amr);
        RunningAmrs.Add(row);
        SelectedAmr = row;
    }

    [RelayCommand]
    private async Task StopSelectedAsync()
    {
        if (SelectedAmr == null) { _log("실행 중인 가상 차량을 선택하세요."); return; }
        await _manager.RemoveVirtualAmrAsync(SelectedAmr.CommId);
        RunningAmrs.Remove(SelectedAmr);
        SelectedAmr = null;
    }

    /// <summary>NA_R_NODE 좌표 캐시 로드 (연결 시 자동 + 버튼 수동 갱신)</summary>
    [RelayCommand]
    private void LoadNodePositions()
    {
        try
        {
            using var db = _dbFactory();
            var positions = db.Set<NodeEx>()
                .ToList()
                .Where(n => !string.IsNullOrEmpty(n.NodeId))
                .ToDictionary(n => n.NodeId, n => (n.Xpos, n.Ypos), StringComparer.OrdinalIgnoreCase);
            _manager.SetNodePositions(positions);
        }
        catch (Exception ex)
        {
            _log($"노드 좌표 로드 실패: {ex.Message}");
        }
    }
}
