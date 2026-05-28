# ACS 스키마 전용 덤프 스크립트
#
# Docker 컨테이너(acs-postgres-db) 안에서 pg_dump --schema-only 를 실행해
# 현재 라이브 DB 의 "스키마만"(데이터 0건) 추출한다.
# 결과 파일은 신규 서버의 docker/init/01_init_acsdb.sql 로 그대로 사용할 수 있다.
#
# 왜 필요한가:
#   레포의 docker/init/01_init_acsdb.sql 에는 과거 DEMO 데이터가 박혀 있어
#   신규 서버가 그 데이터로 자동 초기화된다. 이 스크립트로 뽑은 스키마 전용 파일을
#   init 으로 쓰면 신규 서버가 "빈 현재 스키마"로 기동하고,
#   그 위에 restore-master.ps1 로 현재 마스터 데이터만 적재한다.
#   (라이브 스키마라 02/03 마이그레이션이 이미 반영돼 있어 별도 마이그 스크립트 불필요)
#
# 사용법:
#   pwsh docker/scripts/backup-schema.ps1
#   pwsh docker/scripts/backup-schema.ps1 -OutputDir D:\backup
#   pwsh docker/scripts/backup-schema.ps1 -DryRun
#
# 사전조건: 컨테이너가 기동 중이어야 함 (docker ps 로 확인).

[CmdletBinding()]
param(
    [string]$Container = 'acs-postgres-db',
    [string]$Database  = 'acsdb',
    [string]$User      = 'postgres',
    [string]$Password  = '1234',
    [string]$OutputDir = "$PSScriptRoot\..\backups",
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# 1) 컨테이너 가동 여부 확인
$running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
if (-not $running) {
    Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker-compose up -d' 먼저 실행하세요."
    exit 1
}

# 2) 출력 폴더 준비
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
if (-not (Test-Path $OutputDir)) {
    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }
}

$timestamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputFile = Join-Path $OutputDir "acs-schema-$timestamp.sql"

# 3) 컨테이너 내부에서 실행할 명령
#    --schema-only     데이터 제외, DDL 만
#    --no-owner / --no-privileges  OWNER/GRANT 제거 (DB 간 이식성)
$remoteDump = '/tmp/acs-schema.sql'
$dumpInner  = "PGPASSWORD='$Password' pg_dump -U '$User' -d '$Database' --schema-only --no-owner --no-privileges -f '$remoteDump'"

Write-Host "Container : $Container" -ForegroundColor Cyan
Write-Host "Database  : $Database"
Write-Host "Mode      : schema-only (데이터 없음)"
Write-Host "Output    : $outputFile"
Write-Host ""

if ($DryRun) {
    Write-Host '[DryRun] 컨테이너에서 실행될 명령:' -ForegroundColor Yellow
    Write-Host "  docker exec $Container bash -c `"$dumpInner`""
    Write-Host "  docker cp $Container`:$remoteDump $outputFile"
    Write-Host "  docker exec $Container rm -f $remoteDump"
    exit 0
}

# 4) pg_dump 실행
Write-Host 'pg_dump --schema-only 실행 중...' -ForegroundColor Cyan
& docker exec $Container bash -c $dumpInner
if ($LASTEXITCODE -ne 0) {
    Write-Error "pg_dump 실패 (exit=$LASTEXITCODE)"
    exit 1
}

# 5) 호스트로 회수 후 컨테이너 임시파일 삭제
& docker cp "$Container`:$remoteDump" $outputFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker cp 실패 (exit=$LASTEXITCODE)"
    exit 1
}
& docker exec $Container rm -f $remoteDump | Out-Null

# 6) 결과 확인
$fi = Get-Item $outputFile
$tableCount = (Select-String -Path $outputFile -Pattern '^CREATE TABLE' -SimpleMatch:$false).Count
$dataCount  = (Select-String -Path $outputFile -Pattern '^(COPY |INSERT INTO)' -SimpleMatch:$false).Count

Write-Host ''
Write-Host "스키마 덤프 완료" -ForegroundColor Green
Write-Host ("  파일       : {0}" -f $outputFile)
Write-Host ("  크기       : {0:N0} bytes" -f $fi.Length)
Write-Host ("  CREATE TABLE : {0} 개" -f $tableCount)
Write-Host ("  데이터 줄  : {0} (0 이어야 정상)" -f $dataCount)
Write-Host ''
Write-Host "다음 단계: 이 파일을 신규 서버의 docker/init/01_init_acsdb.sql 로 배치하세요." -ForegroundColor Cyan

if ($tableCount -eq 0) {
    Write-Warning '덤프에 CREATE TABLE 이 없습니다. 스키마가 비어 있거나 권한 문제일 수 있습니다.'
    exit 2
}
if ($dataCount -gt 0) {
    Write-Warning "데이터 줄이 $dataCount 개 발견됨 — --schema-only 가 예상대로 동작하지 않았을 수 있습니다."
}

exit 0
