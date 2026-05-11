# ACS DB 마스터 데이터 백업/복원

운영 중인 ACS Postgres 컨테이너에서 **마스터/설정 데이터**(노드, 링크, 차량, 알람 스펙, MQTT 설정, 옵션 등)를 추출하고, 신규 설치 또는 볼륨을 재생성한 컨테이너에 다시 적재하는 PowerShell 스크립트.

> 이력/운영 데이터(이력, 큐, 차량 런타임 상태)는 신규 설치 시 비어 있어야 정상이므로 백업·복원 대상에서 제외함.

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

### A. 사이트 이관 (사이트 A → 사이트 B)

```powershell
# 사이트 A
pwsh docker/scripts/backup-master.ps1 -OutputDir .\transfer

# 사이트 B (acsdb 가 빈 스키마만 존재)
pwsh docker/scripts/restore-master.ps1 -InputFile .\transfer\acs-master-*.sql
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

## 백업 대상 테이블

| 영역 | 테이블 |
|------|--------|
| Path / Layout | NA_R_NODE, NA_R_LINK, NA_R_LINK_ZONE, NA_R_STATION, NA_R_LOCATION, NA_R_BAY, NA_R_ZONE |
| Intersection | NA_T_INTERSECTION, NA_R_ORDER_PAIR |
| Vehicle | NA_R_VEHICLE |
| 자재 / 알람 | NA_M_CARRIER, NA_A_ALARMSPEC |
| 통신 | NA_C_MQTT |
| 사이트 / 옵션 | NA_R_SPECIALCONFIG, NA_X_OPTION, NA_X_APPLICATION_MANAGER |
| 옵션 포함 | NA_X_APPLICATION (`-IncludeApplication` 명시 시) |

## 제외되는 테이블 (운영/이력)

- 현재 명령/상태: `NA_T_TRANSPORTCMD`, `NA_T_CURRENTINTERSECTION`, `NA_C_NIO`, `NA_A_ALARM`
- 차량 런타임: `NA_R_VEHICLE_IDLE`, `NA_R_VEHICLE_ORDER`, `NA_R_VEHICLE_CROSS_WAIT`
- 이력: `NA_H_*` (9개)
- 로그/요청 큐/UI 로그: `NA_L_*`, `NA_Q_*`, `NA_U_*`

## 동작 원리

- **백업**: 컨테이너 내부에서 `pg_dump --data-only --column-inserts --disable-triggers --no-owner --no-privileges -t <마스터테이블>` 실행 → `docker cp` 로 호스트에 회수.
- **복원**: 호스트의 SQL 을 `docker cp` 로 컨테이너에 전송 → `psql -1 -v ON_ERROR_STOP=1 -f ...` 로 단일 트랜잭션 적용. 시퀀스 값은 `pg_dump` 가 자동 포함한 `setval()` 로 동기화됨.
- **스키마**: `docker/init/01_init_acsdb.sql` 가 컨테이너 최초 기동 시 자동 생성. 백업 파일에는 스키마가 포함되지 않음.
