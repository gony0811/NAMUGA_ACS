# NAMUGA ACS — EXCHANGE(v2) 구현 사양서

> 구현 주체(개발자 / Claude Code)가 직접 착수할 수 있는 수준의 상세 설계.
> 상위 인터페이스 정의는 `EXCHANGE_통신_인터페이스_사양서.md` 참조. 본 문서는 **ACS 측 구현**만 다룬다.
> 작성일: 2026-07-16 · 기준 레포: `NAMUGA_ACS` (main)

---

## 0. 확정 결정 (변경 시 본 문서 전체 재검토)

| # | 결정 | 내용 |
|---|---|---|
| D1 | 데이터 모델 | **1 EXCHANGE = 1 TransportCommand** (Origin→Mid→Dest 3-waypoint). 2-TC 분해 방식 폐기 |
| D2 | 배칭 | **v2 일괄 개발**: 트립당 최대 2 EXCHANGE. 같은 Bay 한정, **대기창 없음** — 스케줄 틱 시점에 2건 가능하면 2건, 아니면 1건 |
| D3 | 슬롯 | AMR 슬롯 4개. **1·2 = 투입(INSERT), 3·4 = 회수(RETRIEVE)**. 교환A=슬롯1+3, 교환B=슬롯2+4 |
| D4 | 기존 흐름 | 기존 MOVECMD/충전/일반 반송 코드는 **무수정**. EXCHANGE 는 병렬 신규 경로 |
| D5 | 스케줄러 배제 | EXCHANGE TC 는 `state=EXCHANGE_QUEUED` 로 insert → 기존 `State="QUEUED"` 쿼리에 자연 배제 |
| D6 | DB | 기존 테이블 무수정. **신규 테이블 `NA_R_VEHICLE_SLOT` 1개만 추가** (additive) |
| D7 | eqpId 관례 | `eqpId=AcsId`, `portId=NULL` (기존 `CreateTransportCommandActivity` 관례 유지). 설비는 `midLoc/midPortId` 에 저장 |
| D8 | 상관키 | `EXCHANGECMD.JobID` = `TC.jobId` = 모든 JOBREPORT `JobID`. 트립 묶음은 `additionalInfo` 의 `TRIP=<tripId>` |

| D9 | 반송 순서 | **설비 먼저, 반납 나중** (픽업→설비들→반납들 고정 순서). 동시 보유 최대 3개 허용 (2026-07-22 확정) |
| D10 | 슬롯 지정 | **ACS 자동배정**. EXCHANGECMD 의 LoadCarrierSlot/UnloadCarrierSlot 은 공백 허용(무시), 사용 슬롯은 JOBREPORT CarrierSlot 으로 통보 (2026-07-22 확정) |
| D11 | 설비 준비신호 | **기존 ACTIONCMD 재사용** (Type=UNLOAD/LOAD, JobId). §4.6 가정 그대로 확정 — TBD 해소 (2026-07-22) |
| D12 | 픽업 후 실패 | 자동 보상 이동 없음 — **수동 정리** (2026-07-20 확정) |
| D13 | JOBCANCEL | **EXCHANGE·일반 MOVECMD 공통 사양** (2026-07-28 확정). MES→ACS 기존 XML 포맷(`<Command>JOBCANCEL</Command>`, JobID). 판정: C1 배차전(QUEUED/EXCHANGE_QUEUED) / C2 픽업전(ASSIGNED) = **즉시 취소** · C3 **적재 후 전 구간** = **충전소 복귀 + Job 삭제(이력 이관) + 차량 ALARM → 작업자 조치 대기** · C4 종료상태 = 거부(CANCEL_REJECTED) · C5 [EXCHANGE] 배칭 중 1건(적재 후) = 반송 전체 중단, 페어 Job 종결 통보. 적재 판정: EXCHANGE=슬롯 점유 / MOVECMD=`FullState=FULL`. **기존 `CancelTransportCommandActivity`(무조건 CANCELED)를 본 판정 로직으로 대체 — D4(기존 코드 무수정)의 승인된 예외.** 충전소 복귀는 CHARGEMOVE 재사용 |

용어 규약(고객 표준): 트립/투어 → **반송**, "10초 틱" → **스케줄링 트리거**, 배칭 판정 → **배차(JOB ASSIGN) 판정**. 코드 주석·로그 문구도 이 용어를 따른다.

---

## 1. 아키텍처 개요

```
[HS: Host 프로세스]
  HostExchangeCmdWorkflow (신규, DefinitionId=HOST-EXCHANGECMD)
    ├ ParseExchangeCmdActivity        : XML → ExchangeCmdModel
    ├ ValidateExchangeCmdActivity     : 위치/슬롯/중복/Bay 검증 → NACK
    ├ CreateExchangeTransportCommandActivity : 1-TC insert (EXCHANGE_QUEUED)
    └ SendJobReportActivity(RECEIVE, Step=10)

[Daemon 프로세스]
  AwakeExchangeJob (신규, 10초) → "SCHEDULE-EXCHANGEJOB" 발행 (bay 별)

[TS: Trans 프로세스]
  ScheduleExchangeJobWorkflow (신규, DefinitionId=SCHEDULE-EXCHANGEJOB)
    ├ GetExchangeQueuedCommandsActivity   : EXCHANGE_QUEUED 조회
    ├ FindBatchCandidateActivity          : 같은 Bay 2건 페어링 평가 (대기창 없음)
    ├ FindVehicleForExchangeActivity      : 4슬롯 EMPTY AMR 탐색
    ├ AssignExchangeTripActivity          : TRIP 발급 + 슬롯 배정 + 원자 할당
    └ StartExchangeTourActivity           : 첫 moveCmd + JOBREPORT(START)

  ExchangeCoordinator (신규 액티비티 군, 이벤트 구동)
    ├ 진입점 1: RailVehicleDestArrivedWorkflow 분기 (jobType=EXCHANGE)
    ├ 진입점 2: RailVehicleAcquireCompleted / DepositCompleted 분기 (EXCHANGE)
    ├ 진입점 3: TransActioncmdWorkflow 분기 (EXCHANGE 게이팅)
    └ ExchangeTourAdvanceActivity : STEP 전진 + 다음 명령 발행 + JOBREPORT

[EI: Trans-EI 프로세스]
  기존 RAIL-CARRIERTRANSFER → moveCmd 변환에 amrSlot/stage 필드 추가 (additive)
  신규 RAIL-ACTIONCMD → actionCmd 변환
```

원칙: **기존 워크플로 파일은 "EXCHANGE 분기 추가" 외에 수정하지 않는다.** 신규 로직은 전부 신규 파일(`ExchangeActivities.cs`, `*ExchangeWorkflow.cs`)에 둔다.

---

## 2. 데이터 모델

### 2.1 NA_T_TRANSPORTCMD 사용 규약 (스키마 무변경)

EXCHANGECMD 예시(JobID `EX20260706103000123`, 설비 `192.168.32.36:RIGHT`, `LoadSourceLoc=IN_BUF_01`, `UnloadDestLoc=OUT_BUF_01`) 기준 insert 스냅샷:

