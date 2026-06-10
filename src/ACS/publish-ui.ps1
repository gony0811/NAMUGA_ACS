# ACS.UI Velopack 릴리스 패키징 스크립트
#
# 1) ACS.UI 를 self-contained(win-x64) 로 staging 폴더에 publish
#    - PublishSingleFile 금지 (Velopack 미지원 — loose files 필요)
# 2) vpk pack 으로 Velopack 릴리스 생성 (Setup.exe + .nupkg + releases.win.json)
#    - outputDir(src/ACS/releases/ui) 에 이전 릴리스가 남아 있으면 델타 패키지 자동 생성
#      → 이 폴더는 회차 간 보존할 것. 다른 PC 에서 빌드 시:
#        vpk download http --url http://<CS호스트>:5100/releases/ui --outputDir src/ACS/releases/ui
# 3) -ReleaseDir 지정 시 CS 서빙 경로(예: C:\acs\releases\ui)로 복사
#    - /MIR 절대 금지 — 델타/이전 릴리스 파일을 피드에 누적 유지해야 함
#
# 클라이언트 최초 설치: http://<CS호스트>:5100/releases/ui/AcsUi-win-Setup.exe 다운로드 후 실행
# 사이트별 Backend.Host 설정: C:\ProgramData\ACS.UI\appsettings.json (업데이트에도 보존됨)
#
# 사용법:
#   pwsh src/ACS/publish-ui.ps1 -Version 1.0.1
#   pwsh src/ACS/publish-ui.ps1 -Version 1.0.1 -ReleaseDir C:\acs\releases\ui
#   pwsh src/ACS/publish-ui.ps1 -Version 1.0.1 -SkipPublish   # staging 재사용, pack 만
#
# 사전조건: .NET 8 SDK, vpk CLI (dotnet tool install -g vpk). PowerShell 5.1+.
# 주의: 버전은 릴리스마다 반드시 증가해야 함 (SemVer, vpk 가 중복 버전 거부)

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version,
    [string]$Configuration = 'Release',
    [string]$ReleaseDir,
    [switch]$SkipPublish,
    [string]$Staging
)

$ErrorActionPreference = 'Stop'

$root   = $PSScriptRoot
$proj   = Join-Path $root 'ACS.UI\ACS.UI.csproj'
$outDir = Join-Path $root 'releases\ui'
if (-not $Staging) { $Staging = Join-Path $root '.publish-ui-staging' }

if (-not (Test-Path $proj)) {
    Write-Error "Project not found: $proj"
    exit 1
}
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Error "vpk CLI not found. Install: dotnet tool install -g vpk"
    exit 1
}

Write-Host "Project   : $proj"
Write-Host "Version   : $Version"
Write-Host "Staging   : $Staging"
Write-Host "OutputDir : $outDir"
Write-Host "ReleaseDir: $(if ($ReleaseDir) { $ReleaseDir } else { '(복사 생략)' })"
Write-Host "Config    : $Configuration"
Write-Host ""

# 1) Publish — self-contained win-x64 (클라이언트 PC 에 .NET 런타임 불필요)
if (-not $SkipPublish) {
    Write-Host "[1/3] dotnet publish ..." -ForegroundColor Cyan
    if (Test-Path $Staging) { Remove-Item -Recurse -Force $Staging }
    & dotnet publish $proj -c $Configuration -r win-x64 --self-contained -o $Staging -p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed (exit=$LASTEXITCODE)"
        exit 1
    }
    Write-Host ""
} else {
    Write-Host "[1/3] Skipped publish (-SkipPublish)" -ForegroundColor Yellow
    Write-Host ""
}

if (-not (Test-Path (Join-Path $Staging 'ACS.UI.exe'))) {
    Write-Error "ACS.UI.exe not found in staging: $Staging. Run once without -SkipPublish."
    exit 1
}

# 2) vpk pack — outputDir 에 이전 릴리스가 있으면 델타 자동 생성
Write-Host "[2/3] vpk pack ..." -ForegroundColor Cyan
& vpk pack --packId AcsUi --packVersion $Version --packDir $Staging `
           --mainExe ACS.UI.exe --packTitle 'ACS UI' --outputDir $outDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed (exit=$LASTEXITCODE)"
    exit 1
}
Write-Host ""

# 3) 릴리스 피드 복사 (누적 — /MIR 금지)
if ($ReleaseDir) {
    Write-Host "[3/3] Copy to release feed: $ReleaseDir ..." -ForegroundColor Cyan
    & robocopy $outDir $ReleaseDir /E /R:1 /W:1 /NJH /NJS /NDL /NP | Out-Host
    if ($LASTEXITCODE -ge 8) {
        Write-Error "robocopy failed (exit=$LASTEXITCODE)"
        exit 1
    }
    Write-Host "Release v$Version published to feed" -ForegroundColor Green
} else {
    Write-Host "[3/3] ReleaseDir 미지정 — 피드 복사 생략. 출력: $outDir" -ForegroundColor Yellow
}
exit 0
