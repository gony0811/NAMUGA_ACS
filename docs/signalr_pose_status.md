# SignalR 차량 텔레메트리(POSE + 상태) 수신 사양서

## 1. 개요

AMR(차량)의 실시간 위치/방향(**POSE**: X, Y, Angle)과 일부 **상태 정보**(배터리·노드·런상태·연결상태)를 **SignalR**을 통해 데스크탑 UI(`ACS.UI`)로 브로드캐스트하는 경로의 사양이다.

- **목적**: AMR 최대 100대 × 1Hz 텔레메트리를 워크플로우 엔진(Elsa)을 거치지 않고 UI 맵에 직접 실시간 반영
- **방향**: **서버(ACS.App) → 클라이언트(ACS.UI) 단방향 브로드캐스트**. 클라이언트가 Hub로 호출하는 메서드는 없음
- **이벤트명**: `"VehicleUpdate"` (단일)
- **Hub 엔드포인트**: `/hubs/vehicle`

> 원천 데이터는 EI → Trans 프로세스로 전달되는 `RAIL-VEHICLEUPDATE` 메시지에 포함된다. Trans는 forward 직전에 EI 원본(특히 POSE)은 유지하되 상태 필드를 자신이 갱신·동기화한 **vehicle 권위값(ProcessingState/State/CurrentNodeId/TransportCommandId/Path 등)** 으로 채운 "전체 상태 스냅샷"을 RabbitMQ fanout으로 재발행한다. ACS.App이 이 fanout을 구독해 POSE + 상태 필드를 추려 SignalR로 다시 브로드캐스트한다.
>
> ⚠️ **알람(AlarmState)은 이 경로에 포함되지 않는다.** 알람은 별도 메시지 `RAIL-VEHICLEALARM`(SET/RESET)이며 UI fanout으로 forward되지 않는다. (이전에는 `State`/`ProcessingState`/`TransferState`도 이 경로에 없었으나, 이제 Trans 권위 스냅샷에 포함되어 실시간 전달된다.)

---

## 2. 엔드투엔드 흐름

```
[EI] ──RAIL-VEHICLEUPDATE──▶ [Trans 프로세스]
                                  │  ForwardToUi → UiAgentSender (MULTICAST=fanout)
                                  │  RabbitMQ exchange: "/VM/DEMO/UI/SENDER"
                                  │  (EI 원본 POSE + Trans 권위 상태 스냅샷 병합 forward)
                                  ▼
                         ┌──────────────────────────────────────────────┐
                         │ ACS.App                                       │
                         │  PoseTelemetrySubscriber (BackgroundSvc)      │
                         │   - RabbitMQ.Client로 fanout 직접 구독        │
                         │   - POSE + 상태 필드 추출 (POSE 없어도 푸시)  │
                         │   - IHubContext<VehicleHub>                   │
                         │       .Clients.All.SendAsync("VehicleUpdate") │
                         └──────────────────────────────────────────────┘
                                  │  SignalR Hub "/hubs/vehicle"
                                  ▼
                         ┌──────────────────────────────────────────────┐
                         │ ACS.UI                                        │
                         │  VehicleHubClient                             │
                         │   .On<VehicleUpdateDto>("VehicleUpdate", …)   │
                         │   └▶ VehicleUpdated 이벤트                    │
                         │       └▶ Dispatcher.UIThread.Post             │
                         │           └▶ MapViewModel.ApplyVehicleUpdate  │
                         │               └▶ DataChanged → 맵 리렌더      │
                         └──────────────────────────────────────────────┘
```

**단계 요약**

1. Trans 프로세스가 `RAIL-VEHICLEUPDATE` JSON을 RabbitMQ fanout exchange(`/VM/DEMO/UI/SENDER`)에 발행한다. forward 직전 상태 필드를 vehicle 권위값으로 채운다(POSE는 EI 원본 유지).
2. ACS.App의 `PoseTelemetrySubscriber`(BackgroundService)가 이 exchange를 직접 구독한다.
3. `Data`가 있으면 POSE(nullable) + 상태 필드를 camelCase 페이로드로 만들어 SignalR `"VehicleUpdate"` 이벤트로 **모든 연결**에 브로드캐스트한다. (POSE가 없어도 상태만 푸시)
4. ACS.UI의 `VehicleHubClient`가 `"VehicleUpdate"`를 수신해 `VehicleUpdated` 이벤트를 발생시킨다.
5. `App.axaml.cs`가 이를 구독, UI 스레드로 마샬링한 뒤 `MapViewModel.ApplyVehicleUpdate(dto)`를 호출한다.
6. `MapViewModel`이 차량을 매칭해 상태 필드는 항상, POSE는 수신된 경우에만 갱신하고 `DataChanged`로 맵을 다시 그린다.

