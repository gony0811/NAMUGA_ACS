# ACS 운영 DB 덤프 → 로컬 acs-postgres-db 복원 스크립트
#
# dump-from-prod.ps1 가 만든 스키마+데이터 덤프(.sql)를 로컬 컨테이너에 적용한다.
# 로컬은 docker-entrypoint(01_init_acsdb.sql)가 이미 스키마+DEMO 데이터를 만들어 둔 상태이므로,
# 덤프와 충돌하지 않게 public 스키마를 통째로 비우고 복원한다.
#
# 사용법:
#   pwsh docker/scripts/restore-prod-dump.ps1 -InputFile .\docker\backups\acs-prod-20260604-120000.sql
#   pwsh docker/scripts/restore-prod-dump.ps1 -InputFile .\dump.sql -Force
#
# 시나리오:
#   1) pwsh docker/scripts/dump-from-prod.ps1     # 운영 → .sql 생성
#   2) pwsh docker/scripts/restore-prod-dump.ps1 -InputFile <위 결과>
#
# 주의: 02_migrate_node_id.sql / 03_migrate_mqtt_id.sql 는 컨테이너 볼륨 초기화 시점에만
#       실행되므로 본 복원으로 재실행되지 않음. 운영 DB 가 이미 마이그레이션 적용 후 상태라면 문제 없음.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputFile,
    [string]$Container = 'acs-postgres-db',
    [string]$Database  = 'acsdb',
    [string]$User      = 'postgres',
    [string]$Password  = '1234',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# 1) 입력 파일 검증
if (-not (Test-Path $InputFile)) {
    Write-Error "덤프 파일을 찾을 수 없습니다: $InputFile"
    exit 1
}
$InputFile = (Resolve-Path $InputFile).Path
$fi = Get-Item $InputFile
if ($fi.Length -eq 0) {
    Write-Error "덤프 파일이 비어 있습니다: $InputFile"
    exit 1
}

# 2) 컨테이너 가동 여부
$running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
if (-not $running) {
    Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker-compose up -d' 먼저 실행하세요."
    exit 1
}

# psql 한 줄 쿼리 실행 — restore-master.ps1 의 Invoke-Psql 헬퍼와 동일.
# Windows PowerShell 5.1 의 docker.exe argv 인용 버그 회피용으로 SQL 을 임시파일로 컨테이너에 복사.
$script:remoteQueryPath = '/tmp/acs-restore-prod-query.sql'
function Invoke-Psql {
    param([string]$Sql)
    $localSql = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText($localSql, $Sql + "`n", (New-Object System.Text.UTF8Encoding($false)))
        & docker cp $localSql "$Container`:$script:remoteQueryPath" 2>$null | Out-Null
        $output = & docker exec -e "PGPASSWORD=$Password" $Container `
            psql -U $User -d $Database -tA -v 'ON_ERROR_STOP=1' -f $script:remoteQueryPath
        return ($output -join "`n")
    }
    finally {
        Remove-Item -Force $localSql -ErrorAction SilentlyContinue
        & docker exec $Container rm -f $script:remoteQueryPath 2>$null | Out-Null
    }
}

Write-Host "Container : $Container" -ForegroundColor Cyan
Write-Host "Database  : $Database"
Write-Host ("Input     : {0} ({1:N0} bytes)" -f $InputFile, $fi.Length)
Write-Host ''

# 3) 사용자 확인 — public 스키마를 통째로 날리는 파괴적 작업
Write-Host '경고: 로컬 acsdb 의 public 스키마를 모두 삭제하고 덤프로 교체합니다.' -ForegroundColor Yellow
Write-Host '      기존 로컬 데이터(DEMO 포함)는 모두 사라집니다.' -ForegroundColor Yellow
if (-not $Force) {
    $resp = Read-Host '계속하려면 "yes" 입력'
    if ($resp -ne 'yes') {
        Write-Host '중단됨.' -ForegroundColor Red
        exit 1
    }
}

