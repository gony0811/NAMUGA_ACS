# SCHEDULE-CHECKVEHICLES 워크플로우

## 개요

Daemon 프로세스가 10초마다 트리거하는 Vehicle 상태 점검·복구 워크플로우. 두 가지 문제를 자동으로 처리한다.

1. **통신 단절 감지** — Vehicle 의 `EventTime` 이 1분 이상 갱신되지 않으면 `ConnectionState = DISCONNECT` 로 변경.
2. **Stuck Vehicle 복구** — `ProcessingState=RUN` + `RunState=STOP` 로 멈춘 차량을 찾아 할당된 TC(TransportCommand) 와 정합 검증 후 `RAIL-CARRIERTRANSFER` 메시지를 EI 로 재전송.

Stuck 복구 로직은 2026-05-08 커밋 `69c105d "Recover stuck vehicles; filter zombie queued TCs"` 로 추가되었으며, 통신 단절·EF Core silent drop·Rollback 오발동 등으로 차량과 TC 가 불일치한 채 차량이 멈춘 케이스를 자동 복구하기 위한 안전망이다.

## 트리거

| 항목 | 값 |
|---|---|
| 워크플로우 DefinitionId | `SCHEDULE-CHECKVEHICLES` |
| 정의 파일 | `src/ACS/ACS.Elsa/Workflows/Trans/ScheduleCheckvehiclesWorkflow.cs` |
| 스케줄러 | `AwakeCheckVehiclesJob` (`PeriodicBackgroundService`) |
| 주기 | 10초 |
| 실행 프로세스 | Daemon (`Acs:Process:Type = daemon` 인 경우만 등록) |
| DI 등록 위치 | `src/ACS/ACS.App/Modules/SchedulingModule.cs:57` |
| 트리거 방식 | `IMessageAgent.Send` 로 `DaemonScheduleMessage { MessageName = "SCHEDULE-CHECKVEHICLES" }` JSON 전송 → Elsa 워크플로우 엔진 라우팅 |

### Daemon → Trans 라우팅 흐름

별개 프로세스인 Daemon 이 메시지를 발행하면 Trans 프로세스의 Elsa 엔진이 받아 워크플로우를 띄우는 구조다.

```
[Daemon 프로세스]
  AwakeCheckVehiclesJob.ExecuteOnce()   ← 10초 주기 (PeriodicBackgroundService)
    │
    ├─ DaemonScheduleMessage { MessageName = "SCHEDULE-CHECKVEHICLES" } JSON 직렬화
    └─ IMessageAgent.Send(json) → MSB(RabbitMQ) 발행
                                      │
                                      ▼
[Trans 프로세스]
  Elsa 메시지 디스패처가 Header.MessageName 으로 워크플로우 매칭
    │
    ▼
  ScheduleCheckvehiclesWorkflow (DefinitionId = "SCHEDULE-CHECKVEHICLES") 인스턴스 실행
```

근거:
- `src/ACS/ACS.App/Scheduling/Awake/AwakeCheckVehiclesJob.cs:20-43` — JSON 생성 후 `_messageAgent.Send((object)json)`.
- `src/ACS/ACS.App/Modules/SchedulingModule.cs:52-64` — `Acs:Process:Type == "daemon"` 일 때만 HostedService 로 등록.
- `src/ACS/ACS.Elsa/Workflows/Trans/ScheduleCheckvehiclesWorkflow.cs` — Trans 측 워크플로우 정의 (DefinitionId 매칭).

## 워크플로우 구조

```
SCHEDULE-CHECKVEHICLES  (10초 주기)
│
├── Step 1: CheckVehiclesEventTimeActivity
│     입력: (없음)
│     출력: StaleVehicles (ICollection<VehicleEx>), StaleCount (int)
│
├── Step 2: If (StaleCount > 0) → DisconnectVehiclesActivity
│     입력: StaleVehicles
│     출력: Success (bool)
│
└── Step 3: RecoverStuckVehiclesActivity   ← TRANPODTCMDID 검사 + carrierTransfer 재전송
      입력: (Vehicle 목록 자체 조회)
      출력: (재전송 수 로그)
```

## Step 1 — CheckVehiclesEventTimeActivity

위치: `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs:698-776`

- 모든 Vehicle 을 `IResourceManagerEx.GetVehicles()` 로 조회.
- 다음 조건이 하나라도 참이면 `staleList` 에 추가:
  - `vehicle.EventTime == default(DateTime)` (한 번도 갱신되지 않음)
  - `(DateTime.UtcNow - vehicle.EventTime).TotalSeconds > 60` (60초 초과)