---

## 3. 연결 사양 (Connection)

### 3.1 서버 측 (ACS.App)

| 항목 | 값 | 위치 |
|------|-----|------|
| SignalR 등록 | `builder.Services.AddSignalR();` | `ACS.App/Program.cs:185` |
| Hub 매핑 | `app.MapHub<VehicleHub>("/hubs/vehicle");` | `ACS.App/Program.cs:206` |
| Hub 클래스 | `VehicleHub : Hub` (빈 클래스, 브로드캐스트 전용) | `ACS.App/Web/Hubs/VehicleHub.cs` |
| Kestrel 바인딩 | `Acs:Api:ListenIP`(기본 `any`), `Acs:Api:ListenPort`(기본 `5100`) | `ACS.App/Program.cs:148-149` |

> `VehicleHub`은 메서드가 없는 빈 Hub다. 발행은 `PoseTelemetrySubscriber`가 `IHubContext<VehicleHub>`로 수행한다.

### 3.2 클라이언트 측 (ACS.UI)

`ACS.UI/Services/VehicleHubClient.cs`

```csharp
_connection = new HubConnectionBuilder()
    .WithUrl(baseUrl.TrimEnd('/') + "/hubs/vehicle")
    .WithAutomaticReconnect()
    .Build();
```

- **Base URL**: `BackendSettings.BaseUrl` = `http://{Host}:{Port}` (기본 `http://127.0.0.1:5100`)
  - 정의: `ACS.UI/Services/BackendSettings.cs`
  - 설정: `ACS.UI/appsettings.json` 의 `Backend` 섹션(`Host`, `Port`)
- **자동 재연결**: `WithAutomaticReconnect()` 활성화
- 연결 시작: `App.axaml.cs`에서 `_vehicleHub.StartAsync()` 호출

---

## 4. 이벤트 / 메시지 사양

### 4.1 이벤트

| 항목 | 값 |
|------|-----|
| 이벤트명 | `"VehicleUpdate"` |
| 방향 | 서버 → 클라이언트 (`Clients.All`) |
| 발행 빈도 | AMR당 약 1Hz (수신되는 텔레메트리에 종속) |

### 4.2 클라이언트 수신 DTO — `VehicleUpdateDto`

`ACS.UI/Models/VehicleUpdateDto.cs`

| 필드 | 타입 | 의미 |
|------|------|------|
| `VehicleId` | `string` | DB PK 식별자 |
| `CommId` | `string` | MQTT 식별자 (매칭 fallback) |
| `PoseX` | `float?` | 위치 X (meters). 미수신 시 null |
| `PoseY` | `float?` | 위치 Y (meters). 미수신 시 null |
| `PoseAngle` | `float?` | 방향 (radian). 미수신 시 null |
| `RunState` | `string` | 주행 상태 (Trans 권위값) |
| `ProcessingState` | `string` | 처리 상태(CHARGE/IDLE/RUN). Trans 권위값 |
| `State` | `string` | 차량 상태(ALIVE/BANNED 등). Trans 권위값 |
| `TransferState` | `string` | 반송 상태. Trans 권위값 |
| `BatteryRate` | `int` | 배터리 잔량(%) |
| `BatteryVoltage` | `float` | 배터리 전압 |
| `CurrentNodeId` | `string` | 현재 노드 (Trans 권위값, 항상 채워짐) |
| `AcsDestNodeId` | `string` | ACS 할당 목적 노드 (완료 시 "" 클리어) |
| `VehicleDestNodeId` | `string` | 차량 목적 노드 |
| `TransportCommandId` | `string` | 할당된 반송 명령 ID (완료 시 "" 클리어) |
| `Path` | `string` | 경로 (완료 시 "" 클리어) |
| `ConnectionState` | `string` | 연결 상태 |
| `EventTime` | `DateTime` | 이벤트 발생 시각 |

