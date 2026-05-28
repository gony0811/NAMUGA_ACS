# ACS DB 마스터 데이터 백업/복원

운영 중인 ACS Postgres 컨테이너에서 **마스터/설정 데이터**(노드, 링크, 차량, 알람 스펙, MQTT 설정, 옵션 등)를 추출하고, 신규 설치 또는 볼륨을 재생성한 컨테이너에 다시 적재하는 PowerShell 스크립트.

> 이력/운영 데이터(이력, 큐, 차량 런타임 상태)는 신규 설치 시 비어 있어야 정상이므로 백업·복원 대상에서 제외함.

| 스크립트 | 역할 |
|----------|------|
| `backup-master.ps1` | 현재 마스터 데이터 추출 → `acs-master-<ts>.sql` (INSERT + setval) |
| `restore-master.ps1` | 위 파일을 빈 스키마에 적재 (단일 트랜잭션) |
| `backup-schema.ps1` | 라이브 DB 스키마 전용 덤프 → `acs-schema-<ts>.sql` (신규 서버 init 용). **서버 이전 시 사용** |
| `create-user.ps1` | 슈퍼유저 공유 없이 **제한된 협업자 계정** 생성 (일부 테이블 읽기 전용 + 본인 스키마). **시나리오 D 참고** |

## 사전조건

- `docker/docker-compose.yml` 로 띄운 `acs-postgres-db` 컨테이너가 실행 중일 것
- PowerShell 5.1 이상 또는 `pwsh`

## 백업

```powershell
# 기본: docker/backups/acs-master-<timestamp>.sql 로 저장
pwsh docker/scripts/backup-master.ps1

# 출력 폴더 지정
pwsh docker/scripts/backup-master.ps1 -OutputDir D:\acs-backup

# 명령만 확인 (실제 실행 X)
pwsh docker/scripts/backup-master.ps1 -DryRun

# NA_X_APPLICATION 도 포함 (사이트 이관 시)
pwsh docker/scripts/backup-master.ps1 -IncludeApplication
```

## 복원

```powershell
# 빈 DB 에 복원 (기존 마스터 데이터가 있으면 거부됨)
pwsh docker/scripts/restore-master.ps1 -InputFile .\docker\backups\acs-master-20260511-120000.sql

# 기존 마스터 데이터를 비우고 복원
pwsh docker/scripts/restore-master.ps1 -InputFile .\backup.sql -Truncate

# TRUNCATE 없이 INSERT 만 시도 (PK 충돌 시 롤백)
pwsh docker/scripts/restore-master.ps1 -InputFile .\backup.sql -Force
```

## 시나리오

### A. 서버 이전 / 사이트 이관 (구 서버 → 신 서버, 권장 클린 절차)

> ⚠️ 레포의 `docker/init/01_init_acsdb.sql` 에는 **과거 DEMO 데이터**가 박혀 있다(노드 N001~N012, 차량 AMR001, 192.168.1.x 데모 location 등). 이걸 그대로 init 으로 쓰면 신규 서버가 데모 데이터로 초기화된다. 신규 서버는 아래처럼 **스키마 전용 init** 위에 현재 마스터 데이터만 적재해야 깨끗하다.

```powershell
# --- 구 서버 (라이브 컨테이너 기동 중) ---
pwsh docker/scripts/backup-schema.ps1 -OutputDir .\transfer                 # acs-schema-<ts>.sql (스키마만)
pwsh docker/scripts/backup-master.ps1 -IncludeApplication -OutputDir .\transfer  # acs-master-<ts>.sql (현재 데이터)

# --- 신 서버 ---
# 1) acs-schema-<ts>.sql 을 docker/init/01_init_acsdb.sql 로 배치 (기존 데모 init 대체).
#    라이브 스키마라 02/03 마이그레이션이 이미 반영돼 있으므로 02/03_*.sql 은 제거.
# 2) docker-compose up -d        # init 이 빈 현재 스키마 생성
# 3) 마스터 데이터 적재:
pwsh docker/scripts/restore-master.ps1 -InputFile .\transfer\acs-master-<ts>.sql -Truncate
# 4) appsettings.json 의 DefaultConnection / MQTT brokerIp / NIO remoteIp 등 사이트 종속 값 점검·수정
```

### B. 같은 사이트 볼륨 재생성