| 컬럼 | 값 | 비고 |
|---|---|---|
| `jobId` | `EX20260706103000123` | 원본 JobID 그대로 (접미사 없음) |
| `state` | `EXCHANGE_QUEUED` | 신규 상태값 (varchar 20 내, 14자) |
| `jobType` | `EXCHANGE` | `TransportCommandEx.JOBTYPE_EXCHANGE` 상수 신설 |
| `source` | `IN_BUF_01:LEFT` | ① 신규 픽업 (Loc:Port 결합, 기존 관례) |
| `originLoc` | `IN_BUF_01:LEFT` | source 와 동일 값 (여정 원점 명시) |
| `midLoc` / `midPortId` | `192.168.32.36` / `RIGHT` | ② 설비 (교환 지점) |
| `dest` | `OUT_BUF_01:LEFT` | ③ 기존 반납 |
| `additionalInfo` | `STEP=10;TRIP=;LOADSLOT=;UNLOADSLOT=;EQJOB_L=PRD-..._LOAD_...;EQJOB_U=PRD-..._UNLOAD_...` | §2.2 규약 |
| `eqpId` / `portId` | `ACS01` / `NULL` | D7 관례 |
| `description` | `MODEL='CF203W';MAGAZINE` | 기존 `GetModel()` 정규식 호환 |
| `priority` | `3` | DEFAULT_PRIORITY |
| `bayId` | 설비 Station 의 Bay | 검증 단계에서 해석 |
| `vehicleId`, `path`, 시간필드 | `NULL` | 진행하며 채움 |

### 2.2 additionalInfo 키-값 규약

포맷: `KEY=VALUE;KEY=VALUE;...` (세미콜론 구분, 값 내 세미콜론 금지). 파서/빌더는 공용 헬퍼로 구현한다 (§4.2).

| 키 | 값 | 기록 시점 |
|---|---|---|
| `STEP` | `10/20/30/40/50/60` | 코디네이터가 단계 전진 시마다 갱신 — **crash 복구의 근거** |
| `TRIP` | 트립 ID (`TRIP20260706103010`) | 배차 시 |
| `LOADSLOT` | `1` 또는 `2` | 배차 시 (투입 슬롯) |
| `UNLOADSLOT` | `3` 또는 `4` | 배차 시 (회수 슬롯) |
| `EQJOB_L` / `EQJOB_U` | 설비 LOAD/UNLOAD 보고용 JobID — **ACS 로직 미사용, 저장만(추적용)**. EXCHANGECMD 필드는 Optional·공백 허용 (2026-07-29 확정) | insert 시 |

### 2.3 신규 테이블 NA_R_VEHICLE_SLOT (유일한 DDL)

```sql
CREATE TABLE public."NA_R_VEHICLE_SLOT" (
    id            bigint NOT NULL,
    "vehicleId"   character varying(64) NOT NULL,
    "slotNo"      integer NOT NULL,              -- 1~4
    role          character varying(10) NOT NULL, -- INSERT | RETRIEVE
    state         character varying(10) NOT NULL, -- EMPTY | OCCUPIED
    "jobId"       character varying(256),         -- 점유한 EXCHANGE JobID
    phase         character varying(5),           -- NEW | OLD
    "updatedTime" timestamp with time zone NOT NULL
);
ALTER TABLE ONLY public."NA_R_VEHICLE_SLOT"
    ADD CONSTRAINT "NA_R_VEHICLE_SLOT_pkey" PRIMARY KEY (id);
CREATE UNIQUE INDEX "IX_VEHICLE_SLOT_VEH_NO" ON public."NA_R_VEHICLE_SLOT" ("vehicleId","slotNo");
CREATE SEQUENCE public."NA_R_VEHICLE_SLOT_id_seq" ...;  -- 기존 시퀀스 관례 동일
```

시드: EXCHANGE 대응 차량마다 4행(slotNo 1~4, role 1·2=INSERT / 3·4=RETRIEVE, state=EMPTY).

### 2.4 상태 정의

**TC 상태 (EXCHANGE 전용 라이프사이클)** — 기존 상수 재사용 + 신규 1개:

```
EXCHANGE_QUEUED → ASSIGNED → TRANSFERRING_SOURCE → TRANSFERRING_DEST → COMPLETED
      (신규)      (배차)      (Origin~Mid 구간)      (Mid~Dest 구간)      (60 보고 후)
```

굵은 상태 전이는 기존 상수(`STATE_ASSIGNED` 등)를 그대로 쓰고, 세밀한 진행은 `additionalInfo.STEP`(10~60)이 담당한다. 이렇게 하면 기존 UI/이력/stuck 복구가 큰 개조 없이 EXCHANGE TC 를 표시·추적할 수 있다.

**슬롯 상태**: `EMPTY ↔ OCCUPIED`. 전이는 §4.7 SlotManager 만 수행 (단일 진입점).

**STEP ↔ TC 상태 ↔ 슬롯 이벤트 매핑**:

| STEP | 의미 | TC.state | 슬롯 변화 |
|---|---|---|---|
| 10 | 접수 (RECEIVE) | EXCHANGE_QUEUED | — |
| (START) | 배차·출발 | ASSIGNED | LOADSLOT/UNLOADSLOT 예약 기록 |
| — | Origin 픽업 완료 | TRANSFERRING_SOURCE | LOADSLOT ← OCCUPIED(NEW) |
| 20 | Mid(설비) 도착 (ARRIVED) | TRANSFERRING_SOURCE | — |
| 30 | OLD 취출 완료 | TRANSFERRING_SOURCE | UNLOADSLOT ← OCCUPIED(OLD) |
| 40 | NEW 투입 완료 | TRANSFERRING_DEST | LOADSLOT ← EMPTY |
| 50 | OLD 반납 완료 | TRANSFERRING_DEST | UNLOADSLOT ← EMPTY |
| 60 | 전체 완료 (COMPLETE) | COMPLETED → 이력 이관 | (모두 EMPTY 확인) |

---

## 3. 신규/수정 파일 총괄