> SignalR JSON 프로토콜은 `PropertyNameCaseInsensitive`가 기본 true이므로, 서버의 camelCase(`poseX`, `runState` …)가 클라이언트 DTO의 파스칼케이스 프로퍼티에 매핑된다.

### 4.3 서버 발행 페이로드

`PoseTelemetrySubscriber.cs` `OnMessageReceived` — 익명 객체(**camelCase**)로 전송:

```csharp
var d = msg.Data;
var payload = new
{
    vehicleId = d.VehicleId,
    commId    = d.CommId,
    poseX     = d.PoseX,        // nullable
    poseY     = d.PoseY,        // nullable
    poseAngle = d.PoseAngle,    // nullable
    runState           = d.RunState,
    processingState    = d.ProcessingState,
    state              = d.State,
    transferState      = d.TransferState,
    batteryRate        = d.BatteryRate,
    batteryVoltage     = d.BatteryVoltage,
    currentNodeId      = d.CurrentNodeId,
    acsDestNodeId      = d.AcsDestNodeId,
    vehicleDestNodeId  = d.VehicleDestNodeId,
    transportCommandId = d.TransportCommandId,
    path               = d.Path,
    connectionState    = d.ConnectionState,
    eventTime          = d.EventTime
};
```

---

## 5. 서버 측 발행 사양 — `PoseTelemetrySubscriber`

파일: `ACS.App/Web/Realtime/PoseTelemetrySubscriber.cs`

- **형태**: `BackgroundService`. `ControlModule`에서 `IHostedService`로 등록되어 Generic Host가 자동 기동 (`Program.cs:188` 주석 참조).
- **워크플로우 우회 이유**: 100대 × 1Hz 부하를 워크플로우 엔진(`GenericWorkflowRabbitMQListener`)을 거치지 않고 `RabbitMQ.Client` API로 직접 처리.

### 5.1 RabbitMQ 구독

| 항목 | 값 | 비고 |
|------|-----|------|
| Exchange 타입 | `fanout` | Trans의 `UiAgentSender`(MULTICAST)와 일치 |
| Exchange 이름 | `{DomainValue}/UI/SENDER` → 정규화 → `/VM/DEMO/UI/SENDER` | `NormalizeName`: `.`→`/`, leading slash 보장 |
| Queue | 익명 임시 큐(인스턴스마다 고유) → fanout에 바인딩 | `routingKey = ""` |
| Consumer | `EventingBasicConsumer`, `autoAck: true` | |

> fanout + 인스턴스별 익명 큐이므로, CS 프로세스를 여러 개 띄우면 **각 인스턴스가 모든 메시지의 사본을 받아** 각자 자기 SignalR 클라이언트에 브로드캐스트한다(경쟁 소비 아님).

### 5.2 설정 키 (`ACS.App/appsettings.json`)

| 키 | 기본값 | 용도 |
|----|--------|------|
| `Destination:Server:Domain:ConnectUrl` | `localhost` | RabbitMQ 호스트 |
| `Destination:Server:Domain:Username` | `guest` | 계정 |
| `Destination:Server:Domain:Password` | `guest` | 비밀번호 |
| `Destination:Server:DomainValue` | `VM/DEMO` | exchange 이름 prefix |

### 5.3 메시지 처리 (`OnMessageReceived`)

1. 본문을 UTF-8로 디코딩 후 `RailVehicleUpdateMessage`로 역직렬화.
2. **`Data == null`이면 무시.** (POSE 유무는 더 이상 드롭 조건이 아님 — 상태만 변하는 메시지도 푸시.)
3. POSE(nullable) + 상태 필드를 camelCase 페이로드로 만들어 `_hub.Clients.All.SendAsync("VehicleUpdate", payload)` — **fire-and-forget**(`_ =`)으로 RabbitMQ consumer 스레드를 막지 않음.
4. 진단 로그는 5초 간격으로 throttle (`BroadcastLogInterval`).

