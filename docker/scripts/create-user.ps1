# ACS 제한된 협업자 DB 계정 생성 스크립트
#
# 운영 DB(acsdb) 의 슈퍼유저(postgres) 자격증명을 공유하지 않고,
# 외부 협업자용 "제한된 로그인 롤" 을 만든다.
#
# 부여하는 것:
#   - acsdb 접속 권한 (CONNECT)
#   - 본인 전용 스키마 (소유자=본인) → 그 안에서 자유롭게 테이블 생성
#   - 지정한 일부 테이블에 대한 읽기 전용(SELECT) 권한
# 부여하지 않는 것:
#   - 슈퍼유저 / CREATEDB / CREATEROLE
#   - 그 외 모든 NA_* 테이블의 데이터 접근 (GRANT 안 함 = 기본 차단)
#
# ⚠️ 한계: 비공개 테이블의 '데이터' 는 차단되지만, 테이블 '이름 목록' 은
#    pg_catalog(psql \dt 등) 로는 조회될 수 있다. (GUI 가 쓰는 information_schema 는 권한 기준 필터됨)
#    이름까지 완전 은폐는 별도 DB 분리(FDW) 가 필요 — 이 스크립트 범위 밖.
#
# 사용법 (PowerShell 5.1 에서는 'pwsh' 대신 '.\' 로 직접 실행):
#   # 로컬 컨테이너 대상
#   .\create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' -DryRun
#   .\create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!'
#   # 원격 운영 DB 대상 (-DbHost 지정 → postgres 이미지를 클라이언트로 TCP 접속)
#   .\create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' -DbHost 10.0.26.2 -AdminPassword '<운영비번>' -DryRun
#   .\create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' -DbHost 10.0.26.2 -AdminPassword '<운영비번>'
#
# 사전조건:
#   - DryRun: Docker 불필요 (SQL 만 출력)
#   - 실제 적용: Docker Desktop 기동 필요. 로컬 모드는 acs-postgres-db 컨테이너,
#     원격 모드(-DbHost)는 postgres 이미지($ClientImage)만 있으면 됨.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$NewUser,
    [Parameter(Mandatory = $true)]
    [string]$NewPassword,
    [string]$SchemaName,
    [string[]]$GrantTables = @('NA_R_VEHICLE', 'NA_T_TRANSPORTCMD'),
    [string]$Privilege = 'SELECT',
    [string]$Container = 'acs-postgres-db',
    [string]$Database  = 'acsdb',
    [string]$AdminUser = 'postgres',
    [string]$AdminPassword = '1234',
    # 원격 DB 모드: -DbHost 지정 시 로컬 컨테이너 대신 해당 호스트로 TCP 접속한다.
    # 로컬에 psql 이 없을 수 있으므로 postgres 이미지를 일회용 클라이언트로 사용한다.
    [string]$DbHost = '',
    [int]$DbPort = 5432,
    [string]$ClientImage = 'postgres:17',
    [switch]$DryRun
)

$IsRemote = -not [string]::IsNullOrWhiteSpace($DbHost)

$ErrorActionPreference = 'Stop'

if (-not $SchemaName) { $SchemaName = $NewUser }

# SQL 식별자/문자열 이스케이프
#   식별자(롤·스키마·테이블)는 큰따옴표로 감싸므로 내부 " 를 "" 로 이중화
#   문자열 리터럴(비밀번호·rolname)은 작은따옴표이므로 내부 ' 를 '' 로 이중화
function Quote-Ident { param([string]$s) return $s.Replace('"', '""') }
$roleId   = Quote-Ident $NewUser
$schemaId = Quote-Ident $SchemaName
$dbId     = Quote-Ident $Database
$roleLit  = $NewUser.Replace("'", "''")
$pwLit    = $NewPassword.Replace("'", "''")

# 1) 적용할 SQL 조립 (모두 idempotent — 재실행 안전)
#    (DryRun 은 여기까지만 — Docker 데몬 없이도 SQL 미리보기가 동작한다)
#    템플릿은 단일따옴표 here-string(리터럴)이라 $$, " 가 그대로 보존된다.
$pwForSql = if ($DryRun) { '********' } else { $pwLit }

$template = @'
-- 1) 비-슈퍼유저 로그인 롤 (이미 있으면 비번만 갱신)
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{ROLELIT}') THEN
    CREATE ROLE "{ROLEID}" LOGIN PASSWORD '{PW}' NOSUPERUSER NOCREATEDB NOCREATEROLE;
  ELSE
    ALTER ROLE "{ROLEID}" WITH LOGIN PASSWORD '{PW}';
  END IF;
END $$;

-- 2) DB 접속 권한
GRANT CONNECT ON DATABASE "{DBID}" TO "{ROLEID}";

-- 3) 본인 전용 스키마(소유자=본인) → 이 안에서 자유롭게 CREATE TABLE
CREATE SCHEMA IF NOT EXISTS "{SCHEMAID}" AUTHORIZATION "{ROLEID}";

-- 3b) 본인 스키마를 search_path 최우선으로 → 별도 지정 없이 본인 스키마에 생성됨
ALTER ROLE "{ROLEID}" SET search_path = "{SCHEMAID}", public;