| 구분 | 파일 | 신규/수정 | 내용 |
|---|---|---|---|
| Core | `ACS.Core/Transfer/Model/TransportCommandEx.cs` | 수정(상수만) | `STATE_EXCHANGE_QUEUED`, `JOBTYPE_EXCHANGE` 상수 추가 |
| Core | `ACS.Core/Resource/Model/VehicleSlotEx.cs` | **신규** | 슬롯 엔티티 + 상수 |
| Core | `ACS.Core/Transfer/ExchangeInfo.cs` | **신규** | additionalInfo 파서/빌더 헬퍼 |
| Manager | `ACS.Manager/Resource/SlotManagerImplement.cs` + `ISlotManagerEx` | **신규** | 슬롯 CRUD·점유 전이 (단일 진입점) |
| Manager | `ACS.Manager/Transfer/TransferManagerExImplement.cs` | 수정(추가만) | `GetExchangeQueuedTransportCommandsByBayId()` 등 조회 메서드 추가 |
| App | `ACS.App/Database/AcsDbContext.cs` | 수정(추가만) | `VehicleSlotEx` 매핑 |
| App | `ACS.App/Scheduling/Awake/AwakeExchangeJob.cs` | **신규** | SCHEDULE-EXCHANGEJOB 트리거 |
| App | `ACS.App/Modules/SchedulingModule.cs` | 수정(등록만) | AwakeExchangeJob HostedService 등록 |
| Elsa | `ACS.Elsa/Activities/HostExchangeActivities.cs` | **신규** | HS 액티비티 군 (§4.1~4.5) |
| Elsa | `ACS.Elsa/Activities/ExchangeActivities.cs` | **신규** | TS 배차·코디네이터 액티비티 군 (§4.7~4.13) |
| Elsa | `ACS.Elsa/Workflows/Host/HostExchangeCmdWorkflow.cs` | **신규** | HOST-EXCHANGECMD |
| Elsa | `ACS.Elsa/Workflows/Trans/ScheduleExchangeJobWorkflow.cs` | **신규** | SCHEDULE-EXCHANGEJOB |
| Elsa | 기존 `RailVehicleDestArrived/AcquireCompleted/DepositCompleted/TransActioncmd` 워크플로 | 수정(분기만) | 첫머리에 `jobType==EXCHANGE → Exchange 코디네이터로 위임` 분기 삽입 |
| Comm | `ACS.Communication/Mqtt/Model/RailCarrierTransferMessage.cs` | 수정(필드 추가) | `amrSlot`, `stage` (additive JSON) |
| Comm | `ACS.Communication/Mqtt/Model/RailActionCmdMessage.cs` | **신규** | RAIL-ACTIONCMD 모델 |
| Manager | `ACS.Manager/Message/MessageManagerExImplement.cs` | 수정(추가만) | `SendActionCmdJson()`, JOBREPORT 확장 오버로드 |
| docker | `docker/init/01_init_acsdb.sql` | 수정(추가만) | NA_R_VEHICLE_SLOT DDL + 시드 |

"수정" 파일은 전부 **추가(additive) 변경**이다 — 기존 라인 변경/삭제 없음. 이것이 회귀 방지의 핵심 규율.

---

## 4. 구현 항목별 상세 계획

각 항목: **목적 → 대상 파일 → 로직 상세 → 완료 기준(DoD)**. 항목 번호는 공수 산정서(`EXCHANGE_개발공수_산정.xlsx`)의 No 와 일치한다.

### 4.1 [항목1] EXCHANGECMD 파서 — `ParseExchangeCmdActivity`

**목적**: EXCHANGECMD XML 을 강타입 모델로 변환한다.

**파일**: `ACS.Elsa/Activities/HostExchangeActivities.cs` (신규)

**로직**:
- 기존 `CreateTransportCommandActivity` 의 `ExtractValue(xml, "//DataLayer/필드") ?? ExtractValue(xml, "//필드") ?? ""` 이중 fallback 패턴을 그대로 사용.
- 추출 필드: `AcsId, JobID, EquipID, Port, Model, LoadEquipJobID, UnloadEquipJobID, LoadSourceLoc, UnloadDestLoc, LoadCarrierSlot, UnloadCarrierSlot, MaterialType, ActionType, UserID`.
- 출력: `ExchangeCmdModel` (신규 record/class, 위 필드 그대로) + `ReplySubject` (Header 에서 추출, JOBREPORT 응답 라우팅용).
- `ActionType != "EXCHANGE"` 이면 즉시 실패 출력 (워크플로 라우팅 오류 방어).

**DoD**: 인터페이스 사양서 §4.1 의 예시 XML 을 넣으면 14개 필드가 정확히 추출된다. 필드 누락 시 빈 문자열(예외 없음).

### 4.2 [항목2] 1-TC 3-waypoint 생성 — `CreateExchangeTransportCommandActivity` + `ExchangeInfo`

**목적**: 검증 통과한 ExchangeCmdModel 로 `NA_T_TRANSPORTCMD` 1행을 §2.1 스냅샷대로 insert.

**파일**: 액티비티는 `HostExchangeActivities.cs`, 헬퍼는 `ACS.Core/Transfer/ExchangeInfo.cs` (신규).

**ExchangeInfo 헬퍼** (additionalInfo 규약의 단일 구현 — 문자열 조작을 코드 곳곳에 흩뿌리지 않는다):

```csharp
public static class ExchangeInfo
{
    public static Dictionary<string,string> Parse(string additionalInfo);
    public static string Build(Dictionary<string,string> map);
    public static string Get(string additionalInfo, string key);        // 없으면 ""
    public static string Set(string additionalInfo, string key, string value); // 갱신된 문자열 반환
}
```

**생성 로직**:
1. `Source`/`OriginLoc` = `LoadSourceLoc` (+`:LEFT` 등 포트 보정 — 기존 `ResolveMissingPortByLocPrefix` 재사용 가능).
2. `MidLoc`=`EquipID`, `MidPortId`=`Port`, `Dest`=`UnloadDestLoc`(+포트 보정).
3. `State=STATE_EXCHANGE_QUEUED`, `JobType=JOBTYPE_EXCHANGE`, `EqpId=AcsId`, `Description=$"MODEL='{Model}';{MaterialType}"`.
4. `AdditionalInfo = ExchangeInfo.Build({STEP:"10", EQJOB_L:..., EQJOB_U:..., TRIP:"", LOADSLOT:"", UNLOADSLOT:""})`.
5. `transferManager.CreateTransportCommand(tc)` — 기존 매니저 메서드 그대로.

**상수 추가** (`TransportCommandEx.cs`, 추가만):

```csharp
public static String STATE_EXCHANGE_QUEUED = "EXCHANGE_QUEUED";  // 14자 < varchar(20)
public static String JOBTYPE_EXCHANGE = "EXCHANGE";
```

**DoD**: insert 후 DB 행이 §2.1 표와 일치. 기존 `GetQueuedTransportCommands()`(State="QUEUED" 조회)에 **잡히지 않음**을 테스트로 증명.

### 4.3 [항목3] EXCHANGE 검증 — `ValidateExchangeCmdActivity`

**목적**: 잘못된 요청을 TC 생성 전에 NACK 로 차단.

**파일**: `HostExchangeActivities.cs`

**검증 순서와 에러 매핑** (기존 `ID_RESULT_*` 코드 재사용, `AbstractManager.cs`):

| # | 검증 | 실패 시 ErrorCode/Msg |
|---|---|---|
| 1 | JobID 중복 (`ExistTransportCommand`) | `102 COMMANDALREADYREQUESTED` |
| 2 | `LoadSourceLoc` Location 존재 (cache 조회) | `25 SOURCEMACHINENOTFOUND` |
| 3 | `EquipID:Port` Location 존재 + Station 타입이 EXCHANGE 가능(EQP) | `21 DESTMACHINENOTFOUND` |
| 4 | `UnloadDestLoc` Location 존재 | `21 DESTMACHINENOTFOUND` |
| 5 | 세 위치가 공통 Bay (`bayId` 해석 겸용) | `22 NOTSAMEBAY` |
| 6 | 슬롯 역할: `LoadCarrierSlot∈{1,2}`, `UnloadCarrierSlot∈{3,4}` 또는 공백(자동배정) | `106` 계열 신규 사유 로그 + NACK |
| 7 | Origin==Mid, Mid==Dest 등 위치 중복 | `106 SOURCEDESTMACHINEDUPLICATE` |

- 실패 시 JOBREPORT 에 `<ErrorCode>/<ErrorMsg>` 채워 즉시 응답, TC 미생성 (기존 MOVECMD NACK 패턴과 동일).
- 검증 성공 시 `bayId` 를 출력 변수로 전달.