# 4) public 스키마 비우기 — 덤프(CREATE TABLE 포함) 와 충돌하지 않도록.
Write-Host 'public 스키마 초기화 중...' -ForegroundColor Cyan
$null = Invoke-Psql @"
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO $User;
GRANT ALL ON SCHEMA public TO public;
"@

# 5) 덤프 파일을 컨테이너로 전송 후 psql -f 로 단일 트랜잭션 적용
$remotePath = '/tmp/acs-prod-restore.sql'
Write-Host "덤프 파일 컨테이너로 전송 중..." -ForegroundColor Cyan
& docker cp $InputFile "$Container`:$remotePath"
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker cp 실패 (exit=$LASTEXITCODE)"
    exit 1
}

Write-Host "psql 로 복원 중... (ON_ERROR_STOP=1, 단일 트랜잭션)" -ForegroundColor Cyan
& docker exec -e "PGPASSWORD=$Password" $Container `
    psql -U $User -d $Database -v 'ON_ERROR_STOP=1' -1 -f $remotePath
$psqlExit = $LASTEXITCODE

# 6) 컨테이너 임시파일 삭제
& docker exec $Container rm -f $remotePath | Out-Null

if ($psqlExit -ne 0) {
    Write-Error "psql 실패 (exit=$psqlExit). 트랜잭션이 롤백되었습니다."
    exit $psqlExit
}

# 7) Sanity check — 주요 테이블 row 수
Write-Host ''
Write-Host '복원 완료 — 주요 테이블 row 수:' -ForegroundColor Green
$checkTables = @(
    'NA_R_NODE', 'NA_R_LINK', 'NA_R_STATION', 'NA_R_VEHICLE',
    'NA_T_INTERSECTION', 'NA_T_TRANSPORTCMD', 'NA_T_CURRENTINTERSECTION',
    'NA_Q_TRANSPORTCMDREQUEST',
    'NA_U_COMMAND', 'NA_U_INFORM', 'NA_U_TRANSPORT',
    'NA_A_ALARM', 'NA_A_ALARMSPEC',
    'NA_M_CARRIER',
    'NA_C_MQTT', 'NA_C_NIO',
    'NA_X_APPLICATION', 'NA_X_APPLICATION_MANAGER', 'NA_X_OPTION'
)
foreach ($t in $checkTables) {
    $exists = (Invoke-Psql "SELECT to_regclass('public.""$t""') IS NOT NULL").Trim()
    if ($exists -ne 't') {
        Write-Host ("  {0,-30} (missing)" -f $t) -ForegroundColor DarkGray
        continue
    }
    $cnt = (Invoke-Psql "SELECT COUNT(*) FROM public.""$t""").Trim()
    Write-Host ("  {0,-30} {1,8}" -f $t, $cnt)
}

# 제외 테이블 — 존재하지 않아야 정상
Write-Host ''
Write-Host '제외 대상(NA_H_* / NA_L_*) — 존재 여부:' -ForegroundColor Cyan
$excludedTables = @(
    'NA_H_TRANSPORTCMDHISTORY', 'NA_H_VEHICLEHISTORY', 'NA_H_VEHICLESEARCHPATH',
    'NA_H_VEHICLE_BATTERYHISTORY', 'NA_H_HEARTBEATFAILHISTORY',
    'NA_H_ALARMRPTHISTORY', 'NA_H_ALARMTIMEHISTORY', 'NA_H_NIOHISTORY',
    'NA_H_MISSMATCHANDFLYHISTORY', 'NA_H_CROSSWAIT_HISTORY',
    'NA_L_LOGMESSAGE', 'NA_L_LARGELOGMESSAGE'
)
foreach ($t in $excludedTables) {
    $exists = (Invoke-Psql "SELECT to_regclass('public.""$t""') IS NOT NULL").Trim()
    $mark = if ($exists -eq 't') { '존재(예상 외)' } else { '없음 (정상)' }
    Write-Host ("  {0,-30} {1}" -f $t, $mark)
}

exit 0
