# ACS 배포 스크립트
#
# bin/Debug/net8.0 의 빌드 결과를 C:\acs\{ds,es,ts,ts02,ui,host}\net8.0 으로 복사.
# 각 폴더의 appsettings.json 은 보존(덮어쓰지 않음).
# rename 한 apphost(예: ES01_P.exe)는 별개 파일이라 그대로 유지됨.
#
# 사용법:
#   pwsh src/ACS/deploy.ps1
#   pwsh src/ACS/deploy.ps1 -Source <경로> -Root C:\acs -Targets es,ts
#
# 사전조건: 'dotnet build src/ACS/ACS.sln' 먼저 실행.

[CmdletBinding()]
param(
    [string]$Source  = "$PSScriptRoot\ACS.App\bin\Debug\net8.0",
    [string]$Root    = "C:\acs",
    [string[]]$Targets = @('ds', 'es', 'ts', 'ts02', 'ui', 'host'),
    [switch]$NoPdb
)

$ErrorActionPreference = 'Stop'

# 1) 소스 폴더 존재 확인
if (-not (Test-Path $Source)) {
    Write-Error "Source not found: $Source. 먼저 'dotnet build' 실행하세요."
    exit 1
}
Write-Host "Source : $Source"
Write-Host "Root   : $Root"
Write-Host "Targets: $($Targets -join ', ')"
Write-Host ""

# 2) robocopy 옵션 구성
#    /E       : 서브폴더 포함(빈 폴더도)
#    /XF ...  : 이 파일은 복사 제외 (appsettings.json 은 환경별 설정 보존)
#    /XD ...  : 이 폴더는 복사 제외 (logs 는 각 프로세스 자체 생성)
#    /R:1 /W:1: 락걸린 파일 재시도 1회 1초 (빠르게 skip)
#    /NJH /NJS /NDL /NP : 출력 간소화
$excludedFiles = @('appsettings.json')
if ($NoPdb) { $excludedFiles += '*.pdb' }
$xf = @('/XF') + $excludedFiles
$xd = @('/XD', 'logs')

# 3) 각 target 처리
$failed = @()
foreach ($name in $Targets) {
    $dst = Join-Path $Root "$name\net8.0"

    if (-not (Test-Path $dst)) {
        Write-Warning "Skip: $dst (폴더 없음)"
        continue
    }

    Write-Host "==> $dst" -ForegroundColor Cyan
    & robocopy $Source $dst /E @xf @xd /R:1 /W:1 /NJH /NJS /NDL /NP | Out-Host

    # robocopy 종료 코드: 0~7 정상, 8 이상 에러
    if ($LASTEXITCODE -ge 8) {
        Write-Warning "robocopy 실패: $dst (exit=$LASTEXITCODE)"
        $failed += $dst
    }
}

# 4) 결과 리포트
Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host "Deploy 완료 — 모든 target 성공" -ForegroundColor Green
    # robocopy 의 정상 종료 코드(0~7)를 PowerShell 종료 코드 0 으로 정규화
    exit 0
} else {
    Write-Host "Deploy 실패 target ($($failed.Count)개):" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $_" }
    exit 1
}