**DoD**: 7개 케이스 각각 단위 테스트. 정상 케이스에서 `bayId` 가 설비 Station 의 Bay 로 해석된다.

### 4.4 [항목4] HOST-EXCHANGECMD 워크플로

**목적**: HS 진입점. 기존 `HostMoveCmdWorkflow` 와 동일한 골격.

**파일**: `ACS.Elsa/Workflows/Host/HostExchangeCmdWorkflow.cs` (신규), `DefinitionId="HOST-EXCHANGECMD"`.

**구조**:

```
Sequence:
  ParseExchangeCmdActivity → (실패: SendJobReport NACK, 종료)
  ValidateExchangeCmdActivity → (실패: SendJobReport NACK, 종료)
  CreateExchangeTransportCommandActivity
  SendExchangeJobReportActivity(Type=RECEIVE, Step=10, StepName=PICKUP_NEW, ErrorCode=0)
```

- 라우팅: HS 의 XML 디스패처가 `<Command>EXCHANGECMD</Command>` 를 DefinitionId 로 매핑하도록 기존 MOVECMD 등록부와 같은 위치에 등록 (등록부만 추가, 기존 라인 무수정).

**DoD**: EXCHANGECMD 송신 → DB 1행 + JOBREPORT(RECEIVE,10) 회신을 통합 환경에서 확인.

### 4.5 [항목5] JOBREPORT 확장 — `SendExchangeJobReportActivity`

**목적**: `Step/StepName/CarrierSlot` 을 포함하는 EXCHANGE 전용 JOBREPORT 빌더. **기존 JOBREPORT 빌더는 건드리지 않는다** (기존 MOVECMD 보고 회귀 방지).

**파일**: `HostExchangeActivities.cs` + `MessageManagerExImplement.cs` 에 전송 오버로드 추가.

**XML 산출** (인터페이스 사양서 §7.1 준수):

```xml
<Msg><Command>JOBREPORT</Command>
  <Header><DestSubject>{ReplySubject}</DestSubject><ReplySubject>/HQ/{AcsId}</ReplySubject></Header>
  <DataLayer>
    <AcsId/><Type/><Step/><StepName/><JobID/><ActionType/>
    <CarrierSlot/>   <!-- 슬롯 번호 1~4, 해당 시에만 -->
    <MaterialType/><UserID/><ErrorCode/><ErrorMsg/>
  </DataLayer></Msg>
```

**전송 API** (추가 오버로드):

```csharp
void SendExchangeJobReportToHost(string type, string jobId, string vehicleId,
    string step, string stepName, string actionType, string carrierSlot,
    string errCode, string errMsg);
```

TS 코디네이터가 이 API 를 호출하면 HS 의 `HostJobReportWorkflow` 경유로 MES 에 송신된다 (기존 JOBREPORT 전달 경로 재사용 — `vehicleabnormal.md` 의 OPERATOR_ABORT JOBREPORT 와 같은 경로).

**Step 값 매핑표** (코디네이터가 사용, 상수화):

| Type | Step | StepName | ActionType | CarrierSlot |
|---|---|---|---|---|
| RECEIVE | 10 | PICKUP_NEW | EXCHANGE | — |
| START | 10 | PICKUP_NEW | EXCHANGE | — (RECEIVE 와 동일 Step — Type 으로 구분, 2026-07-29 확정) |
| ARRIVED | 20 | MOVE_TO_EQUIP | EXCHANGE | — |
| STEP_COMPLETE | 30 | UNLOAD_OLD | UNLOAD | UNLOADSLOT(3|4) |
| STEP_COMPLETE | 40 | LOAD_NEW | LOAD | LOADSLOT(1|2) |
| STEP_COMPLETE | 50 | RETURN_OLD | MOVE | UNLOADSLOT(3|4) |
| COMPLETE | 60 | DONE | EXCHANGE | — |

**DoD**: 7종 보고 각각의 XML 스냅샷 테스트. 기존 `SendJobReportToHost`(MOVECMD 용) 호출부는 diff 0.

### 4.6 [항목6] ACTIONCMD 라우팅 (확정 D11 — 기존 ACTIONCMD 재사용)

**목적**: 설비 준비신호가 MES→ACS 로 도달했을 때, 해당 EXCHANGE TC 와 단계(취출/투입)를 식별해 코디네이터에 전달.

**파일**: 기존 `TransActioncmdWorkflow.cs` 첫머리에 분기 1개 추가 + 실제 처리는 `ExchangeActivities.cs` 의 `RouteExchangeActionCmdActivity`.

**로직** (ACTIONCMD 재사용 가정):
1. `ACTIONCMD.JobId` 로 TC 조회. `tc.JobType != EXCHANGE` → 기존 경로로 그대로 통과 (무간섭).
2. EXCHANGE 이면: `ACTIONCMD.Type` 판독 — `UNLOAD` = OLD 취출 허가, `LOAD` = NEW 투입 허가.
3. 게이팅 검증: `Type=UNLOAD` 는 `STEP=20` 상태에서만, `Type=LOAD` 는 `STEP=30` 에서만 유효. 어긋나면 WARN 로그 + NACK (순서 위반 방어 — 설비가 투입 요청을 먼저 보내는 이상 케이스 차단).
4. 유효하면 `SendActionCmdJson()` 으로 EI 에 RAIL-ACTIONCMD 발행 (§4.12).

**DoD**: STEP 상태별 허용/거부 매트릭스 테스트 (20→UNLOAD 허용, 20→LOAD 거부, 30→LOAD 허용 등). TBD 확정 시 이 액티비티의 입력 파싱부만 교체.

### 4.7 [항목7] 슬롯 점유 모델 — `VehicleSlotEx` + `ISlotManagerEx`

**목적**: 슬롯 1~4 의 점유를 영속 추적. **모든 슬롯 전이는 SlotManager 한 곳으로만** — 점유 갱신을 액티비티마다 직접 DAO 로 하면 정합이 깨진다.

**파일**: `ACS.Core/Resource/Model/VehicleSlotEx.cs`, `ACS.Manager/Resource/SlotManagerImplement.cs`, `AcsDbContext.cs` 매핑 추가, DI 등록(`CoreModule` 또는 `ResourceModule` 패턴).

**엔티티**:

```csharp
public class VehicleSlotEx
{
    public static string ROLE_INSERT = "INSERT";     // slotNo 1,2
    public static string ROLE_RETRIEVE = "RETRIEVE"; // slotNo 3,4
    public static string STATE_EMPTY = "EMPTY";
    public static string STATE_OCCUPIED = "OCCUPIED";
    public static string PHASE_NEW = "NEW";
    public static string PHASE_OLD = "OLD";
    public virtual long Id { get; set; }
    public virtual string VehicleId { get; set; }
    public virtual int SlotNo { get; set; }
    public virtual string Role { get; set; }
    public virtual string State { get; set; }
    public virtual string JobId { get; set; }
    public virtual string Phase { get; set; }
    public virtual DateTime UpdatedTime { get; set; }
}
```

**매니저 인터페이스**:

