using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using ACS.UI.Services;
using ACS.UI.ViewModels;
using ACS.UI.Views;

namespace ACS.UI;

public partial class App : Application
{
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
            // 패키지 기본값 + 사이트 오버라이드(ProgramData) 순으로 로드.
            // Velopack 업데이트는 설치 폴더(current\)를 통째로 교체하므로
            // 사이트별 Backend.Host는 C:\ProgramData\ACS.UI\appsettings.json에 두어 업데이트에도 보존한다.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile(System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                    "ACS.UI", "appsettings.json"), optional: true, reloadOnChange: false)
                .Build();
            var backend = configuration.GetSection("Backend").Get<BackendSettings>() ?? new BackendSettings();
            var baseUrl = backend.BaseUrl;

            var apiService = new AcsApiService(baseUrl);
            var mainViewModel = new MainWindowViewModel(apiService);
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // Velopack 자동 업데이트: 시작 직후 + 4시간 주기로 CS 릴리스 피드 체크.
            // 다운로드 완료 시 종료-시-적용을 예약하고 상태바 배너로 알림 (모달 금지 — 운영 화면 차단 방지).
            // 서버 미접속/미설치(IDE 실행) 등 모든 실패는 무시 — 오프라인에서도 정상 기동 보장.
            var updateService = new UpdateService(baseUrl);
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var updateInfo = await updateService.CheckAndDownloadAsync();
                        if (updateInfo != null)
                        {
                            updateService.ApplyOnExit(updateInfo);   // 종료 시 자동 적용 예약
                            Dispatcher.UIThread.Post(() =>
                                mainViewModel.NotifyUpdateReady(
                                    updateInfo.TargetFullRelease.Version.ToString(),
                                    () => updateService.ApplyAndRestart(updateInfo)));
                            return;   // 적용 예약 완료 — 추가 체크 불필요
                        }
                    }
                    catch { /* 서버 미접속 등 — 무시하고 다음 주기에 재시도 */ }
                    await Task.Delay(TimeSpan.FromHours(4));
                }
            });

            // SignalR VehicleHub: 차량 텔레메트리(POSE + 상태, 1Hz) → MapViewModel 실시간 갱신
            _vehicleHub = new VehicleHubClient(baseUrl);
            _vehicleHub.VehicleUpdated += dto =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    mainViewModel.MapViewModel.ApplyVehicleUpdate(dto);
                });
            };
            _ = _vehicleHub.StartAsync();

            // SignalR HostCommHub: Host(MES) TCP 통신 로그 → HostCommunicationViewModel 실시간 갱신
            _hostCommHub = new HostCommHubClient(baseUrl);
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
