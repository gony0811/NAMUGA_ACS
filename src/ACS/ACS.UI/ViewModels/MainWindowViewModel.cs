using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;
using ACS.UI.Views;

namespace ACS.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAcsApiService _apiService;
    private readonly Dictionary<string, Window> _popupWindows = new();

    [ObservableProperty]
    private MapViewModel _mapViewModel;

    [ObservableProperty]
    private VehicleListViewModel _vehicleListViewModel;

    [ObservableProperty]
    private DashboardViewModel _dashboardViewModel;

    [ObservableProperty]
    private SummaryViewModel _summaryViewModel;

    [ObservableProperty]
    private DataViewViewModel _dataViewViewModel;

    [ObservableProperty]
    private LogViewModel _logViewModel;

    [ObservableProperty]
    private ApplicationViewModel _applicationViewModel;

    [ObservableProperty]
    private AppManagementViewModel _appManagementViewModel;

    [ObservableProperty]
    private MqttViewModel _mqttViewModel;

    [ObservableProperty]
    private HostCommunicationViewModel _hostCommunicationViewModel;

    [ObservableProperty]
    private NodeViewModel _nodeViewModel;

    [ObservableProperty]
    private StationViewModel _stationViewModel;

    [ObservableProperty]
    private LinkViewModel _linkViewModel;

    [ObservableProperty]
    private BayViewModel _bayViewModel;

    [ObservableProperty]
    private ZoneViewModel _zoneViewModel;

    [ObservableProperty]
    private PortViewModel _portViewModel;

    [ObservableProperty]
    private LinkZoneViewModel _linkZoneViewModel;

    [ObservableProperty]
    private TransferCommandViewModel _transferCommandViewModel;

    [ObservableProperty]
    private VehicleViewModel _vehicleViewModel;

    [ObservableProperty]
    private TransportCmdHistoryViewModel _transportCmdHistoryViewModel;

    [ObservableProperty]
    private VehicleHistoryViewModel _vehicleHistoryViewModel;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _lastUpdateTime = "-";

    // 자동 업데이트: 다운로드 완료된 새 버전 (null이면 배너 숨김)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateReady))]
    private string _updateReadyVersion;

    public bool IsUpdateReady => !string.IsNullOrEmpty(UpdateReadyVersion);

    private Action _applyUpdateAndRestart;

    // Ribbon tab selection
    [ObservableProperty]
    private bool _isTab0Selected = true; // Dashboard

    [ObservableProperty]
    private bool _isTab1Selected; // User

    [ObservableProperty]
    private bool _isTab2Selected; // Basic Control

    [ObservableProperty]
    private bool _isTab3Selected; // Data View

    [ObservableProperty]
    private bool _isTab4Selected; // History

    [ObservableProperty]
    private bool _isTab5Selected; // Log

    [ObservableProperty]
    private bool _isTab6Selected; // Application

    [ObservableProperty]
    private bool _isTab7Selected; // Layout

    [ObservableProperty]
    private bool _isTab8Selected; // Preference

    public MainWindowViewModel(IAcsApiService apiService)
    {
        _apiService = apiService;
        _mapViewModel = new MapViewModel();
        _vehicleListViewModel = new VehicleListViewModel();
        _dashboardViewModel = new DashboardViewModel();
        _summaryViewModel = new SummaryViewModel();
        _dataViewViewModel = new DataViewViewModel();
        _logViewModel = new LogViewModel(_apiService);
        _applicationViewModel = new ApplicationViewModel();
        _appManagementViewModel = new AppManagementViewModel(_apiService);
        _mqttViewModel = new MqttViewModel(_apiService);
        _hostCommunicationViewModel = new HostCommunicationViewModel(_apiService);
        _nodeViewModel = new NodeViewModel(_apiService) { MapViewModel = _mapViewModel };
        _stationViewModel = new StationViewModel(_apiService);
        _linkViewModel = new LinkViewModel(_apiService) { MapViewModel = _mapViewModel };
        _bayViewModel = new BayViewModel(_apiService);
        _zoneViewModel = new ZoneViewModel(_apiService);
        _portViewModel = new PortViewModel(_apiService);
        _linkZoneViewModel = new LinkZoneViewModel(_apiService);
        _transferCommandViewModel = new TransferCommandViewModel(_apiService);
        _vehicleViewModel = new VehicleViewModel(_apiService);
        _transportCmdHistoryViewModel = new TransportCmdHistoryViewModel(_apiService);
        _vehicleHistoryViewModel = new VehicleHistoryViewModel(_apiService);

        // 메뉴 선택 시 팝업 윈도우 열기 연결
        _applicationViewModel.OnViewChangeRequested = OpenPopupView;
        _dataViewViewModel.OnViewChangeRequested = OpenPopupView;
    }

    /// <summary>
    /// 팝업 윈도우 열기 (non-modal — UI thread 차단 없음)
    /// </summary>
    private void OpenPopupView(string viewName)
    {
        // 이미 열린 창이 있으면 재사용 (숨겨진 경우 다시 표시) — 동일 제목 중복 창 생성 방지
        if (_popupWindows.TryGetValue(viewName, out var existing))
        {
            if (!existing.IsVisible)
                existing.Show();
            existing.Activate();
            return;
        }

        var (title, content) = viewName switch
        {
            "AppManagement" => ("Application Management", (Control)new AppManagementView { DataContext = AppManagementViewModel }),
            "Mqtt" => ("MQTT", (Control)new MqttView { DataContext = MqttViewModel }),
            "HostCommunication" => ("Host Communication - TCP", (Control)new HostCommunicationView { DataContext = HostCommunicationViewModel }),
            "Log" => ("Log Viewer", (Control)new LogView { DataContext = LogViewModel }),
            "Node" => ("Node", (Control)new NodeView { DataContext = NodeViewModel }),
            "Station" => ("Station", (Control)new StationView { DataContext = StationViewModel }),
            "Link" => ("Link", (Control)new LinkView { DataContext = LinkViewModel }),
            "Bay" => ("Bay", (Control)new BayView { DataContext = BayViewModel }),
            "Zone" => ("Zone", (Control)new ZoneView { DataContext = ZoneViewModel }),
            "Port" => ("Port", (Control)new PortView { DataContext = PortViewModel }),
            "LinkZone" => ("LinkZone", (Control)new LinkZoneView { DataContext = LinkZoneViewModel }),
            "TransferCommand" => ("Transfer Command", (Control)new TransferCommandView { DataContext = TransferCommandViewModel }),
            "Vehicle" => ("Vehicle", (Control)new VehicleView { DataContext = VehicleViewModel }),
            "TransportCmdHistory" => ("TrCmd History", (Control)new TransportCmdHistoryView { DataContext = TransportCmdHistoryViewModel }),
            "VehicleHistory" => ("Vehicle History", (Control)new VehicleHistoryView { DataContext = VehicleHistoryViewModel }),
            _ => ((string)null, (Control)null)
        };
        if (content == null) return;

        var window = new Window
        {
            Title = title,
            Content = content,
            Width = 900,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        window.Closed += (_, _) =>
        {
            _popupWindows.Remove(viewName);
            if (viewName == "AppManagement")
                AppManagementViewModel.AutoRefreshEnabled = false;   // 창 닫으면 폴링 중지
        };
        _popupWindows[viewName] = window;
        window.Show();

        // 뷰별 초기 데이터 로드
        if (viewName == "AppManagement")
            AppManagementViewModel.AutoRefreshEnabled = true;   // 기본 ON: 즉시 갱신 + 5초 폴링
        if (viewName == "Node")
            _ = NodeViewModel.LoadNodesAsync();
        if (viewName == "Station")
            _ = StationViewModel.LoadStationsAsync();
        if (viewName == "Link")
            _ = LinkViewModel.LoadLinksAsync();
        if (viewName == "Bay")
            _ = BayViewModel.LoadBaysAsync();
        if (viewName == "Mqtt")
            _ = MqttViewModel.LoadMqttConfigsAsync();
        if (viewName == "Zone")
            _ = ZoneViewModel.LoadZonesAsync();
        if (viewName == "Port")
            _ = PortViewModel.LoadLocationsAsync();
        if (viewName == "LinkZone")
            _ = LinkZoneViewModel.LoadLinkZonesAsync();
        if (viewName == "TransferCommand")
            _ = TransferCommandViewModel.LoadTransferCommandsAsync();
        if (viewName == "Vehicle")
            _ = VehicleViewModel.LoadVehiclesAsync();
        if (viewName == "Log")
            _ = LogViewModel.SearchCommand.ExecuteAsync(null);   // 창 오픈 시 기본 범위 1회 조회
        if (viewName == "TransportCmdHistory")
            _ = TransportCmdHistoryViewModel.SearchCommand.ExecuteAsync(null);
        if (viewName == "VehicleHistory")
            _ = VehicleHistoryViewModel.SearchCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// 업데이트 다운로드 완료 알림 — 상태바 배너 표시.
    /// 업데이트는 종료 시 자동 적용되며, "지금 재시작" 클릭 시 즉시 적용된다.
    /// </summary>
    public void NotifyUpdateReady(string version, Action applyAndRestart)
    {
        _applyUpdateAndRestart = applyAndRestart;
        UpdateReadyVersion = version;
    }

    /// <summary>상태바 "지금 재시작" 버튼 → 업데이트 즉시 적용 후 재시작.</summary>
    [RelayCommand]
    private void RestartForUpdate() => _applyUpdateAndRestart?.Invoke();

    /// <summary>Log 리본 버튼 → Log Viewer 팝업 열기.</summary>
    [RelayCommand]
    private void OpenLog() => OpenPopupView("Log");

    /// <summary>History 리본 버튼 → TrCmd History 팝업 열기.</summary>
    [RelayCommand]
    private void OpenTransportCmdHistory() => OpenPopupView("TransportCmdHistory");

    /// <summary>History 리본 버튼 → Vehicle History 팝업 열기.</summary>
    [RelayCommand]
    private void OpenVehicleHistory() => OpenPopupView("VehicleHistory");

    public async Task LoadInitialDataAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var nodes = await _apiService.GetNodesAsync();
            var links = await _apiService.GetLinksAsync();
            var vehicles = await _apiService.GetVehiclesAsync();
            var commands = await _apiService.GetTransportCommandsAsync();
            var stations = await _apiService.GetStationsAsync();
            var locations = await _apiService.GetLocationsAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MapViewModel.UpdateNodes(nodes);
                MapViewModel.UpdateLinks(links);
                MapViewModel.UpdateVehicles(vehicles);
                MapViewModel.UpdateStations(stations);
                MapViewModel.UpdateLocations(locations);
                DashboardViewModel.UpdateFromLinks(links);
                DashboardViewModel.UpdateFromVehicles(vehicles);
                DashboardViewModel.UpdateFromCommands(commands);
                SummaryViewModel.UpdateFromLinks(links);
                SummaryViewModel.UpdateFromVehicles(vehicles);
                SummaryViewModel.UpdateFromCommands(commands);
                VehicleListViewModel.UpdateVehicles(vehicles);
                SummaryViewModel.UpdateConnectionState("Connected");
                LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                ConnectionStatus = "Connected";
            });
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Error: " + ex.Message;
            SummaryViewModel.UpdateConnectionState("Error");
        }
    }
}