```csharp
public interface ISlotManagerEx
{
    IList<VehicleSlotEx> GetSlots(string vehicleId);
    bool AreAllSlotsEmpty(string vehicleId);                       // 배차 적격 판정
    (int loadSlot, int unloadSlot)? ReserveExchangePair(string vehicleId, string jobId);
        // INSERT 군에서 빈 슬롯 1개 + RETRIEVE 군에서 빈 슬롯 1개를 골라 jobId 로 예약.
        // 둘 중 하나라도 없으면 null (예약 자체가 원자적이어야 함 — 동일 트랜잭션)
    void Occupy(string vehicleId, int slotNo, string jobId, string phase);  // EMPTY→OCCUPIED
    void Release(string vehicleId, int slotNo);                             // OCCUPIED→EMPTY
    void ReleaseAllByJobId(string jobId);                                   // 실패/취소 정리
}
```

**갱신 규율**: `UpdatedTime` 은 `DateTime.UtcNow` (memory.md 의 UTC 교훈 준수). 모든 전이에 message name 을 로그로 남긴다.

**DoD**: Reserve→Occupy→Release 전이 단위 테스트. 4슬롯 중 INSERT 만 다 찬 경우 `ReserveExchangePair`=null. EF 매핑으로 재기동 후 상태 재조회 일치.

### 4.8 [항목8] EXCHANGE 디스패처 — `ScheduleExchangeJobWorkflow`

**목적**: `EXCHANGE_QUEUED` TC 를 같은 Bay 기회주의 배칭으로 AMR 에 배차. **기존 SCHEDULE-QUEUEJOB 은 무수정.**

**파일**: `ACS.App/Scheduling/Awake/AwakeExchangeJob.cs` (신규, `AwakeQueueTransportJob` 을 그대로 본떠 10초 주기·bay 별 `SCHEDULE-EXCHANGEJOB` 발행), `ACS.Elsa/Workflows/Trans/ScheduleExchangeJobWorkflow.cs` (신규), 조회 메서드는 `TransferManagerExImplement.cs` 에 추가:

```csharp
public IList GetExchangeQueuedTransportCommandsByBayId(string bayId)
    // State="EXCHANGE_QUEUED" AND bayId, priority DESC → createdTime ASC 정렬
```

**워크플로 시퀀스** (의사코드):

```
queued = GetExchangeQueuedCommandsActivity(bayId)      // 정렬된 목록
while (queued.Count > 0):
    tcA = queued[0]
    tcB = queued.Count > 1 ? queued[1] : null          // 같은 Bay 는 조회 조건이 보장
    vehicle = FindVehicleForExchangeActivity(tcA)       // §4.11
    if vehicle == null: break                           // 이번 틱 종료 (차량 없음)
    batch = (tcB != null) ? [tcA, tcB] : [tcA]          // 대기창 없음: 있으면 2건, 없으면 1건
    ok = AssignExchangeTripActivity(vehicle, batch)     // §4.9 — 원자
    if !ok: break
    StartExchangeTourActivity(vehicle, batch)           // §4.13 첫 명령 + START 보고
    queued.RemoveRange(batch)
```

- 멱등성: `state=EXCHANGE_QUEUED` 인 것만 조회하므로 이미 ASSIGNED 된 트립은 다음 틱에 자연 배제.
- 경합: 할당은 §4.9 의 단일 트랜잭션. 10초 틱이 겹쳐도 첫 트랜잭션만 성공.

**DoD**: (a) 큐 2건+차량 1대 → 1트립 2건 배칭, (b) 큐 1건 → 즉시 단독 출발(대기 없음), (c) 큐 3건 → 2건 배칭 + 1건은 다음 차량/틱, (d) 차량 없음 → 큐 유지. 4케이스 통합 테스트.

### 4.9 [항목9] 슬롯 할당 + TRIP 배정 — `AssignExchangeTripActivity`

**목적**: 트립 단위 원자 할당. 부분 할당 절대 금지.

**파일**: `ExchangeActivities.cs`

**로직** (전부 하나의 DB 트랜잭션 — `EfCorePersistentDao` 트랜잭션 패턴 사용):

```
tripId = "TRIP" + yyyyMMddHHmmssfff
foreach tc in batch:                       // 1건 또는 2건
    pair = slotManager.ReserveExchangePair(vehicle.VehicleId, tc.JobId)
    if pair == null → 전체 rollback, return false
    tc.State = STATE_ASSIGNED
    tc.VehicleId = vehicle.VehicleId
    tc.AssignedTime = now
    tc.AdditionalInfo = ExchangeInfo.Set(... TRIP=tripId, LOADSLOT=pair.load, UNLOADSLOT=pair.unload)
    transferManager.UpdateTransportCommand(tc)
vehicle 갱신:
    UpdateVehicleTransportCommandId(vehicle, tripId)    // TC 하나가 아니라 "트립 ID" 를 가리킴
    UpdateVehicleTransferState(vehicle, TRANSFERSTATE_ASSIGNED)
    UpdateVehicleProcessingState(vehicle, PROCESSINGSTATE_RUN)
    UpdateVehicleAcsDestNodeId(vehicle, 첫 목적지(Origin) StationId, "SCHEDULE-EXCHANGEJOB")
```

**핵심 설계 결정 — `vehicle.transportCommandId = tripId`**: 기존 코드는 이 필드로 "차량의 현재 작업"을 찾는다. EXCHANGE 트립은 TC 가 2개일 수 있으므로 tripId 를 넣고, 트립→TC 목록 해석은 `additionalInfo.TRIP` 역조회 헬퍼(`GetTransportCommandsByTripId`, TransferManager 에 추가)로 한다. **주의**: 기존 stuck 복구(`RecoverStuckVehiclesActivity`)는 이 필드를 TC JobId 로 조회하는데, tripId 조회는 실패 → WARN "TC 없음" 로그가 나온다. §4.14 에서 EXCHANGE 분기를 추가해 처리한다 (기존 로직 무수정, 분기 추가).

**DoD**: 2건 배칭 시 슬롯 1·3(A교환)/2·4(B교환) 배정 확인. `ReserveExchangePair` 실패 주입 시 TC·슬롯·차량 모두 원상복구(트랜잭션 롤백) 확인.

### 4.10 [항목10] EXCHANGE 코디네이터 / 다구간 투어 — `ExchangeTourAdvanceActivity`

**목적**: 트립의 유일한 두뇌. AMR 이벤트를 받아 STEP 를 전진시키고 다음 명령·보고를 발행한다. **모든 전진은 이 액티비티 하나로 수렴** — 분산시키지 않는다.

**파일**: `ExchangeActivities.cs`. 진입점은 기존 이벤트 워크플로 3곳의 EXCHANGE 분기:

| 진입점 (기존 워크플로) | EXCHANGE 에서의 의미 |
|---|---|
| `RailVehicleDestArrivedWorkflow` | waypoint 도착 (Origin/Mid/Dest) |
| `RailVehicleAcquireCompletedWorkflow` | 픽업 완료 (Origin 신규픽업 / Mid OLD취출) |
| `RailVehicleDepositCompletedWorkflow` | 하치 완료 (Mid NEW투입 / Dest OLD반납) |

분기 코드는 각 워크플로 첫머리에 동일 패턴 (기존 로직 무수정):