```powershell
pwsh docker/scripts/backup-master.ps1                  # 먼저 백업
docker-compose down -v                                  # 볼륨 포함 제거
docker-compose up -d                                    # init 스크립트가 스키마 재생성
# (컨테이너 로그에서 "PostgreSQL init process complete" 확인)
pwsh docker/scripts/restore-master.ps1 -InputFile .\docker\backups\acs-master-<timestamp>.sql
```

### C. 운영 DB 마스터 데이터 교체

```powershell
pwsh docker/scripts/restore-master.ps1 -InputFile .\new-master.sql -Truncate
```

### D. 제한된 협업자 계정 생성

운영 DB의 슈퍼유저(`postgres`) 비번을 외부에 주지 않고, 협업자에게 **접속 + 일부 테이블 읽기 전용 + 본인 전용 스키마 테이블 생성권**만 부여한다. 앱/시뮬레이터의 접속 설정은 그대로 `postgres` 를 쓰며, 이 계정은 사람 전용이다.

```powershell
# DryRun 으로 적용될 SQL 먼저 확인 (비밀번호는 마스킹되어 출력)
pwsh docker/scripts/create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' -DryRun

# 실제 적용 (기본: NA_R_VEHICLE, NA_T_TRANSPORTCMD 읽기 전용)
pwsh docker/scripts/create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!'

# 공유 테이블 직접 지정 / 전용 스키마 이름 지정
pwsh docker/scripts/create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' `
     -GrantTables 'NA_R_VEHICLE','NA_T_TRANSPORTCMD' -SchemaName dev1_work
```

**원격 운영 DB 대상 (`-DbHost`)** — 로컬 컨테이너 대신 원격 호스트에 TCP 접속한다. 로컬에 `psql` 이 없어도 `postgres` 이미지(`-ClientImage`, 기본 `postgres:17`)를 일회용 클라이언트로 써서 SQL 을 stdin 으로 전달한다. Docker Desktop 만 떠 있으면 되고 로컬 `acs-postgres-db` 컨테이너는 불필요하다.

```powershell
# DryRun 은 접속 없이 SQL 만 출력 (Docker 도 불필요)
pwsh docker/scripts/create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' -DbHost 10.0.26.2 -DryRun

# 실제 적용 — 운영 DB 의 슈퍼유저 비번을 -AdminPassword 로 전달
pwsh docker/scripts/create-user.ps1 -NewUser dev1 -NewPassword 'S3cret!' `
     -DbHost 10.0.26.2 -AdminPassword '<운영 postgres 비번>'
```

> ⚠️ **운영 DB 주의**: `-DbHost` 는 운영 서버를 직접 변경한다. 적용 전 `-DryRun` 으로 SQL 을 검토하고, 가능하면 운영 점검 시간에 수행할 것. 단일 트랜잭션(`-1 -v ON_ERROR_STOP=1`)이라 중간 실패 시 전체 롤백된다.

**파라미터**

| 이름 | 기본값 | 설명 |
|------|--------|------|
| `-NewUser` | (필수) | 새 로그인 롤 이름 |
| `-NewPassword` | (필수) | 새 롤의 비밀번호. 커밋 파일에 박지 않도록 기본값 없음 |
| `-GrantTables` | `NA_R_VEHICLE`, `NA_T_TRANSPORTCMD` | 읽기 전용으로 열어줄 테이블 목록 |
| `-Privilege` | `SELECT` | 공유 테이블에 부여할 권한 (재사용 시 변경 가능, 예: `'SELECT, INSERT'`) |
| `-SchemaName` | `=NewUser` | 본인이 테이블을 만들 전용 스키마 이름 |
| `-AdminUser` / `-AdminPassword` | `postgres` / `1234` | GRANT 를 실행할 슈퍼유저 자격증명 |
| `-DryRun` | - | 적용 없이 생성될 SQL 만 출력 |

**부여/차단되는 것**

- ✅ `acsdb` 접속, 본인 전용 스키마 소유(그 안에서 자유롭게 `CREATE TABLE`), 지정 테이블 읽기 전용
- ❌ 슈퍼유저 / `CREATEDB` / `CREATEROLE`, `public` 에 무단 테이블 생성, GRANT 안 한 나머지 `NA_*` 테이블 데이터 접근

> ⚠️ **은폐 한계**: 비공개 테이블의 **데이터**는 완전히 차단되지만, 테이블 **이름 목록**은 `pg_catalog`(예: psql `\dt`)로 조회될 수 있다. GUI 툴이 주로 쓰는 `information_schema` 는 권한 기준으로 필터링되어 권한 없는 테이블이 안 뜬다. 이름까지 완전히 숨기려면 그 2개 테이블만 들어있는 **별도 DB + `postgres_fdw`** 구성이 필요하며, 이 스크립트 범위 밖이다.

