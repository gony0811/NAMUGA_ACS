using Velopack;
using Velopack.Sources;

namespace ACS.UI.Services;

/// <summary>
/// Velopack 기반 클라이언트 자동 업데이트 서비스.
/// CS 웹 호스트가 정적 서빙하는 릴리스 피드(http://{Backend.Host}:{Backend.Port}/releases/ui)에서
/// 새 버전을 확인/다운로드하고, 종료 시 적용 또는 즉시 재시작 적용을 수행한다.
/// </summary>
public class UpdateService
{
    private readonly UpdateManager _manager;

    public UpdateService(string baseUrl)
    {
        _manager = new UpdateManager(new SimpleWebSource($"{baseUrl}/releases/ui"));
    }

    /// <summary>현재 설치된 버전. 미설치(IDE/bin 직접 실행) 상태면 "dev".</summary>
    public string CurrentVersion =>
        _manager.IsInstalled ? _manager.CurrentVersion?.ToString() ?? "unknown" : "dev";

    /// <summary>
    /// 업데이트 확인 후 있으면 다운로드까지 수행.
    /// 미설치 상태(개발 실행)면 no-op으로 null 반환. 서버 미접속 등 오류는 호출부에서 처리.
    /// </summary>
    public async Task<UpdateInfo> CheckAndDownloadAsync()
    {
        if (!_manager.IsInstalled) return null;

        var info = await _manager.CheckForUpdatesAsync();
        if (info == null) return null;

        await _manager.DownloadUpdatesAsync(info);
        return info;
    }

    /// <summary>다운로드된 업데이트를 즉시 적용하고 앱을 재시작한다.</summary>
    public void ApplyAndRestart(UpdateInfo info) => _manager.ApplyUpdatesAndRestart(info);

    /// <summary>앱 종료 시 업데이트가 자동 적용되도록 예약한다.</summary>
    public void ApplyOnExit(UpdateInfo info) => _manager.WaitExitThenApplyUpdates(info);
}
