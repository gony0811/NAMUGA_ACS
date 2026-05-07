using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ACS.UI.Services;
using ACS.UI.ViewModels;
using ACS.UI.Views;

namespace ACS.UI;

public partial class App : Application
{
    private const string BackendBaseUrl = "http://192.168.0.6:5100";

    private VehicleHubClient _vehicleHub;
    private HostCommHubClient _hostCommHub;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var apiService = new AcsApiService(BackendBaseUrl);
            var mainViewModel = new MainWindowViewModel(apiService);
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // SignalR VehicleHub: 차량 POSE 텔레메트리(1Hz) → MapViewModel 실시간 갱신
            _vehicleHub = new VehicleHubClient(BackendBaseUrl);
            _vehicleHub.PoseUpdated += pose =>
            {
                // [TEMP DEBUG] SignalR PoseUpdate 수신 로그
                Console.WriteLine($"[PoseUpdate] vid={pose.VehicleId} commId={pose.CommId} x={pose.X:F3} y={pose.Y:F3} angle={pose.Angle:F3} t={pose.EventTime:HH:mm:ss.fff}");

                Dispatcher.UIThread.Post(() =>
                {
                    mainViewModel.MapViewModel.ApplyPoseUpdate(pose.VehicleId, pose.CommId, pose.X, pose.Y, pose.Angle);
                });
            };
            _ = _vehicleHub.StartAsync();

            // SignalR HostCommHub: Host(MES) TCP 통신 로그 → HostCommunicationViewModel 실시간 갱신
            _hostCommHub = new HostCommHubClient(BackendBaseUrl);
            _hostCommHub.LogReceived += log =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var vm = mainViewModel.HostCommunicationViewModel;
                    string direction = log.Direction == "Send"
                        ? (log.Success == false ? "Error" : "Send")
                        : "Receive";
                    string message = string.IsNullOrEmpty(log.Error)
                        ? $"{log.MessageName} ({log.Length} bytes)"
                        : $"{log.MessageName}: {log.Error}";
                    vm.AddLog(direction, message, log.RemoteEndPoint);

                    // 송신은 SenderState, 수신은 ListenState를 Connected로 갱신.
                    if (log.Direction == "Receive") vm.UpdateListenState(true);
                    else if (log.Direction == "Send" && log.Success != false) vm.UpdateSenderState(true);
                });
            };
            _hostCommHub.ConnectionChanged += conn =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var vm = mainViewModel.HostCommunicationViewModel;
                    vm.AddLog(conn.Connected ? "Connect" : "Disconnect",
                              conn.Connected ? "Host connected" : "Host disconnected",
                              conn.RemoteEndPoint);
                    vm.UpdateListenState(conn.Connected);
                });
            };
            _ = _hostCommHub.StartAsync();

            desktop.Exit += async (_, _) =>
            {
                if (_vehicleHub != null)
                {
                    try { await _vehicleHub.StopAsync(); } catch { }
                    await _vehicleHub.DisposeAsync();
                }
                if (_hostCommHub != null)
                {
                    try { await _hostCommHub.StopAsync(); } catch { }
                    await _hostCommHub.DisposeAsync();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
