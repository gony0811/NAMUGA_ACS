# CONTROL_STARTHEARTBEAT 워크플로우

## 개요

Primary 서버(CS01_P)에서 동일 하드웨어의 모든 관리 Application(DS, ES, TS, HS, UI 등)에 HeartBeat 메시지를 주기적으로 전송하고, 응답 여부에 따라 Application 상태를 관리하는 워크플로우.

## 트리거

- **커맨드명**: `CONTROL_STARTHEARTBEAT`
- **라우팅**: `elsa-migration.json`의 `ElsaCommands`에 등록되어 Elsa 워크플로우 엔진으로 라우팅
- **실행 방식**: `ElsaWorkflowManagerBridge` → `IWorkflowRunner.RunAsync()` (동기 실행)

## 데이터 흐름

```
[Quartz Scheduler / Control Server]
        │
        ▼
  ElsaWorkflowManagerBridge.Execute("CONTROL_STARTHEARTBEAT", XmlDocument)
        │
        ▼
  ┌─────────────────────────────────────────┐
  │  Step 1: ExtractHeartBeatInput          │
  │  - Input에서 CONTROL-HEARTBEAT XML 추출  │
  │  - IControlServerManager에서 설정 조회    │
  │    · HardwareType (PRIMARY/SECONDARY)    │
  │    · HeartBeatTimeout (밀리초)            │
  └─────────────┬───────────────────────────┘
                │
                ▼
  ┌─────────────────────────────────────────┐
  │  Step 2: GetApplicationsByHardware      │
  │  - IApplicationManager                   │
  │    .GetApplicationNamesByRunningHardware │
  │    (hardwareType)                        │
  │  - 결과: [DS01, ES01, TS01, HS01, UI01] │
  └─────────────┬───────────────────────────┘
                │
                ▼
  ┌─────────────────────────────────────────┐
  │  Step 3: ForEach Application            │
  │                                         │
  │  ┌───────────────────────────────────┐  │
  │  │ 3a. SendHeartBeat                 │  │
  │  │ - ISynchronousMessageAgent        │  │
  │  │   .Request(document, dest, timeout)│  │
  │  └──────────┬────────────────────────┘  │
  │             │                            │
  │        ┌────┴────┐                       │
  │        │         │                       │
  │     응답 O    응답 X                      │
  │        │         │                       │
  │        ▼         ▼                       │
  │  ┌──────────┐ ┌──────────────────────┐  │
  │  │ active   │ │ inactive 상태 변경    │  │
  │  │ 상태 갱신 │ │ + HeartBeatFail      │  │
  │  │ CheckTime│ │   History 기록        │  │
  │  │ 업데이트  │ │                      │  │
  │  └──────────┘ └──────────────────────┘  │
  │                                         │
  └─────────────────────────────────────────┘
```

## Application 상태 전이

```
         HeartBeat 응답 성공
  ┌──────────────────────────┐
  │                          │
  ▼                          │
[active] ──── HeartBeat 응답 실패 ───→ [inactive]
  ▲                                        │
  │          재시작/복구 후                   │
  └────────────────────────────────────────┘
```

| 현재 상태 | HeartBeat 결과 | 동작 |
|-----------|---------------|------|
| active    | 응답 성공      | CheckTime 갱신 |
| inactive  | 응답 성공      | → active로 변경, CheckTime 갱신 |
| hang      | 응답 성공      | → active로 변경, CheckTime 갱신 |
| active    | 응답 실패      | → inactive로 변경, HeartBeatFailHistory 기록 |
| inactive  | 응답 실패      | 상태 유지, HeartBeatFailHistory 기록 |

## 관련 인터페이스

| 인터페이스 | 역할 |
|-----------|------|
| `IControlServerManager` | HeartBeat 설정 (타임아웃, 재시도 횟수 등) |
| `IApplicationManager` | Application 상태 조회/업데이트 |
| `ISynchronousMessageAgent` | 동기 메시지 전송 (Request-Reply) |
| `IHistoryManagerEx` | HeartBeatFailHistory 기록 |

## 파일 위치

| 파일 | 설명 |
|------|------|
| `ACS.Elsa/Workflows/ControlStartHeartBeatWorkflow.cs` | C# 코드 기반 워크플로우 정의 |
| `ACS.Elsa/Workflows/Json/ControlStartHeartBeat.json` | JSON 기반 워크플로우 정의 (대안) |
| `ACS.Elsa/Activities/HeartBeatActivities.cs` | HeartBeat 관련 Elsa 액티비티 |
| `ACS.Elsa/elsa-migration.json` | Elsa 라우팅 설정 |

## 레거시 대응

이 워크플로우는 기존 Quartz 기반 `HeartBeatJob` (`ACS.App/Control/Scheduling/HeartBeatJob.cs`)과 `CONTROL_STARTHEARTBEAT` BizJob의 기능을 Elsa 워크플로우로 재구현한 것이다. 기존 Quartz 스케줄러에서 이 워크플로우를 호출하거나, Elsa의 자체 트리거를 통해 실행할 수 있다.

## JSON vs C# 워크플로우

| 방식 | 장점 | 단점 |
|------|------|------|
| C# 코드 (`WorkflowBase`) | 컴파일 타임 검증, IDE 지원, 복잡한 로직 표현 | 변경 시 재빌드 필요 |
| JSON 파일 | 런타임 변경 가능, Elsa Studio에서 편집 | 타입 안전성 부족 |

현재 `CONTROL_STARTHEARTBEAT` (C# 코드)가 기본 활성화되어 있고, `CONTROL_STARTHEARTBEAT_JSON` (JSON)은 대안으로 사용 가능하다.