- **제외 대상**: `ProcessingState ∈ {PARK, CHARGE}` 인 Vehicle.
- 시간 비교는 반드시 `DateTime.UtcNow` 사용 — EventTime 은 `EfCorePersistentDao.SetPropertyValue` 에서 UTC 로 저장되므로 Local 시간과 비교하면 KST(+9h) 오프셋만큼 항상 stale 판정됨.

## Step 2 — DisconnectVehiclesActivity

위치: `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs:782-835`

- 발동 조건: `StaleCount > 0` (위 워크플로우의 `If` 분기).
- 입력으로 받은 `vehicleList` 의 각 Vehicle 에 대해 `ConnectionState != "DISCONNECT"` 이면 `IResourceManagerEx.UpdateVehicleConnectionState(vehicleId, CONNECTIONSTATE_DISCONNECT, "SCHEDULE-CHECKVEHICLES")` 호출.
- `CommType`(NIO/MQTT) 에 관계없이 일괄 처리.

## Step 3 — RecoverStuckVehiclesActivity

위치: `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs:840-976`

본 워크플로우의 핵심. 사용자 표현 "AMR 이 RUN 상태일 때 TRANPODTCMDID 를 확인해서 진행 중인 JOB 이 있으면 목적지로 보내는 로직" 이 여기에 해당한다. `TRANPODTCMDID` 는 코드 상 `VehicleEx.TransportCommandId` 필드이며, vehicle 에 할당된 `TransportCommandEx.JobId` 를 참조한다.

### 3.1 발동 조건 (모두 만족해야 재전송)

`IResourceManagerEx.GetVehicles()` 의 각 Vehicle 에 대해 아래 순서로 검사하며, 하나라도 실패하면 해당 Vehicle 은 `continue` 로 스킵한다.

| # | 조건 | 의미 |
|---|---|---|
| 1 | `vehicle.ProcessingState == "RUN"` | ACS 가 명령을 부여해 운행 중인 차량만 |
| 2 | `vehicle.RunState == "STOP"` | 실제로는 정지해 있음 — stuck 상태 |
| 3 | `vehicle.AlarmState == "NOALARM"` | 알람 중인 차량에는 이동 명령 미전송 (안전) |
| 4 | `!string.IsNullOrEmpty(vehicle.TransportCommandId)` | **TRANPODTCMDID 검사 포인트** — 할당된 TC ID 가 존재 |
| 5 | `transferManager.GetTransportCommand(vehicle.TransportCommandId) != null` | DB 에서 TC 가 실제로 조회됨 |
| 6 | `tc.VehicleId == vehicle.VehicleId` (또는 3.3 의 자동 보정 가능) | TC ↔ Vehicle 양방향 일치 |
| 7 | `(vehicle.TransferState, tc.State)` 가 3.2 매칭 표 중 하나 | 재전송 방향(useSource / jobType) 결정 가능 |

조건 5 실패 시 WARN 로그: `RecoverStuckVehiclesActivity: TC 없음 vehicleId=..., transportCommandId=...`.

### 3.2 상태 매칭 표 (조건 7)

| `vehicle.TransferState` | `tc.State` | `useSource` | `jobType` | 의미 |
|---|---|---|---|---|
| `ASSIGNED` | `ASSIGNED` 또는 `TRANSFERRING_SOURCE` | `true` | `UNLOAD` | Source(pick-up) 로 가던 중 → Source 좌표 기준 재전송 |
| `TRANSFERING_DEST` | `TRANSFERRING_DEST` | `false` | `LOAD` | Dest(drop-off) 로 가던 중 → Dest 좌표 기준 재전송 |
| 그 외 조합 | — | — | — | 재전송 대상 아님 (`continue`) |

`useSource = true` 이면 `CarrierTransferJsonBuilder` 가 TC.Source 좌표를, `false` 이면 TC.Dest 좌표를 목적지로 사용한다.

### 3.3 TC.VehicleId 불일치 시 자동 보정 (Self-Heal)

`tc.VehicleId != vehicle.VehicleId` 인 경우, **Vehicle 측 (`TransportCommandId`, `TransferState`) 을 진실 원천(source of truth) 으로 간주**하여 TC 를 재연결한다.

**보정 가능 조건 (`canHeal`)**:
- `vehicle.TransportCommandId == tc.JobId` (vehicle 이 이 TC 를 자기 작업이라 주장)
- AND `vehicle.TransferState ∈ {TRANSFERING_DEST, ASSIGNED}`

`canHeal` 이 false 면 WARN 로그(`TC.VehicleId 불일치 (자동 보정 불가)`) 남기고 스킵.

**보정 로직**:

