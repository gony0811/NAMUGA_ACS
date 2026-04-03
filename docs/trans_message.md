# RAIL-VEHICLEUPDATE JSON 메시지 사양

## 1. 개요

EI 프로세스가 MQTT로 수신한 AMR 상태 메시지를 Trans 프로세스에 JSON 형식으로 전달하는 메시지이다.
AMR의 모든 상태(RunState, FullState, AlarmState, Battery)와 위치(CurrentNodeId)를 하나의 메시지로 통합하여 Trans에서 일괄 DB 업데이트한다.

## 2. 메시지 플로우

```
AMR                    EI 프로세스                          Trans 프로세스
 │                        │                                    │
 │  MQTT status 토픽      │                                    │
 │  amr/{vehicleId}/status│                                    │
 ├───────────────────────>│                                    │
 │                        │  VEHICLE-MESSAGERECEIVED 워크플로우  │
 │                        │  SendAmrVehicleUpdateActivity       │
 │                        │                                    │
 │                        │  RAIL-VEHICLEUPDATE JSON            │
 │                        │  (RabbitMQ)                         │
 │                        ├───────────────────────────────────>│
 │                        │                                    │  ESListener JSON 감지
 │                        │                                    │  RAIL-VEHICLEUPDATE 워크플로우
 │                        │                                    │  RailVehicleUpdateActivity
 │                        │                                    │  → DB 업데이트
```

## 3. JSON 구조

### 3.1 전체 구조

```json
{
  "header": {
    "messageName": "RAIL-VEHICLEUPDATE",
    "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "timestamp": "2026-04-03T10:30:45.123Z",
    "sender": "EI"
  },
  "data": {
    "vehicleId": "AMR001",
    "commId": "amr001",
    "runState": "RUN",
    "fullState": "EMPTY",
    "alarmState": "NOALARM",
    "batteryRate": 85,
    "batteryVoltage": 27.3,
    "vehicleDestNodeId": "N0002",
    "currentNodeId": "N0001",
    "nodeChanged": true,
    "connectionState": "CONNECT",
    "eventTime": "2026-04-03T10:30:45.123Z"
  }
}
```

### 3.2 Header 필드

| 필드 | 타입 | 설명 | 값 |
|------|------|------|----|
| `messageName` | string | 메시지 이름 | 고정값 `"RAIL-VEHICLEUPDATE"` |
| `transactionId` | string | 트랜잭션 ID | GUID (`Guid.NewGuid()`) |
| `timestamp` | DateTime | 메시지 생성 시각 | UTC (`DateTime.UtcNow`) |
| `sender` | string | 송신 프로세스 | 고정값 `"EI"` |

### 3.3 Data 필드

| 필드 | 타입 | 설명 | 값 범위 |
|------|------|------|---------|
| `vehicleId` | string | DB PK (VehicleEx.VehicleId) | 예: `"AMR001"` |
| `commId` | string | MQTT vehicleId (VehicleEx.CommId) | 예: `"amr001"` |
| `runState` | string | 주행 상태 | `"RUN"` / `"STOP"` |
| `fullState` | string | 적재 상태 | `"FULL"` / `"EMPTY"` |
| `alarmState` | string | 알람 상태 | `"NOALARM"` / `"ALARM"` |
| `batteryRate` | int | 배터리 잔량 (%) | 0 ~ 100 |
| `batteryVoltage` | float | 배터리 전압 (V) | 예: `27.3` |
| `vehicleDestNodeId` | string | 목적지 노드 ID | 예: `"N0002"` |
| `currentNodeId` | string | 현재 노드 ID (노드 변경 시에만 설정) | 예: `"N0001"` 또는 `null` |
| `nodeChanged` | bool | 노드 변경 여부 플래그 | `true` / `false` |
| `connectionState` | string | 연결 상태 | 고정값 `"CONNECT"` |
| `eventTime` | DateTime | 이벤트 발생 시각 | UTC (`DateTime.UtcNow`) |

## 4. 상태값 매핑

EI의 `SendAmrVehicleUpdateActivity`에서 AMR MQTT 상태값을 ACS 내부 상태값으로 변환한다.