```csharp
// vehicle.TransportCommandId 가 "TRIP" prefix → EXCHANGE 트립
if (vehicle.TransportCommandId?.StartsWith("TRIP") == true)
    → ExchangeTourAdvanceActivity 로 위임 후 종료
// 아니면 기존 로직 그대로
```

**투어 상태 기계** — 트립 내 TC 들의 `STEP` 조합이 곧 투어 상태다. 단일 교환 기준 전이표:

| 현재 상태 | 수신 이벤트 | 액션 (순서 보장) | 다음 |
|---|---|---|---|
| ASSIGNED, STEP=10 | Origin 도착 | acquire 지시는 moveCmd 에 내장(AMR 자동) — 대기 | STEP=10 |
| STEP=10 | AcquireCompleted(Origin) | ① Occupy(LOADSLOT, NEW) ② tc.State=TRANSFERRING_SOURCE ③ moveCmd(Mid, jobType=EXCHANGE, portType=EQP) ④ AcsDestNodeId=Mid | STEP=10→(이동) |
| (이동중) | Mid 도착 | ① STEP=20 ② JOBREPORT(ARRIVED,20) — 이후 ACTIONCMD 대기 (§4.6 게이팅) | STEP=20 |
| STEP=20 | AcquireCompleted(Mid) ← actionCmd(UNLOAD) 결과 | ① Occupy(UNLOADSLOT, OLD) ② STEP=30 ③ JOBREPORT(STEP_COMPLETE,30,UNLOAD,slot) | STEP=30 |
| STEP=30 | DepositCompleted(Mid) ← actionCmd(LOAD) 결과 | ① Release(LOADSLOT) ② STEP=40 ③ JOBREPORT(STEP_COMPLETE,40,LOAD,slot) ④ tc.State=TRANSFERRING_DEST ⑤ 다음 목적지 이동 명령 (§4.13) | STEP=40 |
| STEP=40 | Dest 도착 | deposit 은 moveCmd(UNLOAD, portType=BUFFER) 에 내장 — 대기 | STEP=40 |
| STEP=40 | DepositCompleted(Dest) | ① Release(UNLOADSLOT) ② STEP=50 ③ JOBREPORT(STEP_COMPLETE,50,MOVE,slot) ④ STEP=60 ⑤ JOBREPORT(COMPLETE,60) ⑥ TC 이력 이관·완료 ⑦ 트립 내 전 TC 완료 시 차량 초기화(IDLE/NOTASSIGNED, transportCommandId="") | 종료 |

**2건 배칭 시 확장**: STEP=40 완료 후 "다음 목적지"가 (a) 두 번째 교환의 Mid 설비 (아직 미교환 TC 존재 시) 또는 (b) 반납 투어 시작. 반납은 OLD 2개 → Dest 2곳 순차 방문. 투어 순서는 `TourPlan` (트립 배정 시 계산, 순서: OriginA→OriginB→MidA→MidB→DestA→DestB, 인접성 기준 단순 정렬) 을 `additionalInfo` 대신 **코디네이터가 매번 TC 상태에서 유도** 한다 — 별도 저장 상태를 늘리지 않고 crash 복구를 단순화 (STEP 만 있으면 항상 "다음 할 일"이 유도 가능해야 한다는 규율).

**이벤트-TC 귀속 판별**: 트립에 TC 가 2개면 도착/완료 이벤트가 어느 교환 것인지 구분해야 한다. 판별 키 = `vehicle.CurrentNodeId`(도착 위치) ↔ 각 TC 의 Origin/Mid/Dest StationId 매칭. 두 교환의 위치가 겹치는 경우(같은 버퍼)는 STEP 이 낮은 TC 우선.

**DoD**: 단일 교환 6단계 전이 전체 + 2건 배칭 인터리브 시나리오를 상태기계 단위 테스트로 검증(이벤트 시퀀스 주입 → STEP/보고/슬롯 스냅샷 비교). 임의 시점 프로세스 재시작 후 STEP 기반으로 재개 가능.

### 4.11 [항목11] 차량 배칭 판정 — `FindVehicleForExchangeActivity`

**목적**: EXCHANGE 트립을 받을 수 있는 AMR 탐색.

**파일**: `ExchangeActivities.cs`

**적격 조건** (기존 `FindSuitableVehicleActivity` 조건 + 슬롯):
1. `ProcessingState == IDLE && ConnectionState == CONNECT`
2. `TransportCommandId` 공백 (기존 작업 없음)
3. `slotManager.AreAllSlotsEmpty(vehicleId)` — **4슬롯 전부 EMPTY** (트립 중간 상태 차량 배제)
4. 같은 Bay + 탐색 앵커 = 첫 Origin (기존 `pathManager.SearchSuitableVehicle(originLocation, bayId)` 재사용)

**DoD**: 슬롯 1개라도 OCCUPIED 인 차량이 후보에서 빠지는 테스트.

### 4.12 [항목12] actionCmd 게이팅 (TS→EI) — `RailActionCmdMessage` + `SendActionCmdJson`

**목적**: §4.6 라우팅이 허가한 취출/투입 동작을 EI 경유 MQTT `actionCmd` 로 AMR 에 전달.

**파일**: `ACS.Communication/Mqtt/Model/RailActionCmdMessage.cs` (신규), `MessageManagerExImplement.cs` 에 `SendActionCmdJson()` 추가 (기존 `SendCarrierTransferJson` 의 vehicleId→CommId→destination 해석 로직과 동일 골격), EI 측 변환 워크플로 1개.

**메시지** (envelope 은 기존 RAIL-* 관례):

```json
{ "header": { "messageName": "RAIL-ACTIONCMD", "transactionId": "<Guid>", "timestamp": "<UTC>", "sender": "TS" },
  "data": { "commandId": "EX...123", "vehicleId": "AMR001",
            "nodeId": "<Mid StationId>", "port": "RIGHT",
            "jobType": "UNLOAD",            // UNLOAD=OLD취출 | LOAD=NEW투입
            "amrSlot": 3 } }
```

EI 는 이를 MQTT `actionCmd` 로 변환: `{ "cmdId", "command":"actionCmd", "nodeId", "port", "jobType", "amrSlot" }`.

**DoD**: RAIL-ACTIONCMD 발행 → EI 변환 → MQTT 페이로드 스냅샷 일치. AMR reply(ACCEPTED) 수신 로그 확인.

### 4.13 [항목13] 이동 명령 연계 — `StartExchangeTourActivity` + 구간 이동 발행

**목적**: 투어의 각 이동 구간을 RAIL-CARRIERTRANSFER 로 발행.

**파일**: `ExchangeActivities.cs` + `RailCarrierTransferMessage.cs` 필드 추가.

**RailCarrierTransferData 확장** (additive — 기존 소비자는 새 필드 무시):

```csharp
[JsonPropertyName("amrSlot")]  public int?   AmrSlot { get; set; }   // 해당 구간 조작 슬롯
[JsonPropertyName("stage")]    public string Stage   { get; set; }   // PICKUP_NEW | MOVE_TO_EQUIP | RETURN_OLD
```

**구간별 발행 파라미터**:

