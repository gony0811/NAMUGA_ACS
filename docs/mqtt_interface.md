# MQTT Interface Specification

AMR과 ACS 간 MQTT 통신 인터페이스 정의서

## 연결 정보

| 항목 | 값 |
|------|-----|
| Broker | `localhost:1883` (기본값) |
| Client ID | `AMR001` |
| QoS | 1 (At Least Once) |
| Clean Session | true |

---

## 토픽 구조

| 토픽 | 방향 | 설명 |
|------|------|------|
| `amr/AMR001/status` | AMR → ACS | 로봇 상태 (주기적 퍼블리시, Retain) |
| `amr/AMR001/command` | ACS → AMR | 로봇 제어 명령 |

---

## Status (AMR → ACS)

**토픽:** `amr/AMR001/status`
**주기:** 1000ms (설정 가능)
**Retain:** true

> JSON 직렬화 규칙: camelCase 프로퍼티명, enum은 문자열로 직렬화

### JSON 구조

```json
{
  "robotState": "Running",
  "errorCode": 0,
  "pose": {
    "x": 1.23,
    "y": 4.56,
    "angle": 0.78
  },
  "mapStatusPercent": 95.5,
  "navigationStatus": "Moving",
  "battery": {
    "voltagePercent": 87.3,
    "currentS": 1.2,
    "currentA": -0.5,
    "temperatureCelsius": 32.1,
    "chargingState": "Charging"
  }
}
```

### 필드 상세

#### 최상위 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `robotState` | string (enum) | 로봇 동작 상태 |
| `errorCode` | number (ushort) | 에러 코드 (0 = 정상) |
| `pose` | object | 로봇 현재 위치 |
| `mapStatusPercent` | number (float) | 맵 상태 (0~100%) |
| `navigationStatus` | string (enum) | 주행 상태 |
| `battery` | object | 배터리 상태 |

#### `pose` 객체

| 필드 | 타입 | 단위 | 설명 |
|------|------|------|------|
| `x` | float | meters | X 좌표 |
| `y` | float | meters | Y 좌표 |
| `angle` | float | radian | 각도 |

#### `battery` 객체

| 필드 | 타입 | 단위 | 설명 |
|------|------|------|------|
| `voltagePercent` | float | % (0~100) | 배터리 잔량 |
| `currentS` | float | A | 배터리 전류 S |
| `currentA` | float | A | 배터리 전류 A (부호 있음) |
| `temperatureCelsius` | float | °C | 배터리 온도 |
| `chargingState` | string (enum) | - | 충전 상태 |

### Enum 값 정의

#### `robotState`

| 값 | 코드 | 설명 |
|----|------|------|
| `Off` | 1 | 정지 |
| `Running` | 2 | 주행 중 |
| `Paused` | 3 | 일시 정지 |

#### `navigationStatus`

| 값 | 코드 | 설명 |
|----|------|------|
| `WaitingForArrival` | 1 | 도착 대기 |
| `Moving` | 2 | 이동 중 |

#### `chargingState`

| 값 | 코드 | 설명 |
|----|------|------|
| `Charging` | 1 | 충전 중 |
| `FullyCharged` | 2 | 완충 |

---

## Command (ACS → AMR)

**토픽:** `amr/AMR001/command`

> 단일 토픽에 JSON 페이로드로 명령 종류와 파라미터를 전송한다.

### JSON 구조

```json
{
  "cmdId": "20260325_160501_001 (년월일 시분초_일련번호)",
  "command": "명령이름",
  "nodeId": "명령 대상 노드 ID (ex: N0001)",
  "port": "LEFT/RIGHT (선택적)",
  "jobType": "LOAD/UNLOAD/EXCHANGE"
}
```

| 필드        | 타입     | 필수           | 설명                     |
|-----------|--------|--------------|------------------------|
| `cmdId`   | string | O            | 명령 일련번호 (년월일_시분초_일련번호)  |  
| `command` | string | O            | 명령 종류                  |
| `nodeId`  | string | 명령에 따라 다름    | 단일 값 파라미터              |
| `port`    | string  | Port의 위치     | LEFT or RIGHT          |
| `jobType` | string  | 목적지에 도착해서 할 일| LOAD, UNLOAD, EXCHANGE |

### 명령 목록

#### `moveCmd` — 로봇 이동 명령

```json
{
  "cmdId": "20260325_160501_001", 
  "command": "moveCmd",
  "nodeId": "N0001",
  "port": "LEFT",
  "jobType": "LOAD"
}
```

#### `actionCmd` — 로봇 행동 명령

```json
{"command": "actionCmd", "nodeId": "N0001", "port": "LEFT", "JobType": "UNLOAD"}
```

## Command Reply (AMR → ACS)

**토픽:** `amr/AMR001/reply`

### 'moveCmd_Reply' - 로봇 이동 명령에 대한 응답

```json
{
  "cmdId": "20260325_160501_001",
  "status": "ACCEPTED", // ACCEPTED, REJECTED, EXECUTING, COMPLETED, FAILED
  "resultCode": 0,      // 0: 성공, 기타: 에러 코드
  "message": "Success",  // 상세 사유 (거부 시 사유 등)
  "timestamp": "2026-03-25T16:05:05Z"
}
```