> 알람 메시지(`RAIL-VEHICLEALARM`)가 혹시 같은 fanout으로 들어오더라도 `RailVehicleUpdateMessage` 역직렬화 실패 → catch에서 조용히 드롭된다(현재는 알람이 이 fanout으로 forward되지 않음).

### 5.4 원본 메시지 모델 — `RailVehicleUpdateMessage`

파일: `ACS.Communication/Mqtt/Model/RailVehicleUpdateMessage.cs` (`RailVehicleUpdateData`):

| JSON | 프로퍼티 | 타입 | UI 실시간 반영 |
|------|----------|------|---------------|
| `vehicleId` | `VehicleId` | `string` | (매칭 키) |
| `commId` | `CommId` | `string` | (매칭 키) |
| `poseX/poseY/poseAngle` | `PoseX/PoseY/PoseAngle` | `float?` | ✅ 위치/방향 |
| `runState` | `RunState` | `string` | ✅ (Trans 권위값) |
| `processingState` | `ProcessingState` | `string` | ✅ (Trans 권위값) |
| `state` | `State` | `string` | ✅ (Trans 권위값) |
| `transferState` | `TransferState` | `string` | ✅ (Trans 권위값) |
| `batteryRate` | `BatteryRate` | `int` | ✅ 배터리 바 |
| `batteryVoltage` | `BatteryVoltage` | `float` | ✅ |
| `currentNodeId` | `CurrentNodeId` | `string` | ✅ (Trans 권위값, 항상) |
| `acsDestNodeId` | `AcsDestNodeId` | `string` | ✅ (완료 시 클리어) |
| `vehicleDestNodeId` | `VehicleDestNodeId` | `string` | ✅ |
| `transportCommandId` | `TransportCommandId` | `string` | ✅ (완료 시 클리어) |
| `path` | `Path` | `string` | ✅ (완료 시 클리어) |
| `connectionState` | `ConnectionState` | `string` | ✅ 차량 색상 |
| `fullState`, `batteryChargingState`, `nodeChanged` | — | — | ❌ VehicleDto에 대응 필드 없음 |

> `State`/`ProcessingState`/`TransferState`/`AcsDestNodeId`/`TransportCommandId`/`Path`는 EI 원본에는 없지만, `RailVehicleUpdateWorkflow`가 forward 직전 vehicle 권위값으로 채워 보내므로 실시간 전달된다(Step 10). `AlarmState`만 별도 메시지(`RAIL-VEHICLEALARM`) 경로라 여기 없다.

---

## 6. 클라이언트 측 수신·처리 사양 (ACS.UI)

### 6.1 `VehicleHubClient` — 수신 핸들러

`ACS.UI/Services/VehicleHubClient.cs`

```csharp
_connection.On<VehicleUpdateDto>("VehicleUpdate", dto =>
{
    VehicleUpdated?.Invoke(dto);
});
```

- `VehicleUpdated` 이벤트(`event Action<VehicleUpdateDto>`)로 노출.
- **콜백은 SignalR 워커 스레드에서 호출됨** → UI 갱신 시 반드시 Dispatcher로 마샬링 필요(주석 명시).

### 6.2 UI 통합 — `App.axaml.cs`

```csharp
_vehicleHub = new VehicleHubClient(baseUrl);
_vehicleHub.VehicleUpdated += dto =>
{
    Dispatcher.UIThread.Post(() =>
    {
        mainViewModel.MapViewModel.ApplyVehicleUpdate(dto);
    });
};
_ = _vehicleHub.StartAsync();
```

### 6.3 적용 — `MapViewModel.ApplyVehicleUpdate(VehicleUpdateDto dto)`

- 매칭: **`VehicleId` 우선, 없으면 `CommId`** 로 `OrdinalIgnoreCase` 비교(공백 trim).
- 두 키 모두 비었거나 목록에 차량이 없으면 무시. no-match 로그는 5초 throttle(`NoMatchLogInterval`).
- **상태 필드 머지 규칙은 두 갈래**:
  - 기존 필드(`RunState`/`ConnectionState`/`CurrentNodeId`/`VehicleDestNodeId`)는 빈 값이면 기존 값 유지(EI 부분 채움 호환). `BatteryRate/BatteryVoltage`는 항상 설정.
  - Trans 권위 스냅샷 필드 중 `AcsDestNodeId`/`TransportCommandId`/`Path`는 **빈 값으로도 직접 대입**해 클리어 반영(작업 완료 시 "" 전달). `ProcessingState`/`State`/`TransferState`는 `null`이 아니면 그대로 반영.
