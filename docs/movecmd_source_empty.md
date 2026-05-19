# MOVECMD SourceLoc/SourcePort 비어있는 경우 처리

## 개요

MES 가 송신하는 `MOVECMD` 메시지에서 `SourceLoc` / `SourcePort` 가 모두 비어있을 때 ACS 가 ActionType 에 따라 source/dest 를 자동 해석하여 `NA_T_TRANSPORTCMD` 행을 생성하는 로직. 또한 `MOVECMD.destLoc` 의 station 타입을 ActionType 과 호환되는지 검증하고, source 와 dest 가 동일해지는 비정상 케이스를 차단한다.

구현 위치: `src/ACS/ACS.Elsa/Activities/HostActivities.cs` 의 `CreateTransportCommandActivity`.

## MOVECMD 입력 예

```xml
<Msg>
  <Command>MOVECMD</Command>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <SourceLoc></SourceLoc>     <!-- 비어있음 -->
    <SourcePort></SourcePort>   <!-- 비어있음 -->
    <DestLoc>192.168.1.101</DestLoc>
    <DestPort>LEFT</DestPort>
    <ActionType>LOAD</ActionType>     <!-- 또는 UNLOAD -->
    <JobID>JOB003</JobID>
    <MaterialType>MAGAZINE</MaterialType>
  </DataLayer>
</Msg>
```

## 처리 순서

`CreateTransportCommandActivity.Execute` 에서 XML 필드 추출 직후 다음 순서로 수행 (`HostActivities.cs:342-407` 부근).

```
1. MOVECMD XML 필드 추출
2. MES.dest station 타입 검증 (LOAD/UNLOAD 일 때만)
3. source/sourcePort 비어있고 LOAD/UNLOAD 이면 자동 해석 분기
   ├── LOAD  : source = ResolveZoneMatchedBuffer(MES.dest, ACQUIRE)
   └── UNLOAD: source = MES.dest, dest = ResolveZoneMatchedBuffer(MES.dest, DEPOSIT)
4. source/dest 문자열 결합 ("Loc:Port")
5. source == dest 차단 가드
6. JobID 중복 검증
7. source/dest location 존재 확인
8. 동일 Bay 검증
9. TransportCommand DB 생성
```

## 자동 해석 — LOAD

- 입력: `ActionType=LOAD`, `SourceLoc=빈값`, `SourcePort=빈값`, `DestLoc=EQP IP`, `DestPort=LEFT|RIGHT`
- 처리:
  - `ResolveZoneMatchedBuffer(accessor, destLoc, destPort, "ACQUIRE")` 호출.
  - `MES.dest` 의 zone(=`NA_R_LINK_ZONE.zoneId` via `Station.LinkId`) 을 추출.
  - 동일 zone 의 `Location.Type=BUFFER` + `Station.Type=ACQUIRE` 후보 수집.
  - `LocationId` 사전순 첫 번째 후보의 `:` 앞부분을 `sourceLoc` 로 채움. `sourcePort="LEFT"` 고정.
- 결과 TransportCommand:
  - `source = <auto-resolved>:LEFT` (예: `BUF01:LEFT`)
  - `dest = MES.dest:DestPort` (예: `192.168.1.101:LEFT`)
- 실패: 후보 0건이면 `<ErrorCode>25</ErrorCode><ErrorMsg>SOURCEMACHINENOTFOUND</ErrorMsg>` 응답.

## 자동 해석 — UNLOAD

- 입력: `ActionType=UNLOAD`, `SourceLoc=빈값`, `SourcePort=빈값`, `DestLoc=EQP IP`, `DestPort=LEFT|RIGHT`
- 처리:
  1. `MES.dest/destPort` 를 `sourceLoc/sourcePort` 로 복사 (EQP 가 차량의 acquire 지점이 됨).
  2. `ResolveZoneMatchedBuffer(accessor, sourceLoc, sourcePort, "DEPOSIT")` 호출.
  3. 동일 zone 의 `Location.Type=BUFFER` + `Station.Type=DEPOSIT` 후보 수집.
  4. 첫 번째 후보의 `:` 앞부분을 `destLoc` 로 채움. `destPort="LEFT"` 고정.
