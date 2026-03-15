using System.Collections.ObjectModel;
using System.Threading;
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
    private CancellationTokenSource _cts;
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
    private ApplicationViewModel _applicationViewModel;

    [ObservableProperty]
    private AppManagementViewModel _appManagementViewModel;

    [ObservableProperty]
    private NioViewModel _nioViewModel;

    [ObservableProperty]
    private HostCommunicationViewModel _hostCommunicationViewModel;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _lastUpdateTime = "-";

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
        _applicationViewModel = new ApplicationViewModel();
        _appManagementViewModel = new AppManagementViewModel();
        _nioViewModel = new NioViewModel();
        _hostCommunicationViewModel = new HostCommunicationViewModel(_apiService);

        // 메뉴 선택 시 팝업 윈도우 열기 연결
        _applicationViewModel.OnViewChangeRequested = OpenPopupView;
        _dataViewViewModel.OnViewChangeRequested = OpenPopupView;
    }

    /// <summary>
    /// 팝업 윈도우 열기 (non-modal — UI thread 차단 없음)
    /// </summary>
    private void OpenPopupView(string viewName)
    {
        // 이미 열린 창이 있으면 활성화
        if (_popupWindows.TryGetValue(viewName, out var existing) && existing.IsVisible)
        {
            existing.Activate();
            return;
        }

        var (title, content) = viewName switch
        {
            "AppManagement" => ("Application Management", (Control)new AppManagementView { DataContext = AppManagementViewModel }),
            "Nio" => ("NIO", (Control)new NioView { DataContext = NioViewModel }),
            "HostCommunication" => ("Host Communication - TCP", (Control)new HostCommunicationView { DataContext = HostCommunicationViewModel }),
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
        window.Closed += (_, _) => _popupWindows.Remove(viewName);
        _popupWindows[viewName] = window;
        window.Show();
    }

    public async Task StartPollingAsync()
    {
        _cts = new CancellationTokenSource();

        // Load static data (nodes, links) once
        await LoadStaticDataAsync();

        // Start periodic polling for dynamic data
        _ = PollDynamicDataAsync(_cts.Token);
    }

    public void StopPolling()
    {
        _cts?.Cancel();
    }

    private async Task LoadStaticDataAsync()
    {
        try
        {
            var nodes = await _apiService.GetNodesAsync();
            var links = await _apiService.GetLinksAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MapViewModel.UpdateNodes(nodes);
                MapViewModel.UpdateLinks(links);
                DashboardViewModel.UpdateFromLinks(links);
                SummaryViewModel.UpdateFromLinks(links);
            });

            ConnectionStatus = "Connected";
            SummaryViewModel.UpdateConnectionState("Connected");
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Error: " + ex.Message;
            SummaryViewModel.UpdateConnectionState("Error");
        }
    }

    private async Task PollDynamicDataAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);

                var vehicles = await _apiService.GetVehiclesAsync();
                var commands = await _apiService.GetTransportCommandsAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VehicleListViewModel.UpdateVehicles(vehicles);
                    MapViewModel.UpdateVehicles(vehicles);
                    DashboardViewModel.UpdateFromVehicles(vehicles);
                    DashboardViewModel.UpdateFromCommands(commands);
                    SummaryViewModel.UpdateFromVehicles(vehicles);
                    SummaryViewModel.UpdateFromCommands(commands);
                    SummaryViewModel.UpdateConnectionState("Connected");
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                    ConnectionStatus = "Connected";
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ConnectionStatus = "Error: " + ex.Message;
                    SummaryViewModel.UpdateConnectionState("Error");
                });
            }
        }
    }
}
