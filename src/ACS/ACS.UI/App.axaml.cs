using Avalonia;
using Avalonia.Controls;
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
    // 시작 시 1회 셋업되는 의존성들 — 로그아웃/재로그인 사이에도 동일 인스턴스를 재사용한다.
    private AcsApiService _apiService;
    private UserSession _userSession;
    private string _baseUrl;

    // 로그인 세션마다 새로 만들어 정리하는 백그라운드 자원들.
    private VehicleHubClient _vehicleHub;
    private HostCommHubClient _hostCommHub;
    private bool _exitHandlerRegistered;

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
            _baseUrl = backend.BaseUrl;

            // UserSession.Current 는 XAML x:Static 권한 게이트 바인딩의 소스 — 재할당 없이 상태만 갱신.
            _userSession = UserSession.Current;
            _apiService = new AcsApiService(_baseUrl, _userSession);

            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 새 LoginWindow를 띄워 desktop.MainWindow 로 지정. Closed 이벤트가 OnLoginClosed 를 호출.
    /// 최초 시작 + 로그아웃 후 재로그인 양쪽에서 사용된다.
    /// </summary>
    private void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loginWindow = new LoginWindow(_apiService, _userSession);
        desktop.MainWindow = loginWindow;
        loginWindow.Closed += async (_, _) =>
            await OnLoginClosed(desktop, loginWindow);
        loginWindow.Show();
    }

    private async System.Threading.Tasks.Task OnLoginClosed(
        IClassicDesktopStyleApplicationLifetime desktop,
        LoginWindow loginWindow)
    {
        if (!_userSession.IsAuthenticated)
        {
            desktop.Shutdown();
            return;
        }

        // 실제 MainWindow 구성 & 표시 (ChangePasswordWindow 의 owner 로 필요)
        var mainViewModel = new MainWindowViewModel(_apiService, _userSession);
        // 상태바 Logout 버튼 → 앱 종료가 아닌 로그인 창 복귀 흐름으로 분기
        mainViewModel.LogoutRequested = () => RestartLoginFlowAsync(desktop);

        var mainWindow = new MainWindow { DataContext = mainViewModel };
        desktop.MainWindow = mainWindow;
        mainWindow.Show();

        // 최초 로그인이거나 Admin이 비밀번호 리셋한 경우 — 변경 모달 강제
        if (loginWindow.Result?.MustChangePassword == true)
        {
            var changeWindow = new ChangePasswordWindow(_apiService, forced: true);
            var changed = await changeWindow.ShowDialog<bool?>(mainWindow);
            if (changed != true)
            {
                await _apiService.LogoutAsync();
                _userSession.Clear();
                desktop.Shutdown();
                return;
            }
        }

        StartBackgroundServices(mainViewModel);

        // desktop.Exit 핸들러는 앱 수명 1회만 등록 (재로그인 때 중복 등록 방지)
        if (!_exitHandlerRegistered)
        {
            _exitHandlerRegistered = true;
            desktop.Exit += async (_, _) => await StopBackgroundServicesAsync();
        }
    }

    /// <summary>
    /// 상태바 Logout → 백엔드 세션 종료 + UserSession.Clear 까지는 ViewModel이 끝낸 상태로 호출됨.
    /// 여기서는 SignalR Hub 들 정리 + 새 LoginWindow 표시 + 기존 MainWindow 닫기를 수행.
    /// 기존 MainWindow 를 닫기 전에 새 LoginWindow 를 desktop.MainWindow 로 먼저 교체해서
    /// OnLastWindowClose 가 발동하지 않도록 한다 (앱 종료 방지).
    /// </summary>
    private async System.Threading.Tasks.Task RestartLoginFlowAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        await StopBackgroundServicesAsync();

        var oldMain = desktop.MainWindow;
        ShowLoginWindow(desktop);
        oldMain?.Close();
    }

    private void StartBackgroundServices(MainWindowViewModel mainViewModel)
    {
        // Velopack 자동 업데이트: 시작 직후 + 4시간 주기로 CS 릴리스 피드 체크.
        // Viewer 권한은 업데이트 적용 불가 — 체크/다운로드 자체를 생략.
        // 서버 미접속/미설치(IDE 실행) 등 모든 실패는 무시 — 오프라인에서도 정상 기동 보장.
        var updateService = new UpdateService(_baseUrl);
        var session = _userSession;
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (session.CanUpdateUi)
                    {
                        var updateInfo = await updateService.CheckAndDownloadAsync();
                        if (updateInfo != null)
                        {
                            updateService.ApplyOnExit(updateInfo);
                            Dispatcher.UIThread.Post(() =>
                                mainViewModel.NotifyUpdateReady(
                                    updateInfo.TargetFullRelease.Version.ToString(),
                                    () => updateService.ApplyAndRestart(updateInfo)));
                            return;
                        }
                    }
                }
                catch { }
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromHours(4));
            }
        });

        // SignalR VehicleHub: 차량 텔레메트리(POSE + 상태, 1Hz) → MapViewModel 실시간 갱신
        _vehicleHub = new VehicleHubClient(_baseUrl);
        _vehicleHub.VehicleUpdated += dto =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainViewModel.MapViewModel.ApplyVehicleUpdate(dto);
            });
        };
        // 알람 SET/RESET 전이 → 맵 알람 강조 + hover 팝업 사유(errorCode/errorMessage) 갱신
        _vehicleHub.VehicleAlarmReceived += dto =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainViewModel.MapViewModel.ApplyVehicleAlarm(dto);
            });
        };
        _ = _vehicleHub.StartAsync();

        // SignalR HostCommHub: Host(MES) TCP 통신 로그 → HostCommunicationViewModel 실시간 갱신
        _hostCommHub = new HostCommHubClient(_baseUrl);
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
    }

    private async System.Threading.Tasks.Task StopBackgroundServicesAsync()
    {
        if (_vehicleHub != null)
        {
            try { await _vehicleHub.StopAsync(); } catch { }
            try { await _vehicleHub.DisposeAsync(); } catch { }
            _vehicleHub = null;
        }
        if (_hostCommHub != null)
        {
            try { await _hostCommHub.StopAsync(); } catch { }
            try { await _hostCommHub.DisposeAsync(); } catch { }
            _hostCommHub = null;
        }
    }
}