- 결과 TransportCommand:
  - `source = MES.dest:DestPort` (예: `192.168.1.103:LEFT`)
  - `dest = <auto-resolved>:LEFT` (예: `BUF03:LEFT`)
- 실패: 후보 0건이면 `<ErrorCode>21</ErrorCode><ErrorMsg>DESTMACHINENOTFOUND</ErrorMsg>` 응답.

## 검증 — MES.dest 의 station 타입

`HostActivities.cs:361-393`. 자동 해석 분기 이전에 실행.

| ActionType | 허용되는 Station.Type |
|---|---|
| LOAD | `DEPOSIT` 또는 `BOTH` |
| UNLOAD | `ACQUIRE` 또는 `BOTH` |

`BOTH` 는 양쪽 모두 허용. 캐시에서 location 또는 station 을 찾지 못하면 검증을 건너뛰고 (이후 location 존재 확인 단계에서 별도로 잡힘).

- 위반 시 응답: `<ErrorCode>21</ErrorCode><ErrorMsg>DESTMACHINENOTFOUND</ErrorMsg>`.
- 위반 시 로그: `CreateTransportCommandActivity: UNLOAD dest station type mismatch - Dest=BUF03:LEFT, Station=BUF03, expected=ACQUIRE/BOTH, actual=DEPOSIT`.

## 검증 — source == dest 차단

`HostActivities.cs:413-421`. source/dest 문자열 결합 직후 실행.

- `source` 와 `dest` 가 대소문자 무시 동일하면 차단.
- 응답: `<ErrorCode>106</ErrorCode><ErrorMsg>SOURCEDESTMACHINEDUPLICATE</ErrorMsg>` (`ID_RESULT_SOURCEDESTMACHINE_DUPLICATE`).
- 로그: `CreateTransportCommandActivity: source and dest are identical - 192.168.1.103:LEFT`.

## ResolveZoneMatchedBuffer — 일반화된 자동 해석 함수

`HostActivities.cs:516-622`. LOAD/UNLOAD 가 공유하는 후보 검색 헬퍼.

```csharp
private static string ResolveZoneMatchedBuffer(
    AutofacContainerAccessor accessor, string anchorLoc, string anchorPort, string stationType)
```

- `anchorLoc:anchorPort` 를 zone 기준점으로 사용.
- 처리 단계 (각 단계 실패 시 단계별 사유를 `logger.Warn` 으로 출력하고 `null` 반환):
  1. `cache.GetLocationByLocationId(anchorKey)` → anchor location 조회.
  2. `cache.GetStationById(anchorLocation.StationId)` → anchor station 조회.
  3. `resource.GetLinkZonesByLinkId(anchorStation.LinkId)` → zone 결정.
  4. `resource.GetLocations()` 전체에서 `Type=BUFFER` + `Station.Type=stationType` + 동일 zone 인 후보 수집.
  5. `LocationId` 오름차순 정렬 후 첫 번째 후보의 `:` 앞부분 반환.

MES 로 가는 NACK 의 `<ErrorMsg>` 는 단일 `SOURCEMACHINENOTFOUND` (또는 UNLOAD 측은 `DESTMACHINENOTFOUND`) 으로 유지. 단계별 상세 사유는 로그에만 출력하여 운영자가 어디서 막혔는지 식별 가능.

## 시드 데이터 (`docker/init/01_init_acsdb.sql`)

자동 해석 후보가 잡히도록 추가된 행:

**NA_R_STATION** (1238-1241):
```
BUF01  N005_N006  ACQUIRE  0  LEFT
BUF02  N008_N009  ACQUIRE  0  LEFT
BUF03  N006_N007  DEPOSIT  0  LEFT
BUF04  N009_N010  DEPOSIT  0  LEFT
```