> (선택적 강화) 익명/기타 접속까지 막으려면 슈퍼유저로 `REVOKE CONNECT ON DATABASE acsdb FROM PUBLIC;` 를 한 번 실행한다. 앱·시뮬레이터는 슈퍼유저라 영향 없다.

**검증** (새 자격증명으로 접속해 동작 확인)

```powershell
# 공유 테이블 읽기 OK / 쓰기·비공개 테이블·public 생성은 permission denied 여야 정상
docker exec -e PGPASSWORD='S3cret!' acs-postgres-db psql -U dev1 -d acsdb -c 'SELECT count(*) FROM public."NA_R_VEHICLE";'
docker exec -e PGPASSWORD='S3cret!' acs-postgres-db psql -U dev1 -d acsdb -c 'SELECT count(*) FROM public."NA_L_LOGMESSAGE";'   # denied 기대
docker exec -e PGPASSWORD='S3cret!' acs-postgres-db psql -U dev1 -d acsdb -c 'CREATE TABLE t_test(id int); DROP TABLE t_test;'  # 본인 스키마 OK
```

**계정/권한 회수 (롤백)** — 슈퍼유저로 실행

```sql
DROP SCHEMA IF EXISTS "dev1" CASCADE;   -- 본인 스키마+테이블 제거
REASSIGN OWNED BY "dev1" TO postgres;   -- 잔여 소유객체 이관(있다면)
DROP OWNED BY "dev1";                   -- 잔여 권한 제거
DROP ROLE IF EXISTS "dev1";
```

## 백업 대상 테이블

| 영역 | 테이블 |
|------|--------|
| Path / Layout | NA_R_NODE, NA_R_LINK, NA_R_LINK_ZONE, NA_R_STATION, NA_R_LOCATION, NA_R_BAY, NA_R_ZONE |
| Intersection | NA_T_INTERSECTION, NA_R_ORDER_PAIR |
| Vehicle | NA_R_VEHICLE |
| 자재 / 알람 | NA_M_CARRIER, NA_A_ALARMSPEC |
| 통신 | NA_C_MQTT, NA_C_NIO |
| 사이트 / 옵션 | NA_R_SPECIALCONFIG, NA_X_OPTION, NA_X_APPLICATION_MANAGER |
| 옵션 포함 | NA_X_APPLICATION (`-IncludeApplication` 명시 시) |

> `NA_C_MQTT`, `NA_C_NIO` 는 통신 인터페이스 정의다. `brokerIp` / `remoteIp` / `machineName` 등 **사이트 종속 값**은 이관 후 신규 환경에 맞게 수정해야 한다.

## 제외되는 테이블 (운영/이력)

- 현재 명령/상태: `NA_T_TRANSPORTCMD`, `NA_T_CURRENTINTERSECTION`, `NA_A_ALARM`
- 차량 런타임: `NA_R_VEHICLE_IDLE`, `NA_R_VEHICLE_ORDER`, `NA_R_VEHICLE_CROSS_WAIT`
- 이력: `NA_H_*` (9개)
- 로그/요청 큐/UI 로그: `NA_L_*`, `NA_Q_*`, `NA_U_*`

## 동작 원리

- **백업**: 컨테이너 내부에서 `pg_dump --data-only --column-inserts --disable-triggers --no-owner --no-privileges -t <마스터테이블>` 실행 → `docker cp` 로 호스트에 회수.
- **복원**: 호스트의 SQL 을 `docker cp` 로 컨테이너에 전송 → `psql -1 -v ON_ERROR_STOP=1 -f ...` 로 단일 트랜잭션 적용. 시퀀스 값은 `pg_dump` 가 자동 포함한 `setval()` 로 동기화됨.
- **스키마**: `docker/init/01_init_acsdb.sql` 가 컨테이너 최초 기동 시 자동 생성. 마스터 백업 파일에는 스키마가 포함되지 않음.
  - 단, 레포의 `01_init_acsdb.sql` 에는 과거 DEMO 데이터가 포함돼 있어 **신규 서버 init 으로는 부적합**하다. 서버 이전 시에는 `backup-schema.ps1` 로 라이브 DB의 **스키마 전용** 덤프(`acs-schema-<ts>.sql`)를 떠서 신규 서버의 init 으로 사용한다. → 시나리오 A 참고.
