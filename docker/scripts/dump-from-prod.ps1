# ACS 운영 DB(10.0.26.2 acsdb) → 개발 PC 덤프 스크립트
#
# 로컬 docker 컨테이너(acs-postgres-db) 안에서 pg_dump 를 실행해
# 원격 운영 DB 를 -h 옵션으로 가리켜 스키마+데이터를 한 번에 추출한다.
# 개발 PC 에 pg_dump 17 클라이언트를 별도 설치하지 않아도 되도록 컨테이너 안에서 실행.
#
# 이력(NA_H_*) / 로그(NA_L_*) 테이블 12개는 --exclude-table-and-children 로 제외.
# (파티션 부모를 제외하면 PG16+ 에서 자식 파티션도 자동 제외)
#
# 사용법:
#   pwsh docker/scripts/dump-from-prod.ps1
#   pwsh docker/scripts/dump-from-prod.ps1 -SourceHost 10.0.26.2 -SourcePassword 'xxx'
#   pwsh docker/scripts/dump-from-prod.ps1 -OutputDir D:\backup
#   pwsh docker/scripts/dump-from-prod.ps1 -DryRun
#
# 사전조건:
#   - 로컬 acs-postgres-db 컨테이너가 기동 중 (docker-compose up -d)
#   - 컨테이너에서 10.0.26.2:5432 로 TCP 접근 가능 (방화벽/pg_hba)

[CmdletBinding()]
param(
    [string]$SourceHost     = '10.0.26.2',
    [int]   $SourcePort     = 5432,
    [string]$SourceDb       = 'acsdb',
    [string]$SourceUser     = 'postgres',
    [string]$SourcePassword = '1234',
    [string]$Container      = 'acs-postgres-db',
    [string]$OutputDir      = "$PSScriptRoot\..\backups",
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# 1) 제외할 이력/로그 테이블 — backup-master.ps1 와 동일하게 명시 나열.
#    대문자 식별자 + 따옴표가 섞이면 와일드카드 패턴 매칭이 까다로워 enumerate.
$excludeTables = @(
    # 이력 (NA_H_*) — 10개
    'NA_H_TRANSPORTCMDHISTORY',
    'NA_H_VEHICLEHISTORY',
    'NA_H_VEHICLESEARCHPATH',
    'NA_H_VEHICLE_BATTERYHISTORY',
    'NA_H_HEARTBEATFAILHISTORY',
    'NA_H_ALARMRPTHISTORY',
    'NA_H_ALARMTIMEHISTORY',
    'NA_H_NIOHISTORY',
    'NA_H_MISSMATCHANDFLYHISTORY',
    'NA_H_CROSSWAIT_HISTORY',
    # 로그 (NA_L_*) — 2개 (파티션 부모)
    'NA_L_LOGMESSAGE',
    'NA_L_LARGELOGMESSAGE'
)

# 2) 컨테이너 가동 여부 확인 (DryRun 은 합성 결과만 보면 되므로 생략)
if (-not $DryRun) {
    $running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
    if (-not $running) {
        Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker-compose up -d' 먼저 실행하세요."
        exit 1
    }
}

# 3) 출력 폴더 준비
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
if (-not (Test-Path $OutputDir)) {
    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputFile = Join-Path $OutputDir "acs-prod-$timestamp.sql"

# 4) bash 스크립트 합성 — backup-master.ps1 와 동일 패턴.
#    Windows PowerShell 5.1 의 native arg 인용 버그(큰따옴표 누락) 회피 위해
#    호스트에서 .sh 파일을 만들어 컨테이너로 복사한 뒤 실행.
$remoteDump   = '/tmp/acs-prod-dump.sql'
$remoteScript = '/tmp/acs-prod-dump.sh'
$lf = "`n"

$dumpCmd = @(
    "PGPASSWORD='$SourcePassword' pg_dump \",
    "  -h '$SourceHost' -p $SourcePort -U '$SourceUser' -d '$SourceDb' \",
    "  --no-owner --no-privileges \"
)
$excludeLines = $excludeTables | ForEach-Object { "  --exclude-table-and-children='public.""$_""' \" }
$tailLine = "  -f '$remoteDump'"
$scriptBody = '#!/bin/bash' + $lf + 'set -e' + $lf + ($dumpCmd -join $lf) + $lf + ($excludeLines -join $lf) + $lf + $tailLine + $lf

Write-Host "Source    : $SourceHost`:$SourcePort / $SourceDb (user=$SourceUser)" -ForegroundColor Cyan
Write-Host "Container : $Container (pg_dump 실행 위치)"
Write-Host "Exclude   : $($excludeTables.Count) 개 (NA_H_* 이력 + NA_L_* 로그)"
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
# LF 줄바꿈, BOM 없는 UTF-8 로 저장 (bash 호환)
[System.IO.File]::WriteAllText($localScript, $scriptBody, (New-Object System.Text.UTF8Encoding($false)))

Write-Host 'pg_dump 스크립트 전송 중...' -ForegroundColor Cyan
& docker cp $localScript "$Container`:$remoteScript"
if ($LASTEXITCODE -ne 0) {
    Remove-Item -Force $localScript -ErrorAction SilentlyContinue
    Write-Error "docker cp (script) 실패 (exit=$LASTEXITCODE)"
    exit 1
}
Remove-Item -Force $localScript -ErrorAction SilentlyContinue

Write-Host "pg_dump 실행 중... (원격: $SourceHost`:$SourcePort)" -ForegroundColor Cyan
& docker exec $Container bash $remoteScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "pg_dump 실패 (exit=$LASTEXITCODE). 운영 DB 접근/방화벽/pg_hba 확인."
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
$createCount = (Select-String -Path $outputFile -Pattern '^CREATE TABLE' -SimpleMatch:$false).Count
$insertCount = (Select-String -Path $outputFile -Pattern '^(INSERT INTO|COPY )' -SimpleMatch:$false).Count
$leakedH     = (Select-String -Path $outputFile -Pattern '"NA_H_' -SimpleMatch:$false).Count
$leakedL     = (Select-String -Path $outputFile -Pattern '"NA_L_' -SimpleMatch:$false).Count

Write-Host ''
Write-Host "덤프 완료" -ForegroundColor Green
Write-Host ("  파일         : {0}" -f $outputFile)
Write-Host ("  크기         : {0:N0} bytes" -f $fi.Length)
Write-Host ("  CREATE TABLE : {0} 줄" -f $createCount)
Write-Host ("  INSERT/COPY  : {0} 줄" -f $insertCount)

if ($leakedH -gt 0 -or $leakedL -gt 0) {
    Write-Warning "제외 대상이 덤프에 포함된 흔적: NA_H_* 매치=$leakedH, NA_L_* 매치=$leakedL"
}
if ($createCount -eq 0) {
    Write-Warning '덤프에 CREATE TABLE 이 없습니다. 운영 DB 가 비어있거나 권한 문제 가능성.'
    exit 2
}

Write-Host ''
Write-Host "다음 단계: pwsh docker/scripts/restore-prod-dump.ps1 -InputFile `"$outputFile`"" -ForegroundColor Cyan
exit 0