-- 4) 공유 테이블 접근용 public USAGE (객체 '진입'만; 데이터 권한 아님)
GRANT USAGE ON SCHEMA public TO "{ROLEID}";
'@

$sql = $template.
    Replace('{ROLELIT}',  $roleLit).
    Replace('{ROLEID}',   $roleId).
    Replace('{DBID}',     $dbId).
    Replace('{SCHEMAID}', $schemaId).
    Replace('{PW}',       $pwForSql)

# 5) 지정 테이블 GRANT (읽기 전용 기본)
$sql += "`n-- 5) 지정 테이블 권한 ($Privilege)`n"
foreach ($t in $GrantTables) {
    $tid = Quote-Ident $t
    $sql += 'GRANT ' + $Privilege + ' ON public."' + $tid + '" TO "' + $roleId + '";' + "`n"
}

if ($IsRemote) {
    Write-Host "Target    : 원격 ${DbHost}:${DbPort}  (postgres 이미지 '$ClientImage' 를 클라이언트로 사용)" -ForegroundColor Cyan
} else {
    Write-Host "Target    : 로컬 컨테이너 '$Container'" -ForegroundColor Cyan
}
Write-Host "Database  : $Database"
Write-Host "New role  : $NewUser  (NOSUPERUSER, NOCREATEDB, NOCREATEROLE)"
Write-Host "Schema    : $SchemaName  (소유자=$NewUser, 테이블 생성 가능)"
Write-Host "Grant     : $Privilege -> $($GrantTables -join ', ')"
Write-Host ''

if ($DryRun) {
    Write-Host '[DryRun] 적용될 SQL (비밀번호 마스킹됨):' -ForegroundColor Yellow
    Write-Host $sql
    exit 0
}

# 6) 실제 적용 전 Docker 데몬 가동 여부 확인
$null = & docker info --format '{{.ServerVersion}}' 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker 데몬에 연결할 수 없습니다. Docker Desktop 을 먼저 켜세요."
    exit 1
}

$psqlExit = 1
if ($IsRemote) {
    # 7-원격) postgres 이미지를 일회용 psql 클라이언트로 써서 $DbHost 에 TCP 접속.
    #         SQL 은 stdin 으로 전달(파일 마운트 불필요, argv 따옴표 버그도 회피).
    Write-Host "원격 ${DbHost}:${DbPort} 에 롤/스키마/권한 적용 중..." -ForegroundColor Cyan
    $sql | & docker run --rm -i -e "PGPASSWORD=$AdminPassword" $ClientImage `
        psql -h $DbHost -p $DbPort -U $AdminUser -d $Database -v 'ON_ERROR_STOP=1' -1
    $psqlExit = $LASTEXITCODE
}
else {
    # 7-로컬) 컨테이너 가동 확인 후, SQL 을 임시파일로 → docker cp → psql -1 -f.
    #         Windows PowerShell 5.1 이 docker argv 의 큰따옴표를 누락하는 버그가 있어 파일 경유.
    $running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
    if (-not $running) {
        Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker compose -f docker/docker-compose.yml up -d' 를 먼저 실행하세요."
        exit 1
    }
    $remotePath = '/tmp/acs-create-user.sql'
    $localSql = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText($localSql, $sql + "`n", (New-Object System.Text.UTF8Encoding($false)))
        & docker cp $localSql "$Container`:$remotePath"
        if ($LASTEXITCODE -ne 0) { Write-Error "docker cp 실패 (exit=$LASTEXITCODE)"; exit 1 }

        Write-Host '롤/스키마/권한 적용 중...' -ForegroundColor Cyan
        & docker exec -e "PGPASSWORD=$AdminPassword" $Container `
            psql -U $AdminUser -d $Database -v 'ON_ERROR_STOP=1' -1 -f $remotePath
        $psqlExit = $LASTEXITCODE
    }
    finally {
        Remove-Item -Force $localSql -ErrorAction SilentlyContinue
        & docker exec $Container rm -f $remotePath 2>$null | Out-Null
    }
}

if ($psqlExit -ne 0) {
    Write-Error "psql 실패 (exit=$psqlExit). 트랜잭션이 롤백되었습니다."
    exit $psqlExit
}

Write-Host ''
Write-Host '완료 - 제한된 계정이 생성/갱신되었습니다.' -ForegroundColor Green
Write-Host "  롤        : $NewUser"
Write-Host "  스키마    : $SchemaName"
Write-Host "  읽기 전용 : $($GrantTables -join ', ')"
Write-Host ''
Write-Host '검증 예 (새 자격증명으로 접속):' -ForegroundColor Cyan
if ($IsRemote) {
    Write-Host "  docker run --rm -e PGPASSWORD='<비번>' $ClientImage psql -h $DbHost -p $DbPort -U $NewUser -d $Database -c 'SELECT count(*) FROM public.""NA_R_VEHICLE"";'"
} else {
    Write-Host "  docker exec -e PGPASSWORD='<비번>' $Container psql -U $NewUser -d $Database -c 'SELECT count(*) FROM public.""NA_R_VEHICLE"";'"
}

exit 0