**NA_R_LOCATION** (1190-1193):
```
BUF01:LEFT  BUF01  BUFFER  MAGAZINE    LEFT  7
BUF02:LEFT  BUF02  BUFFER  MAGAZINE    LEFT  8
BUF03:LEFT  BUF03  BUFFER  MAGAZINE    LEFT  9
BUF04:LEFT  BUF04  BUFFER  MAGAZINE    LEFT  10
```

**Sequence 보정**:
```sql
SELECT pg_catalog.setval('public."NA_R_LOCATION_id_seq"', 10, true);
```

모든 BUF 행은 `NA_R_LINK_ZONE` 상 `zoneId=DEMO` 인 link 와 묶여 있어 기존 EQP (192.168.1.101~103) 와 zone 매칭이 가능.

## 에러 응답 매핑

| 상황 | ErrCode | ErrMsg |
|---|---|---|
| LOAD source 자동 해석 실패 (BUFFER+ACQUIRE 후보 0건) | `25` | `SOURCEMACHINENOTFOUND` |
| UNLOAD dest 자동 해석 실패 (BUFFER+DEPOSIT 후보 0건) | `21` | `DESTMACHINENOTFOUND` |
| LOAD 인데 MES.dest station 이 DEPOSIT/BOTH 아님 | `21` | `DESTMACHINENOTFOUND` |
| UNLOAD 인데 MES.dest station 이 ACQUIRE/BOTH 아님 | `21` | `DESTMACHINENOTFOUND` |
| source == dest | `106` | `SOURCEDESTMACHINEDUPLICATE` |
| JobID 중복 | `102` | `COMMANDALREADYREQUESTED` |
| source location 미조회 | `25` | `SOURCEMACHINENOTFOUND` |
| dest location 미조회 | `21` | `DESTMACHINENOTFOUND` |
| 같은 Bay 아님 | `22` | `NOTSAMEBAY` |
| 정상 | `0` | (빈값) |

에러 코드 상수는 `src/ACS/ACS.Core/Base/AbstractManager.cs` 의 `ID_RESULT_*` 튜플.

## 검증 시나리오

전제: ACS.App 빌드 + 재기동, 시드 적용 (`docker compose down -v && docker compose up -d` 또는 동등한 INSERT).

### A. LOAD 정상

요청:
```xml
<SourceLoc></SourceLoc><SourcePort></SourcePort>
<DestLoc>192.168.1.101</DestLoc><DestPort>LEFT</DestPort>
<ActionType>LOAD</ActionType><JobID>JOB-A</JobID>
```

기대 로그:
```
ResolveZoneMatchedBuffer(ACQUIRE): scan result - BUFFER=4, ACQUIRE=2, zoneMatch=2, candidates=2
ResolveZoneMatchedBuffer(ACQUIRE): chosen first candidate 'BUF01:LEFT' → 'BUF01'
CreateTransportCommandActivity: LOAD source auto-resolved - SourceLoc=BUF01, SourcePort=LEFT, Dest=192.168.1.101
CreateTransportCommandActivity: TransportCommand created - Id=JOB-A, Source=BUF01:LEFT, Dest=192.168.1.101:LEFT, BayId=DEMO, JobType=LOAD
```

JOBREPORT: `<ErrorCode>0</ErrorCode><ErrorMsg></ErrorMsg>`.

### B. UNLOAD 정상

요청:
```xml
<SourceLoc></SourceLoc><SourcePort></SourcePort>
<DestLoc>192.168.1.103</DestLoc><DestPort>LEFT</DestPort>
<ActionType>UNLOAD</ActionType><JobID>JOB-B</JobID>
```