### 4.1 RunState

| AMR 값 (MQTT) | ACS 값 (JSON) | 비고 |
|---------------|---------------|------|
| `"Run"` | `"RUN"` | VehicleEx.RUNSTATE_RUN |
| `"Stop"` | `"STOP"` | VehicleEx.RUNSTATE_STOP |
| 기타/null | 기존 DB 값 유지 | 매핑 실패 시 변경하지 않음 |

### 4.2 FullState

| AMR 값 (MQTT) | ACS 값 (JSON) | 비고 |
|---------------|---------------|------|
| `"Full"` | `"FULL"` | VehicleEx.FULLSTATE_FULL |
| `"Empty"` | `"EMPTY"` | VehicleEx.FULLSTATE_EMPTY |
| 기타/null | 기존 DB 값 유지 | 매핑 실패 시 변경하지 않음 |

### 4.3 AlarmState

| 조건 | ACS 값 (JSON) | 비고 |
|------|---------------|------|
| `Error.Code == 0` | `"NOALARM"` | VehicleEx.ALARMSTATE_NOALARM |
| `Error.Code != 0` | `"ALARM"` | VehicleEx.ALARMSTATE_ALARM |

## 5. 노드 변경 감지

`SendAmrVehicleUpdateActivity`에서 AMR의 Pose(X, Y) 좌표로 최근접 노드를 판별한다.

1. `NearestNodeFinder.FindNearestNode()`로 유클리드 거리 기준 최근접 노드 탐색
2. 임계 거리: **2.0m** (기본값, 설정: `Acs:Amr:NearestNodeThresholdMeters`)
3. 임계 거리 내 노드가 없으면 `currentNodeId = null`, `nodeChanged = false`
4. 최근접 노드가 기존 `vehicle.CurrentNodeId`와 다르면:
   - `currentNodeId = 새 노드 ID`
   - `nodeChanged = true`
5. 같으면 `currentNodeId = null`, `nodeChanged = false`

## 6. Trans 수신 처리

`ESListener`에서 RabbitMQ 메시지가 `{`로 시작하면 JSON으로 판별하여 `header.messageName`으로 워크플로우를 라우팅한다.

`RailVehicleUpdateActivity`에서 다음 순서로 업데이트:

| 순서 | 항목 | 조건 |
|------|------|------|
| 1 | ConnectionState | 기존 값이 `"CONNECT"`가 아닐 때 |
| 2 | RunState | 값이 다를 때 |
| 3 | FullState | 값이 다를 때 |
| 4 | AlarmState | 값이 다를 때 |
| 5 | BatteryRate | 값이 다를 때 |
| 6 | BatteryVoltage | 차이 > 0.01 일 때 |
| 7 | VehicleDestNodeId | 값이 다를 때 |
| 8 | CurrentNodeId | `nodeChanged == true` 이고 노드가 캐시에 존재할 때 |
| 9 | EventTime | 항상 업데이트 |

## 7. 관련 소스

| 파일 | 설명 |
|------|------|
| `ACS.Communication/Mqtt/Model/RailVehicleUpdateMessage.cs` | JSON 모델 클래스 |
| `ACS.Elsa/Activities/MqttActivities.cs` (SendAmrVehicleUpdateActivity) | EI: 메시지 생성 및 전송 |
| `ACS.Elsa/Workflows/Ei/VehicleMessageReceivedWorkflow.cs` | EI: VEHICLE-MESSAGERECEIVED 워크플로우 |
| `ACS.Elsa/Workflows/Trans/RailVehicleUpdateWorkflow.cs` | Trans: 수신 워크플로우 및 DB 업데이트 |
| `ACS.Communication/Msb/RabbitMQ/Marker/ESListener.cs` | Trans: JSON 메시지 감지 및 라우팅 |
| `ACS.Manager/Message/MessageManagerExImplement.cs` | `SendVehicleUpdateJson()` 전송 구현 |
| `ACS.Core/Path/NearestNodeFinder.cs` | 최근접 노드 탐색 |