| 구간 | destPortId/destNodeId | jobType | portType | amrSlot | stage |
|---|---|---|---|---|---|
| →Origin (신규픽업) | tc.Source | LOAD | BUFFER | LOADSLOT | PICKUP_NEW |
| →Mid (설비) | midLoc:midPortId | EXCHANGE | EQP | — | MOVE_TO_EQUIP |
| →Dest (반납) | tc.Dest | UNLOAD | BUFFER | UNLOADSLOT | RETURN_OLD |

- 첫 구간(→Origin)은 `SendCarrierTransferWithRetryActivity` 패턴(5초×3회) 재사용. 이후 구간은 코디네이터가 단발 발행 + 실패 시 §4.14.
- 매 구간 발행 직후 `UpdateVehicleAcsDestNodeId(다음 waypoint StationId)` — 도착 감지의 전제.

**DoD**: 3구간 각각의 JSON 스냅샷. AcsDestNodeId 가 구간마다 전진하는지 확인.

### 4.14 [항목14] 배칭 인지 실패/복구

**목적**: 실패·중단 시 트립/슬롯 정합 유지. 기존 안전망(stuck 복구, OPERATOR_ABORT)에 EXCHANGE 분기 추가.

**파일**: `ExchangeActivities.cs` (+기존 워크플로 분기 삽입)

**케이스별 처리**:

| 케이스 | 처리 |
|---|---|
| 배차 트랜잭션 실패 | §4.9 롤백으로 자동 원복 (추가 작업 없음) |
| **Source 매거진 없음 (2026-07-24 확정)** | AMR 이 Origin 픽업 시도 → 매거진 부재 감지 시: ① JOBREPORT(COMPLETE, ErrorCode=`MAGAZINE_NOT_FOUND`, ErrorMsg=`No magazine found at LoadSourceLoc candidates`) → MES ② 슬롯 예약 전체 해제·차량 IDLE (실물 적재 전 — 안전) ③ **TC 오류 COMPLETE 로 즉시 종결** — 이력 이관 후 정리. **ACS 내부 재시도·PENDING 없음** — 매거진 보충 후 재교체는 MES 가 새 EXCHANGECMD 로 재요청 (설비 연동 사양서의 "새 Job 재시작" 정책과 정합). 차량·슬롯 즉시 반환으로 반송 자원 점유 최소화. AS-IS(오류→운영자 ABORT→전체 초기화) 폐기 |
| 첫 구간 CARRIERTRANSFER 3회 실패 | 트립 전체 롤백: TC들→EXCHANGE_QUEUED, 슬롯 ReleaseAllByJobId, 차량→IDLE/NOTASSIGNED (기존 `RollbackVehicleAssignmentActivity` 패턴의 트립 버전) |
| STEP≥30 실패 (OLD 취출 후) | **자동 롤백 금지** — 물리 상태 복원 불가. JOBREPORT(COMPLETE, ErrorCode=신규코드, ErrorMsg=EXCHANGEFAILED_MANUAL) 로 MES 통지 + 차량 ALARM 유지 + 슬롯 OCCUPIED 유지(실물 반영). 운영자 수동 정리 후 슬롯 Release |
| **JOBCANCEL (D13, 2026-07-28 확정 — EXCHANGE·MOVECMD 공통)** | 신규 **`CancelJobActivity` (공통 판정)** — JobID 로 TC 조회, jobType 무관 동일 판정·jobType 별 자원 처리만 분기: **C1**(QUEUED/EXCHANGE_QUEUED) CANCELED·이력 이관, JOBREPORT(CANCEL,0). **C2**(ASSIGNED, 픽업 전) 반송 중지 + (EXCHANGE: 슬롯 예약 해제) + 차량 IDLE 후 C1 과 동일. **C3**(적재 후 — EXCHANGE: 슬롯 하나라도 OCCUPIED / MOVECMD: `FullState=FULL`) ① JOBREPORT(CANCEL,0) ② TC 삭제·이력 이관 ③ 충전소 복귀 발행(CHARGEMOVE 재사용) ④ 차량 ALARM → 작업자 조치 대기 (실물 회수→슬롯/적재 수동 정리→알람 해제→운행 복귀, I5 준수). **C4**(terminal) NACK(CANCEL_REJECTED). **C5**[EXCHANGE 배칭] 차량 공유로 반송 전체 중단 — 페어 TC 종결 통보(COMPLETE + ErrorCode=EXCHANGE_CANCELED, 코드값 MES 협의) 후 동일 복귀. `STATE_CANCELING`→`STATE_CANCELED`. **기존 `CancelTransportCommandActivity` 는 본 로직으로 대체 — D4 의 승인된 예외.** 일반 반송 취소 회귀 테스트 항목 추가 필요 |
| OPERATOR_ABORT | 기존 `RailVehicleAbnormalWorkflow` 의 HandleOperatorAbort 에 트립 분기: 트립 내 모든 TC 를 이력 이관·삭제, 슬롯은 **실물 기준** — OCCUPIED 슬롯은 유지하고 UI/수동 정리 대상으로 로그 |
| stuck (RUN+STOP) | `RecoverStuckVehiclesActivity` 에 분기: `TransportCommandId.StartsWith("TRIP")` 이면 트립 TC 들의 STEP 에서 현재 구간을 유도해 해당 구간 CARRIERTRANSFER 재푸시 (기존 매칭표의 트립 버전) |
| TS 재기동 | 복구 로직 불요 — STEP·슬롯이 영속이므로 다음 이벤트/틱에서 자연 재개. 단, 재기동 직후 1회 `트립 정합 감사`(TC.STEP ↔ 슬롯 상태 모순 검출 → WARN) 잡 권장 |

**DoD**: 위 6케이스 각각 시뮬 테스트. 특히 "STEP≥30 실패 시 자동 롤백이 일어나지 않음"을 명시적으로 검증 (잘못된 롤백이 최악의 사고).

### 4.15 [항목15] EI MQTT 매핑

**목적**: 확장된 RAIL-CARRIERTRANSFER(amrSlot/stage)와 신규 RAIL-ACTIONCMD 를 MQTT 명령으로 변환.

**파일**: EI 측 변환 액티비티 (`MqttActivities.cs` 에 추가 또는 신규 `ExchangeMqttActivities.cs`).

**변환 규칙**:
- `RAIL-CARRIERTRANSFER` → `moveCmd`: 기존 매핑 유지 + `amrSlot` 이 있으면 그대로 전달, `jobType` 그대로 (`EXCHANGE` 포함 — AMR 인터페이스 md 가 이미 지원), `portType` 은 목적지 LocationEx.Type.
- `RAIL-ACTIONCMD` → `actionCmd`: §4.12 표 그대로 1:1 매핑.
- AMR reply/status 의 역방향 변환은 **기존 그대로** — acquire/deposit completed, RAIL-VEHICLEUPDATE 는 무수정으로 EXCHANGE 에 재사용된다 (귀속 판별은 TS 코디네이터 몫, §4.10).

**DoD**: moveCmd/actionCmd MQTT 페이로드 스냅샷이 인터페이스 사양서 §4.5 와 일치.

### 4.16 [항목16] DB 마이그레이션 + 시드

**파일**: `docker/init/01_init_acsdb.sql` (추가만) + 운영 DB 용 idempotent 스크립트 (`CREATE TABLE IF NOT EXISTS`).