1. `tc.VehicleId = vehicle.VehicleId` 로 재연결.
2. `vehicle.TransferState == TRANSFERING_DEST` 이고 `tc.State != TRANSFERRING_DEST` 면:
   - `tc.State = TRANSFERRING_DEST`
   - `tc.LoadedTime == null` 인 경우 `DateTime.Now` 로 설정
3. `vehicle.TransferState == ASSIGNED` 이고 `tc.State` 가 `ASSIGNED` / `TRANSFERRING_SOURCE` 가 아니면:
   - `tc.State = ASSIGNED`
4. `transferManager.UpdateTransportCommand(tc)` 호출
5. WARN 로그: `TC 재연결 완료 vehicleId=..., tc=..., oldVehicleId=..., oldState=..., newState=...`

**배경**: Rollback 의 잘못된 발동이나 EF Core ChangeTracker 의 silent drop 으로 TC.VehicleId 가 비워진 케이스를 자동 복구하기 위함. 장기 생존 DbContext 의 ChangeTracker 가 갱신을 silent drop 하던 패턴은 별도 메모 참조.

### 3.4 메시지 빌드 / 송신

- JSON 빌드: `CarrierTransferJsonBuilder.Build(tc, vehicle.VehicleId, jobType, useSource, resourceManager, logger)`
- 송신: `messageManager.SendCarrierTransferJson(json)` — **응답 대기·재시도 없음** (단발 재푸시)
- 비교: 신규 할당 경로의 `SendCarrierTransferWithRetryActivity` 는 5초 timeout × 3회 재시도를 수행하지만, Recover 는 이미 명령 중인 vehicle 의 재푸시이므로 단순 송신만 한다.
- JSON 빌드 실패 시 ERROR 로그(`JSON 빌드 실패 vehicleId=..., tc=...`) 남기고 스킵.

**송신 경로 (`SendCarrierTransferJson` 내부)** — `src/ACS/ACS.Manager/Message/MessageManagerExImplement.cs:1706-1781`:

1. JSON 에서 `vehicleId` 추출 → `resourceManager.GetVehicle(vehicleId)` 로 VehicleEx 조회 → `vehicle.CommId` 획득.
2. `CommId` 로 `MqttConfig` 조회 (`PersistentDao.FindByName`).
3. `ApplicationManager.GetApplication(...)` 로 대상 Application 조회.
4. `destinationName = application.DestinationName + "/" + application.Name` 조립.
5. `esAgent.Send(jsonMessage, destinationName, false, "")` 호출로 EI(MQTT) 토픽에 발행.

즉 Trans 가 직접 vehicle 의 EI 토픽을 계산해 단발 publish 하는 구조이며, 도중에 응답을 기다리는 hop 은 없다.

### 3.5 로깅

| 레벨 | 케이스 | 메시지 형식 |
|---|---|---|
| ERROR | DI 해결 실패 | `RecoverStuckVehiclesActivity: 필수 서비스 해결 실패` |
| ERROR | JSON 빌드 실패 | `RecoverStuckVehiclesActivity: JSON 빌드 실패 vehicleId=..., tc=...` |
| ERROR | 예외 catch | `RecoverStuckVehiclesActivity: {ex.Message}` |
| WARN | TC 조회 실패 | `RecoverStuckVehiclesActivity: TC 없음 vehicleId=..., transportCommandId=...` |
| WARN | self-heal 불가 | `RecoverStuckVehiclesActivity: TC.VehicleId 불일치 (자동 보정 불가) ...` |
| WARN | self-heal 완료 | `RecoverStuckVehiclesActivity: TC 재연결 완료 ...` |
| INFO | 재전송 성공 (대당) | `RecoverStuckVehiclesActivity: RAIL-CARRIERTRANSFER 재전송 vehicleId=..., tc=..., transferState=..., tcState=..., jobType=..., useSource=..., acsDestNodeId=...` |
| INFO | 사이클 요약 | `RecoverStuckVehiclesActivity: 총 N대 재전송 완료` (N > 0 일 때만) |

## 관련 도메인 모델

### VehicleEx (`src/ACS/ACS.Core/Resource/Model/VehicleEx.cs`)

