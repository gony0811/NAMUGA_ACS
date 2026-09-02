using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;

namespace ACS.App.Web
{
    /// <summary>
    /// CS 기동 시 ACS.UI Velopack 릴리스 피드가 비어 있으면 자동으로 채우는 부트스트랩.
    /// 1) 배포본 동봉 시드(releases-seed/ui) 복사 — 프로덕션 경로
    /// 2) 소스 트리의 기존 빌드 산출물(releases/ui) 복사 — 개발 경로 (재패키징 불필요)
    /// 3) 소스 트리 + vpk CLI 감지 시 publish-ui.ps1 백그라운드 실행 — 개발 경로 (산출물 없음)
    /// 어떤 실패도 CS 기동을 막지 않는다 (경고 로그만).
    /// </summary>
    public static class ReleaseFeedBootstrapper
    {
        private const string FeedManifest = "releases.win.json";

        public static void Bootstrap(string releasePath)
        {
            try
            {
                if (Directory.EnumerateFileSystemEntries(releasePath).Any())
                {
                    if (!File.Exists(Path.Combine(releasePath, FeedManifest)))
                        Log.Warning("[ReleaseFeed] 피드에 파일은 있으나 {Manifest} 가 없음 — 자동 구성 생략, 수동 확인 필요: {Path}",
                            FeedManifest, releasePath);
                    return;
                }

                var seedDir = Path.Combine(AppContext.BaseDirectory, "releases-seed", "ui");
                if (File.Exists(Path.Combine(seedDir, FeedManifest)))
                {
                    CopyToFeed(seedDir, releasePath, "배포본 시드");
                    return;
                }

                var scriptPath = FindPublishScript();
                if (scriptPath == null)
                {
                    Log.Warning("[ReleaseFeed] 피드가 비어 있으나 시드({SeedDir})도 소스 트리(publish-ui.ps1)도 없음 — 자동 구성 불가. " +
                                "publish-ui.ps1 수동 실행 필요", seedDir);
                    return;
                }

                // 소스 트리에 이미 패키징된 산출물이 있으면 그대로 복사 — 재패키징하면 vpk가
                // 기존 버전 이하라며 거부하므로(중복 버전) 빌드가 아닌 복사가 정답이다.
                var sourceReleaseDir = Path.Combine(Path.GetDirectoryName(scriptPath), "releases", "ui");
                if (File.Exists(Path.Combine(sourceReleaseDir, FeedManifest)))
                {
                    CopyToFeed(sourceReleaseDir, releasePath, "소스 트리 기존 산출물");
                    return;
                }

                if (!IsVpkAvailable())
                {
                    Log.Warning("[ReleaseFeed] vpk CLI 미설치 — 자동 빌드 불가. 설치: dotnet tool install -g vpk");
                    return;
                }

                var version = ReadUiVersion(scriptPath) ?? "1.0.0";
                Log.Information("[ReleaseFeed] 피드가 비어 있음 — ACS.UI v{Version} 릴리스를 백그라운드로 빌드/패키징 시작 " +
                                "(script: {Script}, feed: {Feed})", version, scriptPath, releasePath);
                _ = Task.Run(() => RunPublishScript(scriptPath, version, releasePath));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[ReleaseFeed] 피드 부트스트랩 실패 — CS 기동은 계속");
            }
        }

        private static void CopyToFeed(string sourceDir, string releasePath, string label)
        {
            int count = 0;
            foreach (var file in Directory.EnumerateFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(releasePath, Path.GetFileName(file)));
                count++;
            }
            Log.Information("[ReleaseFeed] {Label}에서 릴리스 피드 구성 완료 — {Count}개 파일 복사 ({Source} → {Feed})",
                label, count, sourceDir, releasePath);
        }

        /// <summary>BaseDirectory에서 상위로 올라가며 publish-ui.ps1 탐색 (개발/소스 실행 감지).</summary>
        private static string FindPublishScript()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "publish-ui.ps1");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        private static bool IsVpkAvailable()
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "vpk",
                    Arguments = "--help",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                proc.WaitForExit(10000);
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private static string ReadUiVersion(string scriptPath)
        {
            try
            {
                var csproj = Path.Combine(Path.GetDirectoryName(scriptPath), "ACS.UI", "ACS.UI.csproj");
                if (!File.Exists(csproj))
                    return null;
                return XDocument.Load(csproj).Descendants("Version").FirstOrDefault()?.Value;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[ReleaseFeed] ACS.UI.csproj 버전 파싱 실패 — 기본값 사용");
                return null;
            }
        }

        private static void RunPublishScript(string scriptPath, string version, string releasePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Version {version} -ReleaseDir \"{releasePath}\"",
                    WorkingDirectory = Path.GetDirectoryName(scriptPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log.Information("[ReleaseFeed] {Line}", e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log.Warning("[ReleaseFeed] {Line}", e.Data); };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                    Log.Information("[ReleaseFeed] ACS.UI v{Version} 릴리스 자동 구성 완료 — 피드: {Feed}", version, releasePath);
                else
                    Log.Warning("[ReleaseFeed] publish-ui.ps1 실패 (exit={Code}) — 피드는 비어 있는 상태로 유지", proc.ExitCode);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[ReleaseFeed] 릴리스 자동 빌드 중 예외");
            }
        }
    }
}
