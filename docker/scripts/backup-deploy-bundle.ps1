# ACS 운영 서버 배포 번들 생성 스크립트
#
# 라이브 컨테이너(acs-postgres-db)에서 스키마 + 마스터 데이터를 뽑고
# docker-compose.yml + README 까지 묶어 한 디렉토리로 출력한다.
# 신규 운영 서버에 그 디렉토리를 통째로 복사하고 `docker-compose up -d` 한 번만 하면
# init/01_schema.sql -> init/02_master_data.sql 순으로 자동 적용되어 즉시 운영 가능.
#
# 백업 범위(로그/이력/런타임 큐는 모두 제외):
#   * 스키마 전체 (CREATE TABLE / 시퀀스 / 인덱스 / FK)
#   * 17개 마스터 테이블 (NA_R_*, NA_C_*, NA_M_*, NA_A_ALARMSPEC, NA_X_OPTION 등)
#   * -IncludeApplication 시 NA_X_APPLICATION 추가 (18개)
#
# 사용법:
#   pwsh docker/scripts/backup-deploy-bundle.ps1
#   pwsh docker/scripts/backup-deploy-bundle.ps1 -IncludeApplication
#   pwsh docker/scripts/backup-deploy-bundle.ps1 -NewPassword 'StrongPw!'
#   pwsh docker/scripts/backup-deploy-bundle.ps1 -OutputDir D:\acs-deploy
#   pwsh docker/scripts/backup-deploy-bundle.ps1 -DryRun
#
# 사전조건: 소스 컨테이너가 기동 중이어야 함 (docker ps 로 확인).

