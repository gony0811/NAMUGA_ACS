# ACS 사이트별 In-Repo 배포 스크립트
#
# 1) ACS.App 을 staging 폴더로 publish
# 2) 각 src/ACS/deploy/<SITE>/ 에 robocopy /MIR 로 미러
#    - appsettings.json 은 사이트별 설정 보존 (/XF)
#    - 이전 회차의 <SITE>.exe 도 보존 (/XF) — 직후 새 apphost 로 덮어쓰기
#    - logs/ 디렉토리 보존 (/XD)
# 3) ACS.App.exe → <SITE>.exe 로 rename (Task Manager 식별 + 사이트별 실행 진입점)
#
# 사용법:
#   pwsh src/ACS/publish-deploy.ps1
#   pwsh src/ACS/publish-deploy.ps1 -Sites TS01_P,ES01_P
#   pwsh src/ACS/publish-deploy.ps1 -SkipPublish
#
# 사전조건: .NET 8 SDK. PowerShell 5.1+.

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string[]]$Sites,
    [switch]$SkipPublish,
    [string]$Staging
)

$ErrorActionPreference = 'Stop'

$root      = $PSScriptRoot
$proj      = Join-Path $root 'ACS.App\ACS.App.csproj'
$deployDir = Join-Path $root 'deploy'
if (-not $Staging) { $Staging = Join-Path $root '.publish-staging' }

# deploy/ 는 git 미추적(PC별 실행 폴더) — 없으면 만들고, config-templates/ 에서
# 사이트별 appsettings.json 을 시딩한다. 이미 있는 파일은 절대 덮어쓰지 않는다.
if (-not (Test-Path $deployDir)) {
    New-Item -ItemType Directory -Force -Path $deployDir | Out-Null
}
$templateDir = Join-Path $root 'config-templates'
if (Test-Path $templateDir) {
    # 공통 설정 1부 시딩: deploy/appsettings.common.json (없을 때만 — 기존 파일 절대 미덮어쓰기)
    $commonTpl  = Join-Path $templateDir 'appsettings.common.json'
    $commonDst  = Join-Path $deployDir 'appsettings.common.json'
    if ((Test-Path $commonTpl) -and (-not (Test-Path $commonDst))) {
        Copy-Item $commonTpl $commonDst
        Write-Host "Seeded  : deploy/appsettings.common.json  (from config-templates — DB/브로커 값 확인 필요)" -ForegroundColor Yellow
    }
    foreach ($t in Get-ChildItem -Path $templateDir -Directory) {
        $siteDir  = Join-Path $deployDir $t.Name
        $siteJson = Join-Path $siteDir 'appsettings.json'
        $tplJson  = Join-Path $t.FullName 'appsettings.json'
        if ((Test-Path $tplJson) -and (-not (Test-Path $siteJson))) {
            New-Item -ItemType Directory -Force -Path $siteDir | Out-Null
            Copy-Item $tplJson $siteJson
            Write-Host "Seeded  : deploy/$($t.Name)/appsettings.json  (from config-templates — DB/호스트 값 확인 필요)" -ForegroundColor Yellow
        }
    }
}
if (-not (Test-Path $proj)) {
    Write-Error "Project not found: $proj"
    exit 1
}

if (-not $Sites -or $Sites.Count -eq 0) {
    $Sites = Get-ChildItem -Path $deployDir -Directory | Select-Object -ExpandProperty Name
}
if ($Sites.Count -eq 0) {
    Write-Error "No site folders under $deployDir"
    exit 1
}

Write-Host "Project   : $proj"
Write-Host "Staging   : $Staging"
Write-Host "Sites     : $($Sites -join ', ')"
Write-Host "Config    : $Configuration"
Write-Host ""

# 1) Publish
if (-not $SkipPublish) {
    Write-Host "[1/3] dotnet publish ..." -ForegroundColor Cyan
    & dotnet publish $proj -c $Configuration -o $Staging --no-self-contained
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed (exit=$LASTEXITCODE)"
        exit 1
    }
    Write-Host ""
} else {
    Write-Host "[1/3] Skipped publish (-SkipPublish)" -ForegroundColor Yellow
    Write-Host ""
}

if (-not (Test-Path $Staging)) {
    Write-Error "Staging not found: $Staging. Run once without -SkipPublish."
    exit 1
}

# 2) 사이트별 미러 + apphost rename
Write-Host "[2/3] Mirror staging -> deploy/<SITE>/ ..." -ForegroundColor Cyan
$failed = @()
foreach ($site in $Sites) {
    $dst = Join-Path $deployDir $site
    if (-not (Test-Path $dst)) {
        Write-Warning "Skip: $dst (folder missing)"
        $failed += $site
        continue
    }

    $siteExe = "$site.exe"
    Write-Host "  ==> $site" -ForegroundColor Cyan
    & robocopy $Staging $dst /MIR /XF appsettings.json $siteExe /XD logs /R:1 /W:1 /NJH /NJS /NDL /NP | Out-Host
    # robocopy: 0~7 정상, 8 이상 에러
    if ($LASTEXITCODE -ge 8) {
        Write-Warning "robocopy failed: $dst (exit=$LASTEXITCODE)"
        $failed += $site
        continue
    }

    # apphost rename: 새 ACS.App.exe 가 staging 에서 막 복사됨. <SITE>.exe 로 이동.
    $srcExe = Join-Path $dst 'ACS.App.exe'
    $dstExe = Join-Path $dst $siteExe
    if (Test-Path $srcExe) {
        if (Test-Path $dstExe) { Remove-Item -Force $dstExe }
        Move-Item -Force -Path $srcExe -Destination $dstExe
        Write-Host "      rename: ACS.App.exe -> $siteExe"
    } else {
        Write-Warning "      ACS.App.exe missing in staging — apphost rename skipped"
    }
}

# 3) 결과
Write-Host ""
Write-Host "[3/3] Summary" -ForegroundColor Cyan
if ($failed.Count -eq 0) {
    Write-Host "Deploy complete -- all $($Sites.Count) site(s) succeeded" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Deploy partial failure ($($failed.Count) site(s)):" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $_" }
    exit 1
}