기대 로그:
```
ResolveZoneMatchedBuffer(DEPOSIT): scan result - BUFFER=4, DEPOSIT=2, zoneMatch=2, candidates=2
ResolveZoneMatchedBuffer(DEPOSIT): chosen first candidate 'BUF03:LEFT' → 'BUF03'
CreateTransportCommandActivity: UNLOAD dest auto-resolved - Source=192.168.1.103:LEFT, DestLoc=BUF03, DestPort=LEFT
CreateTransportCommandActivity: TransportCommand created - Id=JOB-B, Source=192.168.1.103:LEFT, Dest=BUF03:LEFT, BayId=DEMO, JobType=UNLOAD
```

### C. station 타입 불일치 차단

요청: `ActionType=LOAD`, `DestLoc=BUF01`, `DestPort=LEFT` (BUF01 은 ACQUIRE station).

기대 로그:
```
CreateTransportCommandActivity: LOAD dest station type mismatch - Dest=BUF01:LEFT, Station=BUF01, expected=DEPOSIT/BOTH, actual=ACQUIRE
```

JOBREPORT: `<ErrorCode>21</ErrorCode><ErrorMsg>DESTMACHINENOTFOUND</ErrorMsg>`.

### D. source == dest 차단

자동 해석 결과가 anchor 와 동일한 location 으로 귀결되는 데이터 구성에서 발생. 정상 시드에서는 발생하지 않으나 비정상 데이터 진입 시 가드가 차단.

기대 로그: `CreateTransportCommandActivity: source and dest are identical - 192.168.1.103:LEFT`.

JOBREPORT: `<ErrorCode>106</ErrorCode><ErrorMsg>SOURCEDESTMACHINEDUPLICATE</ErrorMsg>`.

### E. 진단용 — 자동 해석 단계별 로그

`ResolveZoneMatchedBuffer` 가 null 반환 시 로그에서 단계별 사유 확인 가능:

| 로그 패턴 | 의미 |
|---|---|
| `anchorLoc empty` | MES.dest 가 비어있음 |
| `DI resolve failed (cache=...,resource=...)` | DI 미구성 |
| `NA_R_LOCATION miss '<key>'` | anchor location 캐시 미스 |
| `NA_R_STATION miss '<id>'` | anchor station 캐시 미스 |
| `Station '<id>' has no LinkId` | station.LinkId 비어있음 |
| `NA_R_LINK_ZONE miss for LinkId='<id>'` | linkzone 행 없음 |
| `LinkZone has empty ZoneId` | zoneId 비어있음 |
| `GetLocations returned null` | location 전체 조회 null |
| `no candidate in zone='<zoneId>' (BUFFER=<n>,<type>=<n>,zoneMatch=<n>)` | 후보 0건 |

## 관련 파일

| 경로 | 역할 |
|---|---|
| `src/ACS/ACS.Elsa/Activities/HostActivities.cs` | `CreateTransportCommandActivity` 본체, `ResolveZoneMatchedBuffer` 헬퍼 |
| `src/ACS/ACS.Core/Base/AbstractManager.cs` | 에러 코드 `ID_RESULT_*` 튜플 정의 |
| `src/ACS/ACS.Core/Path/Model/StationExs.cs` | `TYPE_ACQUIRE`, `TYPE_DEPOSITE`, `TYPE_BOTH` 상수 |
| `src/ACS/ACS.Core/Resource/Model/LocationExs.cs` | `LOCATION_TYPE_BUFFER` 등 상수 |
| `docker/init/01_init_acsdb.sql` | BUF01~BUF04 station + location 시드 |
| `src/ACS/ACS.App/Host/HostMessageService.cs` | JOBREPORT XML 빌드 (`AppendElement` → `<ErrorCode>`, `<ErrorMsg>`) |
| `src/ACS/ACS.Elsa/Workflows/Host/HostMoveCmdWorkflow.cs` | MOVECMD 워크플로우 정의, `CreateTransportCommandActivity` → `SendJobReportActivity` 와이어링 |