[CmdletBinding()]
param(
    [string]$Container = 'acs-postgres-db',
    [string]$Database  = 'acsdb',
    [string]$User      = 'postgres',
    [string]$Password  = '1234',
    [string]$OutputDir = "$PSScriptRoot\..\backups",
    [string]$BundleName,
    [switch]$IncludeApplication,
    [string]$NewPassword,
    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# 1) 마스터 테이블 목록 import
. $PSScriptRoot\_master-tables.ps1
$masterTables = if ($IncludeApplication) { $script:MasterTablesWithApplication } else { $script:MasterTables }

# 2) 컨테이너 가동 여부
$running = & docker ps --filter "name=^/$Container$" --filter 'status=running' --format '{{.Names}}' 2>$null
if (-not $running) {
    Write-Error "컨테이너 '$Container' 가 실행 중이 아닙니다. 'docker-compose up -d' 먼저 실행하세요."
    exit 1
}

# 3) 번들 경로 결정
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if (-not $BundleName) { $BundleName = "deploy-$timestamp" }
$bundleRoot = Join-Path $OutputDir $BundleName
$bundleInit = Join-Path $bundleRoot 'init'
$bundleCompose = Join-Path $bundleRoot 'docker-compose.yml'
$bundleReadme  = Join-Path $bundleRoot 'README.md'
$schemaPath    = Join-Path $bundleInit '01_schema.sql'
$dataPath      = Join-Path $bundleInit '02_master_data.sql'

# 비밀번호 결정 (미지정 시 placeholder)
$composePassword = if ([string]::IsNullOrEmpty($NewPassword)) { '__CHANGE_ME__' } else { $NewPassword }
$composePasswordMasked = if ([string]::IsNullOrEmpty($NewPassword)) { '__CHANGE_ME__' } else { '***' }

Write-Host "Container        : $Container" -ForegroundColor Cyan
Write-Host "Database         : $Database"
Write-Host "Bundle           : $bundleRoot"
Write-Host "Tables           : $($masterTables.Count) 개 (IncludeApplication=$IncludeApplication)"
Write-Host "Compose password : $composePasswordMasked"
Write-Host ''

# 4) 동명 번들 처리
if (Test-Path $bundleRoot) {
    if ($Force) {
        if (-not $DryRun) {
            Write-Host "기존 번들 디렉토리를 덮어씁니다: $bundleRoot" -ForegroundColor Yellow
            Remove-Item -Recurse -Force $bundleRoot
        }
    }
    else {
        Write-Error "이미 존재하는 번들 디렉토리: $bundleRoot (덮어쓰려면 -Force)"
        exit 1
    }
}

# 5) 원본 docker-compose.yml 위치 검증 (번들 compose 생성에 필요)
$sourceCompose = Join-Path $PSScriptRoot '..\docker-compose.yml'
$sourceCompose = [System.IO.Path]::GetFullPath($sourceCompose)
if (-not (Test-Path $sourceCompose)) {
    Write-Error "원본 docker-compose.yml 을 찾을 수 없습니다: $sourceCompose"
    exit 1
}

# 6) DryRun: 단계만 출력 후 종료
if ($DryRun) {
    Write-Host '[DryRun] 다음 작업이 실행될 예정입니다:' -ForegroundColor Yellow
    Write-Host "  1) mkdir $bundleInit"
    Write-Host "  2) 스키마 덤프 -> $schemaPath"
    Write-Host "     docker exec $Container bash -c `"PGPASSWORD='***' pg_dump -U '$User' -d '$Database' --schema-only --no-owner --no-privileges -f /tmp/acs-schema.sql`""
    Write-Host "     docker cp $Container`:/tmp/acs-schema.sql <위 경로>"
    Write-Host "  3) 마스터 데이터 덤프 -> $dataPath"
    Write-Host "     pg_dump --data-only --column-inserts --disable-triggers --no-owner --no-privileges -t '<table>' ... (테이블 $($masterTables.Count) 개)"
    Write-Host "  4) docker-compose.yml 사본 (POSTGRES_PASSWORD: `"$composePasswordMasked`") -> $bundleCompose"
    Write-Host "  5) README.md 생성 -> $bundleReadme"
    Write-Host "  6) 무결성 점검 + 테이블별 row 수 출력"
    exit 0
}

# 7) 번들 디렉토리 생성
New-Item -ItemType Directory -Path $bundleInit -Force | Out-Null

# 실패 시 롤백을 위한 try/catch
$rollback = $true
try {
    # ===== 7a) 스키마 덤프 =====
    Write-Host 'pg_dump --schema-only 실행 중...' -ForegroundColor Cyan
    $remoteSchema = '/tmp/acs-schema.sql'
    $schemaInner  = "PGPASSWORD='$Password' pg_dump -U '$User' -d '$Database' --schema-only --no-owner --no-privileges -f '$remoteSchema'"
    & docker exec $Container bash -c $schemaInner
    if ($LASTEXITCODE -ne 0) { throw "pg_dump --schema-only 실패 (exit=$LASTEXITCODE)" }

    & docker cp "$Container`:$remoteSchema" $schemaPath
    if ($LASTEXITCODE -ne 0) { throw "docker cp (schema) 실패 (exit=$LASTEXITCODE)" }
    & docker exec $Container rm -f $remoteSchema | Out-Null

    # ===== 7b) 마스터 데이터 덤프 =====
    # PS 5.1 의 docker.exe argv 인용 버그 회피 — bash 스크립트를 호스트에서 합성해 컨테이너로 복사 후 실행.
    Write-Host 'pg_dump --data-only 실행 중...' -ForegroundColor Cyan
    $remoteData   = '/tmp/acs-master.sql'
    $remoteScript = '/tmp/acs-master.sh'
    $lf = "`n"

    $dumpCmd = @(
        "PGPASSWORD='$Password' pg_dump -U '$User' -d '$Database' \",
        "  --data-only --column-inserts --disable-triggers --no-owner --no-privileges \",
        "  -f '$remoteData' \"
    )
    $tableLines = $masterTables | ForEach-Object { "  -t 'public.""$_""' \" }
    if ($tableLines.Count -gt 0) {
        $tableLines[-1] = $tableLines[-1].TrimEnd(' \')
    }
    $scriptBody = '#!/bin/bash' + $lf + 'set -e' + $lf + ($dumpCmd -join $lf) + $lf + ($tableLines -join $lf) + $lf

    $localScript = [System.IO.Path]::GetTempFileName() + '.sh'
    [System.IO.File]::WriteAllText($localScript, $scriptBody, (New-Object System.Text.UTF8Encoding($false)))

    & docker cp $localScript "$Container`:$remoteScript"
    $cpExit = $LASTEXITCODE
    Remove-Item -Force $localScript -ErrorAction SilentlyContinue
    if ($cpExit -ne 0) { throw "docker cp (data script) 실패 (exit=$cpExit)" }

    & docker exec $Container bash $remoteScript
    if ($LASTEXITCODE -ne 0) { throw "pg_dump --data-only 실패 (exit=$LASTEXITCODE)" }

    & docker cp "$Container`:$remoteData" $dataPath
    if ($LASTEXITCODE -ne 0) { throw "docker cp (data) 실패 (exit=$LASTEXITCODE)" }
    & docker exec $Container rm -f $remoteScript $remoteData | Out-Null

    # ===== 7c) docker-compose.yml 사본 (비번 치환) =====
    Write-Host 'docker-compose.yml 사본 작성 중...' -ForegroundColor Cyan
    $composeText = [System.IO.File]::ReadAllText($sourceCompose, [System.Text.Encoding]::UTF8)
    # 'POSTGRES_PASSWORD: "1234"' 형태의 라인을 새 비번 또는 placeholder 로 치환.
    # 공백/큰따옴표 변형까지 안전하게 다루기 위해 정규식 사용.
    $composeNew = [System.Text.RegularExpressions.Regex]::Replace(
        $composeText,
        '(?m)^(\s*POSTGRES_PASSWORD\s*:\s*).*$',
        ('${1}"' + $composePassword + '"')
    )
    [System.IO.File]::WriteAllText($bundleCompose, $composeNew, (New-Object System.Text.UTF8Encoding($false)))

    # ===== 7d) README =====
    Write-Host 'README.md 생성 중...' -ForegroundColor Cyan
    $sourceHost = try { [System.Net.Dns]::GetHostName() } catch { 'unknown' }
    $createdAt  = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')
    $tableCount = $masterTables.Count
    $passwordSection = if ($composePassword -eq '__CHANGE_ME__') {
@"
> ⚠️ ``docker-compose.yml`` 의 ``POSTGRES_PASSWORD`` 가 placeholder ``__CHANGE_ME__`` 로 들어 있습니다. 기동 전에 운영 비번으로 반드시 치환하세요.
"@
    } else {
@"
> ℹ️ ``docker-compose.yml`` 의 ``POSTGRES_PASSWORD`` 는 번들 생성 시 ``-NewPassword`` 로 지정한 값이 박혀 있습니다. 필요 시 직접 편집하세요.
"@
    }

    $readme = @"
# ACS 운영 서버 배포 번들

| 항목 | 값 |
|------|----|
| 생성 시각 | $createdAt |
| 소스 호스트 | $sourceHost |
| 소스 컨테이너 | $Container |
| 소스 DB | $Database |
| 마스터 테이블 수 | $tableCount (IncludeApplication=$IncludeApplication) |

이 디렉토리에는 신규 운영 서버에서 ACS DB 를 한 번에 띄우기 위한 모든 산출물이 들어 있습니다.

```
.
├── docker-compose.yml          # 비번이 치환된 사본
├── README.md                   # (이 문서)
└── init/
    ├── 01_schema.sql           # pg_dump --schema-only (DDL + 시퀀스 + 인덱스 + FK)
    └── 02_master_data.sql      # 마스터 ${tableCount}개 테이블 INSERT (시퀀스 setval 포함)
```

> 로그(``NA_L_*``), 이력(``NA_H_*``), 런타임 큐(``NA_T_*``, ``NA_Q_*``, ``NA_U_*``, ``NA_A_ALARM``) 는 의도적으로 빠져 있습니다 — 신규 서버에서는 비어 있어야 정상입니다.

---

## ⚠️ 기동 전 필수 변경

$passwordSection

추가로 다음 사이트 종속값을 운영 환경에 맞게 갱신해야 합니다 (DB 기동 후 SQL 로 수정):

``````sql
-- 현재 값 점검
SELECT name, brokerIp, brokerPort FROM public."NA_C_MQTT";
SELECT name, remoteIp, remotePort, machineName FROM public."NA_C_NIO";
SELECT * FROM public."NA_R_SPECIALCONFIG";

-- 예) MQTT 브로커 IP 갱신
UPDATE public."NA_C_MQTT" SET brokerIp = '<운영 broker IP>' WHERE name = '<해당 인터페이스>';
``````

또한 ACS 앱 측 ``appsettings.json`` 의 ``ConnectionStrings:DefaultConnection`` (호스트/비번), MQTT brokerIp, NIO remoteIp 를 맞춰주세요.

---

## 사전 점검

- Docker / Docker Compose 설치 확인 (``docker --version``, ``docker compose version`` 또는 ``docker-compose --version``)
- 호스트 포트 충돌 확인: ``5432``(Postgres), ``5672`` ``1883`` ``15672``(RabbitMQ — 같이 띄우는 경우)
- 디스크 여유 (마스터 데이터는 작지만 운영 중 ``NA_L_*`` / ``NA_H_*`` 가 증가)

---

## 기동 절차

``````powershell
# 이 디렉토리에서 실행 (Linux 라면 동일하게 bash 에서 실행)
docker-compose up -d

# 또는 신형 docker CLI
docker compose up -d

# init 적용 확인 (01_schema.sql -> 02_master_data.sql 순서)
docker logs -f acs-postgres-db
# "database system is ready to accept connections" 까지 보이면 완료
``````

> ``./init`` 디렉토리는 PostgreSQL 컨테이너의 **빈 볼륨이 최초 기동될 때 한 번만** 실행됩니다. 이미 데이터가 있는 볼륨이면 init 이 스킵되니, 재적용하려면 ``docker-compose down -v`` 로 볼륨까지 제거 후 다시 ``up -d`` 하세요. (운영 데이터 손실 주의)

---

## 검증

``````sql
-- 마스터 테이블 row 수 (번들 생성 시 콘솔에 출력된 수와 같아야 함)
SELECT 'NA_R_NODE' AS t, COUNT(*) FROM public."NA_R_NODE"
UNION ALL SELECT 'NA_R_LINK',    COUNT(*) FROM public."NA_R_LINK"
UNION ALL SELECT 'NA_R_VEHICLE', COUNT(*) FROM public."NA_R_VEHICLE"
UNION ALL SELECT 'NA_C_MQTT',    COUNT(*) FROM public."NA_C_MQTT"
UNION ALL SELECT 'NA_C_NIO',     COUNT(*) FROM public."NA_C_NIO";

-- 로그/이력/큐 테이블이 비어있는지 확인 (0 이어야 정상)
SELECT 'NA_L_LOGMESSAGE' AS t, COUNT(*) FROM public."NA_L_LOGMESSAGE"
UNION ALL SELECT 'NA_T_TRANSPORTCMD', COUNT(*) FROM public."NA_T_TRANSPORTCMD"
UNION ALL SELECT 'NA_A_ALARM',        COUNT(*) FROM public."NA_A_ALARM";
``````

---

## 롤백 / 재기동

``````powershell
docker-compose down -v   # 볼륨 포함 제거
docker-compose up -d     # init 재실행
``````
"@

    [System.IO.File]::WriteAllText($bundleReadme, $readme, (New-Object System.Text.UTF8Encoding($false)))

    # ===== 7e) 무결성 점검 =====
    $createTableCount = (Select-String -Path $schemaPath -Pattern '^CREATE TABLE' -SimpleMatch:$false).Count
    $insertCount      = (Select-String -Path $dataPath   -Pattern '^INSERT INTO'  -SimpleMatch:$false).Count

    if ($createTableCount -eq 0) {
        throw "스키마 덤프에 CREATE TABLE 이 없습니다 — 권한 또는 컨테이너 상태를 확인하세요."
    }
    if ($insertCount -eq 0) {
        throw "마스터 데이터에 INSERT 가 없습니다 — 마스터 테이블이 비어 있거나 권한 문제일 수 있습니다."
    }

    $rollback = $false
}
finally {
    if ($rollback) {
        Write-Warning "오류 발생 — 번들 디렉토리를 롤백합니다: $bundleRoot"
        Remove-Item -Recurse -Force $bundleRoot -ErrorAction SilentlyContinue
    }
}

# 8) 요약 출력
$schemaFi = Get-Item $schemaPath
$dataFi   = Get-Item $dataPath
$composeFi = Get-Item $bundleCompose
$readmeFi  = Get-Item $bundleReadme

Write-Host ''
Write-Host '번들 생성 완료' -ForegroundColor Green
Write-Host ("  번들 경로            : {0}" -f $bundleRoot)
Write-Host ("  init/01_schema.sql   : {0,10:N0} bytes ({1} CREATE TABLE)" -f $schemaFi.Length, $createTableCount)
Write-Host ("  init/02_master_data.sql : {0,7:N0} bytes ({1} INSERT)"     -f $dataFi.Length, $insertCount)
Write-Host ("  docker-compose.yml   : {0,10:N0} bytes" -f $composeFi.Length)
Write-Host ("  README.md            : {0,10:N0} bytes" -f $readmeFi.Length)
Write-Host ''
Write-Host '소스 DB 의 마스터 테이블 row 수:' -ForegroundColor Cyan

# 컨테이너에 SELECT 던져 row 수 출력 (검증용)
$countQueryLines = $masterTables | ForEach-Object {
    "SELECT '$_' AS t, COUNT(*)::text AS c FROM public.""$_"""
}
$countQuery = ($countQueryLines -join "$lf UNION ALL$lf") + ' ORDER BY t;'

$localCount = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($localCount, $countQuery + "`n", (New-Object System.Text.UTF8Encoding($false)))
$remoteCount = '/tmp/acs-count.sql'
& docker cp $localCount "$Container`:$remoteCount" 2>$null | Out-Null
$counts = & docker exec -e "PGPASSWORD=$Password" $Container `
    psql -U $User -d $Database -tA -F '|' -v 'ON_ERROR_STOP=1' -f $remoteCount
Remove-Item -Force $localCount -ErrorAction SilentlyContinue
& docker exec $Container rm -f $remoteCount 2>$null | Out-Null

foreach ($line in $counts) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line.Split('|')
    if ($parts.Count -ge 2) {
        Write-Host ("  {0,-30} {1,8}" -f $parts[0], $parts[1])
    }
}

Write-Host ''
Write-Host "다음 단계: 이 디렉토리를 신규 서버로 복사 후 'docker-compose up -d' 실행." -ForegroundColor Cyan
Write-Host "          기동 전에 README.md 의 '기동 전 필수 변경' 섹션을 반드시 확인하세요." -ForegroundColor Cyan

exit 0