- **POSE는 `HasValue`일 때만 갱신** — POSE 없는 상태 메시지가 기존 위치를 (0,0)으로 지우지 않도록.
- 끝에 `DataChanged?.Invoke()`로 맵 리렌더 트리거.

### 6.4 POSE 보존 머지 — `MapViewModel.UpdateVehicles`

REST 차량 목록 갱신 시, 기존 차량의 실시간 `PoseX/Y/Angle`을 신규 항목으로 머지한다(VehicleId/CommId 기준). REST 응답에는 POSE가 없으므로 여전히 필요. (상태 필드는 REST가 권위값이라 별도 머지하지 않음.)

### 6.5 표시 범위 및 한계

- **맵(MapView)만 실시간**: 차량 색상(ConnectionState/State), 배터리 바(BatteryRate), 호버 팝업(RunState/ProcessingState/Battery/Node/TC/Connection)이 1Hz로 갱신. `State`/`ProcessingState`도 이제 메시지에 포함되어 실시간 반영된다.
- **차량 목록 그리드는 실시간 아님**: `VehicleListViewModel`/`VehicleViewModel`은 `ObservableCollection<VehicleDto>`이고 `VehicleDto`는 `INotifyPropertyChanged` 미구현이라 in-place 갱신이 그리드에 반영되지 않음 → 기존 REST 새로고침 유지. (그리드 실시간화는 별도 작업.)

### 6.6 수명주기 / 종료

`App.axaml.cs` `desktop.Exit`에서 `_vehicleHub.StopAsync()` 후 `DisposeAsync()`. 연결 끊김 시 `WithAutomaticReconnect()`로 자동 재연결.

---

## 7. 참고: 동일 패턴의 부 채널 (Host 통신 로그)

동일한 구조(RabbitMQ → Subscriber → SignalR → UI)로 동작하는 채널이 하나 더 있다:

- **Hub**: `HostCommHub` (`/hubs/hostcomm`)
- **이벤트**: `"Log"`(통신 로그), `"Connection"`(연결 상태 변경)
- **서버 발행**: `ACS.App/Web/Realtime/HostCommSubscriber.cs` (exchange `/VM/DEMO/UI/HOSTCOMM`)
- **클라이언트**: `ACS.UI/Services/HostCommHubClient.cs`

본 문서 범위와 무관하므로 상세는 생략한다.

---

## 8. 핵심 파일 참조

| 역할 | 파일 |
|------|------|
| 서버 Hub 정의 | `src/ACS/ACS.App/Web/Hubs/VehicleHub.cs` |
| Hub 매핑/SignalR 등록 | `src/ACS/ACS.App/Program.cs` (`AddSignalR`, `MapHub`) |
| 서버 발행 (RabbitMQ → SignalR) | `src/ACS/ACS.App/Web/Realtime/PoseTelemetrySubscriber.cs` |
| TS → UI fanout forward | `src/ACS/ACS.Elsa/Workflows/Trans/RailVehicleUpdateWorkflow.cs` (`ForwardToUi`) |
| 원본 메시지 모델 | `src/ACS/ACS.Communication/Mqtt/Model/RailVehicleUpdateMessage.cs` |
| 클라이언트 연결/수신 | `src/ACS/ACS.UI/Services/VehicleHubClient.cs` |
| 수신 페이로드 DTO | `src/ACS/ACS.UI/Models/VehicleUpdateDto.cs` |
| UI 통합 | `src/ACS/ACS.UI/App.axaml.cs` |
| 적용/머지 | `src/ACS/ACS.UI/ViewModels/MapViewModel.cs` |
| Base URL 설정 | `src/ACS/ACS.UI/Services/BackendSettings.cs`, `src/ACS/ACS.UI/appsettings.json` |
