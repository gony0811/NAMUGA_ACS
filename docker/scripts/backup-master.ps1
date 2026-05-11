# ACS 마스터 데이터 백업 스크립트
#
# Docker 컨테이너(acs-postgres-db) 안에서 pg_dump 를 실행해
# 운영자가 구성한 마스터/설정 테이블만 INSERT 문 형태로 추출한다.
# 결과 파일은 docker/init/01_init_acsdb.sql 로 생성된 스키마 위에
# psql 로 그대로 적용할 수 있다 (restore-master.ps1 참고).
#
# 사용법:
#   pwsh docker/scripts/backup-master.ps1
#   pwsh docker/scripts/backup-master.ps1 -OutputDir D:\backup
#   pwsh docker/scripts/backup-master.ps1 -IncludeApplication
#   pwsh docker/scripts/backup-master.ps1 -DryRun
#
# 사전조건: 컨테이너가 기동 중이어야 함 (docker ps 로 확인).

[CmdletBinding()]
param(
    [string]$Container = 'acs-postgres-db',
    [string]$Database  = 'acsdb',
    [string]$User      = 'postgres',
    [string]$Password  = '1234',
    [string]$OutputDir = "$PSScriptRoot\..\backups",
    [switch]$IncludeApplication,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# 1) 백업 대상 마스터 테이블 (운영/이력 테이블은 제외)
$masterTables = @(
    # Path / Layout
    'NA_R_NODE', 'NA_R_LINK', 'NA_R_LINK_ZONE',
    'NA_R_STATION', 'NA_R_LOCATION', 'NA_R_BAY', 'NA_R_ZONE',
    # Intersection 정의
    'NA_T_INTERSECTION', 'NA_R_ORDER_PAIR',
    # Vehicle 마스터
    'NA_R_VEHICLE',
    # 자재 / 알람 정의
    'NA_M_CARRIER', 'NA_A_ALARMSPEC',
    # 통신 설정
    'NA_C_MQTT',
    # 사이트 / 옵션
    'NA_R_SPECIALCONFIG',
    'NA_X_OPTION', 'NA_X_APPLICATION_MANAGER'
)

if ($IncludeApplication) {
    # NA_X_APPLICATION 은 기본적으로 ApplicationInitializer 가 런타임에 만들어 PK 충돌 위험.
    # 사이트 이관 등 명시적으로 필요할 때만 포함.
    $masterTables += 'NA_X_APPLICATION'
}

# 2) 컨테이너 가동 여부 확인
$running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
if (-not $running) {
    Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker-compose up -d' 먼저 실행하세요."
    exit 1
}

# 3) 출력 폴더 준비 (DryRun 이어도 경로 표시용으로 정규화)
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
if (-not (Test-Path $OutputDir)) {
    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputFile = Join-Path $OutputDir "acs-master-$timestamp.sql"

# 4) bash 스크립트 합성 — Windows PowerShell 5.1 의 native arg 인용 버그 회피
#    pg_dump -t 'public."NA_R_NODE"' 같은 큰따옴표가 PS argv 를 거치며 깨지는 문제가 있어,
#    호스트에서 .sh 파일을 만들어 컨테이너로 복사한 뒤 실행한다.
#    스크립트 내부에서는 bash single-quote 로 안전하게 식별자 wrap.
#    --data-only       스키마 제외, INSERT 만
#    --column-inserts  컬럼명 명시 INSERT (스키마 진화에 강함)
#    --disable-triggers 외래키 제약 비활성화 후 일괄 적재
#    --no-owner / --no-privileges  OWNER/GRANT 문 제거 (DB 간 이식성)
$remoteDump   = '/tmp/acs-master-backup.sql'
$remoteScript = '/tmp/acs-master-backup.sh'
$lf = "`n"

$dumpCmd = @(
    "PGPASSWORD='$Password' pg_dump -U '$User' -d '$Database' \",
    "  --data-only --column-inserts --disable-triggers --no-owner --no-privileges \",
    "  -f '$remoteDump' \"
)
$tableLines = $masterTables | ForEach-Object { "  -t 'public.""$_""' \" }
# 마지막 줄의 trailing backslash 제거
if ($tableLines.Count -gt 0) {
    $tableLines[-1] = $tableLines[-1].TrimEnd(' \')
}
$scriptBody = '#!/bin/bash' + $lf + 'set -e' + $lf + ($dumpCmd -join $lf) + $lf + ($tableLines -join $lf) + $lf

Write-Host "Container : $Container" -ForegroundColor Cyan
Write-Host "Database  : $Database"
Write-Host "Tables    : $($masterTables.Count) 개"
Write-Host "Output    : $outputFile"
Write-Host ""

if ($DryRun) {
    Write-Host '[DryRun] 컨테이너에서 실행될 bash 스크립트:' -ForegroundColor Yellow
    $scriptBody.Split($lf) | ForEach-Object { Write-Host "  $_" }
    Write-Host ''
    Write-Host '[DryRun] PowerShell 측 명령 순서:'
    Write-Host "  (스크립트를 임시파일로 저장 후) docker cp <tmp.sh> $Container`:$remoteScript"
    Write-Host "  docker exec $Container bash $remoteScript"
    Write-Host "  docker cp $Container`:$remoteDump $outputFile"
    Write-Host "  docker exec $Container rm -f $remoteScript $remoteDump"
    exit 0
}

# 5) 호스트 임시파일 → 컨테이너 전송 → bash 실행
$localScript = [System.IO.Path]::GetTempFileName() + '.sh'
# LF 줄바꿈, BOM 없는 ASCII 로 저장 (bash 호환)
[System.IO.File]::WriteAllText($localScript, $scriptBody, (New-Object System.Text.UTF8Encoding($false)))

Write-Host 'pg_dump 스크립트 전송 중...' -ForegroundColor Cyan
& docker cp $localScript "$Container`:$remoteScript"
if ($LASTEXITCODE -ne 0) {
    Remove-Item -Force $localScript -ErrorAction SilentlyContinue
    Write-Error "docker cp (script) 실패 (exit=$LASTEXITCODE)"
    exit 1
}
Remove-Item -Force $localScript -ErrorAction SilentlyContinue

Write-Host 'pg_dump 실행 중...' -ForegroundColor Cyan
& docker exec $Container bash $remoteScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "pg_dump 실패 (exit=$LASTEXITCODE)"
    exit 1
}

# 6) 호스트로 파일 회수 후 컨테이너 임시파일 삭제
& docker cp "$Container`:$remoteDump" $outputFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker cp (dump) 실패 (exit=$LASTEXITCODE)"
    exit 1
}
& docker exec $Container rm -f $remoteScript $remoteDump | Out-Null

# 7) 결과 확인
$fi = Get-Item $outputFile
$insertCount = (Select-String -Path $outputFile -Pattern '^INSERT INTO' -SimpleMatch:$false).Count

Write-Host ''
Write-Host "백업 완료" -ForegroundColor Green
Write-Host ("  파일    : {0}" -f $outputFile)
Write-Host ("  크기    : {0:N0} bytes" -f $fi.Length)
Write-Host ("  INSERT  : {0} 줄" -f $insertCount)

if ($insertCount -eq 0) {
    Write-Warning '백업 파일에 INSERT 문이 없습니다. 마스터 테이블이 비어 있거나 권한 문제일 수 있습니다.'
    exit 2
}

exit 0
