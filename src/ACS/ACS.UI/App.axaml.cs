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

            desktop.Exit += async (_, _) =>
            {
                if (_vehicleHub != null)
                {
                    try { await _vehicleHub.StopAsync(); } catch { }
                    await _vehicleHub.DisposeAsync();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