- §2.3 DDL + 시퀀스 + 인덱스.
- 시드: EXCHANGE 대응 차량별 4행. 대상 설비·버퍼(`NA_R_LOCATION`/`NA_R_STATION`) 미등록 시 INSERT (DDL 아님, 운영 데이터).
- `AcsDbContext.cs` 에 `VehicleSlotEx` 매핑 추가 (기존 매핑 블록과 동일 패턴, 컬럼명 quoted 관례 준수).

**DoD**: `docker compose down -v && up -d` 후 4행×차량수 시드 확인. 기존 테이블 diff 0.

### 4.17 [항목17] 단위 테스트

**파일**: `ACS.Host.Test` 프로젝트에 `Exchange/` 폴더 신설.

최소 세트: `ExchangeInfoTests`(파서/빌더 왕복), `ParseExchangeCmdTests`, `ValidateExchangeCmdTests`(7 케이스), `SlotManagerTests`(전이·예약 원자성), `ExchangeDispatcherTests`(§4.8 의 4 케이스), `ExchangeTourStateMachineTests`(§4.10 전이표 전체 + 인터리브), `ExchangeRecoveryTests`(§4.14 의 6 케이스).

---

## 5. 구현 순서 (의존성 기반)

각 슬라이스는 독립적으로 빌드·검증 가능해야 한다. Claude Code 에 슬라이스 단위로 지시하는 것을 전제로 한 순서.

```
S1. 기반          : 상수 추가(TransportCommandEx) + ExchangeInfo 헬퍼 + 단위테스트   [항목2 일부, 17]
S2. 슬롯 모델      : VehicleSlotEx + DDL/시드 + AcsDbContext + SlotManager          [항목7, 16]
S3. HS 수신 경로   : 파서 → 검증 → 1-TC 생성 → HOST-EXCHANGECMD → RECEIVE 보고      [항목1,3,4 + 5 일부]
     ✅ 검증점: EXCHANGECMD 송신 → DB 1행(EXCHANGE_QUEUED) + JOBREPORT(10) 회신
S4. 배차          : AwakeExchangeJob → 디스패처 → 배칭판정 → 원자할당               [항목8,9,11]
     ✅ 검증점: 큐 2건 → 1트립 2건 배칭, 슬롯 1·3/2·4 기록
S5. 투어 실행      : CARRIERTRANSFER 확장 → 코디네이터 전이표 → JOBREPORT 확장        [항목10,13,5]
     ✅ 검증점: 시뮬 AMR 로 6단계 보고 완주 (단일 교환)
S6. 게이팅        : ACTIONCMD 라우팅 → RAIL-ACTIONCMD → EI 변환                    [항목6,12,15]
     ✅ 검증점: STEP 20/30 에서 actionCmd 게이팅 매트릭스 통과
S7. 배칭 완성      : 2건 인터리브 투어 + 이벤트 귀속 판별                            [항목10 확장]
S8. 실패/복구      : 6 케이스 + stuck/abort 분기                                   [항목14]
S9. 회귀          : 기존 MOVECMD/충전 E2E 재확인                                    [항목17 확장]
```

## 6. 로깅 규약

기존 관례(`{ActivityName}: {메시지} key=value`)를 따르고, EXCHANGE 트립 추적을 위해 **모든 로그에 `trip=` 과 `job=` 을 포함**한다. 최소 로그 포인트: 배차 결정(배칭/단독 사유 포함), 매 STEP 전진, 슬롯 전이, 게이팅 허용/거부, 실패 케이스 분기. 예:

```
ExchangeTourAdvanceActivity: STEP 30→40 trip=TRIP..., job=EX...123, event=DepositCompleted(Mid), slot=1 released
FindBatchCandidateActivity: 단독 출발 (같은 Bay 대기 EXCHANGE 없음) trip=..., bay=BAY01
```

## 7. 외부 팀 의존 사항 (AMR / MES 개발 참조)

ACS 구현이 상대측에 요구하는 것들. 상대 사양서에 반영 필요.

**AMR 측**:
| # | 요구 | 근거 |
|---|---|---|
| A1 | `moveCmd.jobType=EXCHANGE` + `portType=EQP` 도착 후 actionCmd 대기(≤120s) | §4.13 |
| A2 | `actionCmd` 에 `amrSlot` 필드 처리 (UNLOAD=회수슬롯 3·4 로 PICK, LOAD=투입슬롯 1·2 에서 PLACE) | §4.12 |
| A3 | **1 EQP 방문에서 actionCmd 2회**(UNLOAD→LOAD) 순차 수행 | §4.10 STEP 20→40 |
| A4 | **최대 4매거진 동시 보유** + 슬롯별 독립 PICK/PLACE | D3 |
| A5 | 한 미션 내 다구간(버퍼→설비→설비→버퍼→버퍼) 이동 — 각 구간은 ACS 가 개별 moveCmd 로 지시하므로 AMR 은 구간 단위 실행이면 충분 | §4.13 |
| A6 | acquire/deposit 완료 이벤트를 구간마다 송신 (기존 reply/status 체계 유지) | §4.10 진입점 |

**MES 측**:
| # | 요구 | 근거 |
|---|---|---|
| M1 | EXCHANGECMD 필드 14종 (인터페이스 사양서 §4.1) — 슬롯은 `1/2`·`3/4` 번호 또는 공백(ACS 자동배정) | §4.3 |
| M2 | JOBREPORT 의 `Step/StepName/CarrierSlot` 수신 처리 (10~60, 슬롯 번호) | §4.5 |
| M3 | ARRIVED(20) 수신 → 설비 READY 통지 → 설비 요청을 ACTIONCMD 로 중계 (**TBD 확정 필요**: 메시지 형식) | §4.6 |
| M4 | 실패 보고: `ErrorCode=EXCHANGEFAILED_MANUAL`(코드값 협의) 수신 시 수동 개입 프로세스 | §4.14 |
| M5 | 동일 트립에서 두 JobID 의 보고가 인터리브되어 도착할 수 있음 (JobID 로 구분) | §4.10 |

## 8. 완료 정의 (전체)

1. §5 의 S1~S9 검증점 전부 통과.
2. 시뮬 환경에서 (a) 단일 EXCHANGE 6단계 완주, (b) 같은 Bay 2건 배칭 완주, (c) §4.14 의 실패 6케이스 동작.
3. 기존 MOVECMD/충전/일반 반송 회귀 테스트 diff 0.
4. 기존 파일 변경이 "분기 추가·메서드 추가·상수 추가·매핑 추가"로만 구성됨을 코드리뷰로 확인 (기존 라인 수정/삭제 없음).

## 9. 참조 문서

| 문서 | 내용 |
|---|---|
| `EXCHANGE_통신_인터페이스_사양서.md` | 전 구간 인터페이스·메시지 정의 (본 문서의 상위) |
| `EXCHANGE_개발공수_산정.xlsx` | 항목 번호·공수 (본 문서 §4 와 번호 일치) |
| `movecmd_source_empty.md` | 기존 TC 생성·검증·에러코드 관례 |
| `schedule_check_vehicle.md` | stuck 복구 — §4.14 분기 대상 |
| `vehicleabnormal.md` | OPERATOR_ABORT — §4.14 분기 대상 |
| `ACSAMR_mqtt_movecmd.md`, `mqtt_interface.md` | AMR MQTT 규약 |
