# ACS 마스터 데이터 복원 스크립트
#
# backup-master.ps1 가 만든 INSERT 스크립트를 신규(또는 빈) Docker DB 에 적용한다.
# 스키마 자체는 docker-compose 의 init 스크립트(01_init_acsdb.sql) 가 이미 만들어 둔 상태여야 함.
#
# 기본 동작:
#   - 컨테이너/스키마 존재 여부 사전 검증
#   - 마스터 테이블에 기존 데이터가 있으면 거부 (-Force 또는 -Truncate 필요)
#   - 트랜잭션(-1) + ON_ERROR_STOP=1 — 일부 실패 시 전체 롤백
#
# 사용법:
#   pwsh docker/scripts/restore-master.ps1 -InputFile .\docker\backups\acs-master-20260511-120000.sql
#   pwsh docker/scripts/restore-master.ps1 -InputFile .\backup.sql -Truncate
#
# 시나리오:
#   1) docker-compose down -v && docker-compose up -d   (스키마 자동 재생성)
#   2) (스키마가 준비될 때까지 대기 — 컨테이너 로그 확인)
#   3) pwsh docker/scripts/restore-master.ps1 -InputFile <백업파일>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputFile,
    [string]$Container = 'acs-postgres-db',
    [string]$Database  = 'acsdb',
    [string]$User      = 'postgres',
    [string]$Password  = '1234',
    [switch]$Force,
    [switch]$Truncate
)

$ErrorActionPreference = 'Stop'

# 백업 스크립트와 동일한 마스터 테이블 목록.
# NA_X_APPLICATION 은 -IncludeApplication 옵션으로 백업됐을 때만 포함되지만,
# 복원 시에는 존재 가능성이 있는 모든 테이블을 점검 대상으로 둔다.
. $PSScriptRoot\_master-tables.ps1
$masterTables = $script:MasterTablesWithApplication

# 1) 입력 파일 검증
if (-not (Test-Path $InputFile)) {
    Write-Error "백업 파일을 찾을 수 없습니다: $InputFile"
    exit 1
}
$InputFile = (Resolve-Path $InputFile).Path
$fi = Get-Item $InputFile
if ($fi.Length -eq 0) {
    Write-Error "백업 파일이 비어 있습니다: $InputFile"
    exit 1
}

# 2) 컨테이너 가동 여부
$running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
if (-not $running) {
    Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker-compose up -d' 먼저 실행하세요."
    exit 1
}

# psql 한 줄 쿼리 실행 — -tA(tuples-only, unaligned)로 값만 반환.
# Windows PowerShell 5.1 이 docker.exe 로 argv 전달 시 큰따옴표(") 가 누락되는 버그가 있어,
# SQL 을 호스트 임시파일로 만들어 컨테이너에 복사한 뒤 psql -f 로 실행한다.
$script:remoteQueryPath = '/tmp/acs-restore-query.sql'
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

# 3) 스키마 존재 검증 — 핵심 마스터 테이블 NA_R_NODE 가 있는지
#    PowerShell 의 "" 는 큰따옴표 문자열 내에서 리터럴 " 로 평가됨.
#    결과 SQL: SELECT to_regclass('public."NA_R_NODE"') IS NOT NULL
$check = Invoke-Psql "SELECT to_regclass('public.""NA_R_NODE""') IS NOT NULL"
if ($check.Trim() -ne 't') {
    Write-Error "스키마가 없습니다. 'docker-compose up -d' 로 init 스크립트가 실행되었는지 확인하세요."
    exit 1
}

Write-Host "Container : $Container" -ForegroundColor Cyan
Write-Host "Database  : $Database"
Write-Host ("Input     : {0} ({1:N0} bytes)" -f $InputFile, $fi.Length)
Write-Host ''

# 4) 기존 데이터 검사
$existingRows = 0
$nonEmpty = @()
foreach ($t in $masterTables) {
    $sqlExist = "SELECT to_regclass('public.""$t""') IS NOT NULL"
    if ((Invoke-Psql $sqlExist).Trim() -ne 't') { continue }
    $cnt = [int]((Invoke-Psql "SELECT COUNT(*) FROM public.""$t""").Trim())
    if ($cnt -gt 0) {
        $existingRows += $cnt
        $nonEmpty += "$t=$cnt"
    }
}

if ($existingRows -gt 0) {
    Write-Host '기존 데이터가 있는 테이블:' -ForegroundColor Yellow
    $nonEmpty | ForEach-Object { Write-Host "  - $_" }
    if ($Truncate) {
        Write-Host '-Truncate 옵션: 모든 마스터 테이블을 비우고 복원합니다.' -ForegroundColor Yellow
        $truncList = ($masterTables | ForEach-Object { "public.""$_""" }) -join ', '
        $null = Invoke-Psql "TRUNCATE $truncList RESTART IDENTITY CASCADE"
    }
    elseif (-not $Force) {
        Write-Error '기존 데이터가 존재합니다. -Truncate (비우고 복원) 또는 -Force (PK 충돌 시 롤백) 를 명시하세요.'
        exit 1
    }
    else {
        Write-Host '-Force 옵션: TRUNCATE 없이 INSERT 만 시도합니다. PK 충돌 시 전체 롤백됩니다.' -ForegroundColor Yellow
    }
}

# 5) 백업 파일을 컨테이너로 전송
$remotePath = '/tmp/acs-master-restore.sql'
Write-Host "백업 파일 컨테이너로 전송 중..." -ForegroundColor Cyan
& docker cp $InputFile "$Container`:$remotePath"
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker cp 실패 (exit=$LASTEXITCODE)"
    exit 1
}

# 6) psql -f 로 단일 트랜잭션 적용
Write-Host "psql 로 복원 중..." -ForegroundColor Cyan
& docker exec -e "PGPASSWORD=$Password" $Container `
    psql -U $User -d $Database -v 'ON_ERROR_STOP=1' -1 -f $remotePath
$psqlExit = $LASTEXITCODE

# 7) 컨테이너 임시파일 삭제
& docker exec $Container rm -f $remotePath | Out-Null

if ($psqlExit -ne 0) {
    Write-Error "psql 실패 (exit=$psqlExit). 트랜잭션이 롤백되었습니다."
    exit $psqlExit
}

# 8) 복원 후 row 수 요약
Write-Host ''
Write-Host '복원 완료 — 테이블별 row 수:' -ForegroundColor Green
foreach ($t in $masterTables) {
    $sqlExist = "SELECT to_regclass('public.""$t""') IS NOT NULL"
    if ((Invoke-Psql $sqlExist).Trim() -ne 't') { continue }
    $cnt = (Invoke-Psql "SELECT COUNT(*) FROM public.""$t""").Trim()
    Write-Host ("  {0,-30} {1,8}" -f $t, $cnt)
}

exit 0