| 필드 | 본 워크플로우에서 사용하는 값 | 비고 |
|---|---|---|
| `ProcessingState` | `IDLE`, `RUN`, `CHARGE`, `PARK` | Step 1 제외 조건(PARK/CHARGE), Step 3 조건 1(RUN) |
| `RunState` | `RUN`, `STOP` | Step 3 조건 2(STOP) |
| `AlarmState` | `ALARM`, `NOALARM` | Step 3 조건 3(NOALARM) |
| `ConnectionState` | `CONNECT`, `DISCONNECT` | Step 2 가 갱신 |
| `EventTime` | UTC `DateTime` | Step 1 의 stale 판정 기준 |
| `TransportCommandId` | TC.JobId 참조 (별칭: TRANPODTCMDID) | Step 3 조건 4·5 |
| `TransferState` | `NOTASSIGNED`, `ASSIGNED`, `ASSIGNED_ENROUTE`, `ASSIGNED_PARKED`, `ASSIGNED_ACQUIRING`, `ASSIGNED_DEPOSITING`, `ACQUIRE_COMPLETE`, `TRANSFERING_DEST`, `DEPOSIT_COMPLETE` | Step 3 매칭 표 |

### TransportCommandEx (`src/ACS/ACS.Core/Transfer/Model/TransportCommandEx.cs`)

| 필드 | 본 워크플로우에서 사용하는 값 | 비고 |
|---|---|---|
| `JobId` | string PK | `vehicle.TransportCommandId` 가 참조 |
| `State` | `CREATED`, `QUEUED`, `WAITING`, `PREASSIGNED`, `ASSIGNED`, `ARRIVED_SOURCE`, `ARRIVED_DEST`, `TRANSFERRING_SOURCE`, `TRANSFERRING_DEST`, `COMPLETED`, ... | Step 3 매칭 표, self-heal 시 갱신 |
| `VehicleId` | 할당된 차량의 VehicleId | Step 3 조건 6, self-heal 대상 |
| `Source`, `Dest` | 좌표 | `CarrierTransferJsonBuilder` 가 useSource 에 따라 선택 |
| `JobType` | `LOAD`, `UNLOAD`, ... | Step 3 매칭 표 |
| `LoadedTime` | nullable DateTime | self-heal 에서 TRANSFERRING_DEST 로 승격 시 null 이면 채움 |

## 연관 안전장치 (같은 커밋 `69c105d`)

이 워크플로우 외에 stuck 복구 시나리오를 받쳐주는 변경이 함께 들어갔다.

- **RollbackVehicleAssignmentActivity 의 progress-aware guard** (`ScheduleActivities.cs:647-689`)
  - Fresh 인스턴스 재조회(`transferManager.GetTransportCommand`, `resourceManager.GetVehicle`) 로 ChangeTracker 스냅샷 의존성을 끊는다.
  - `tc.State ∈ {TRANSFERRING_SOURCE, TRANSFERRING_DEST, COMPLETED}` 또는 `vehicle.TransferState ∈ {ACQUIRE_COMPLETE, TRANSFERING_DEST, DEPOSIT_COMPLETE}` 이면 롤백 스킵 — 진행 중인 작업을 잘못 되돌리는 사고 방지.
- **TransferManagerExImplement 의 zombie TC 필터링** (`TransferManagerExImplement.cs:215-253`)
  - `GetQueuedTransportCommands` / `GetQueuedTransportCommandsByBayId` 에서 `State=QUEUED` 인데 `VehicleId` 가 남아있는 좀비 TC 를 메모리에서 필터.
  - Rollback 오발동이나 EF silent drop 으로 만들어진 좀비가 다음 사이클에서 다시 잡혀 잘못된 재할당을 일으키는 것을 방지.
- **RailVehicleDepositCompletedWorkflow 의 QUEUED regression 보정** (`RailVehicleDepositCompletedWorkflow.cs:358-380`)
  - `tc.State == QUEUED` 이지만 Vehicle 측이 이 TC 를 자기 작업이라 주장하고 이동 중이면, Vehicle 정보를 진실 원천으로 TC 를 재연결.

## 참고 파일

| 항목 | 파일 |
|---|---|
| 워크플로우 정의 | `src/ACS/ACS.Elsa/Workflows/Trans/ScheduleCheckvehiclesWorkflow.cs` |
| Step 1 / 2 / 3 액티비티 | `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs:698-976` |
| 스케줄러 | `src/ACS/ACS.App/Scheduling/Awake/AwakeCheckVehiclesJob.cs` |
| 스케줄러 등록 | `src/ACS/ACS.App/Modules/SchedulingModule.cs:57` |
| Vehicle 모델 | `src/ACS/ACS.Core/Resource/Model/VehicleEx.cs` |
| TransportCommand 모델 | `src/ACS/ACS.Core/Transfer/Model/TransportCommandEx.cs` |
| 메시지 빌더 | `CarrierTransferJsonBuilder` (in `src/ACS/ACS.Elsa/Activities/`) |
| Rollback 가드 / Zombie 필터 | `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs:647-689`, `src/ACS/ACS.Manager/Transfer/TransferManagerExImplement.cs:215-253` |
