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

| 토픽 | 방향 | QoS | 설명 |
|------|------|-----|------|
| `amr/AMR001/status` | AMR → ACS | 1 | 로봇 상태 (주기적 퍼블리시, Retain). heartbeat 역할 겸함 |
| `amr/AMR001/reply` | AMR → ACS | 1 | 명령 응답 (ACCEPTED/EXECUTING/ARRIVED/COMPLETED/STEP_COMPLETE/REJECTED/FAILED/CANCELED) |
| `amr/AMR001/heartbeat` | AMR → ACS | 0 | (선택) 생존 신호. ACS 는 status/heartbeat 마지막 수신 시각으로 30초 무응답 시 DISCONNECT 처리 |
| `amr/AMR001/alarm`, `amr/AMR001/response` | AMR → ACS | 1 | ACS 가 구독하나 **현재 처리 워크플로 없음** (예약). 알람은 status.error 로 보고할 것 |
| `amr/AMR001/command` | ACS → AMR | 1 | 로봇 제어 명령 (moveCmd / actionCmd / cancelCmd) |

`amr/` prefix 는 ACS `NA_C_MQTT.TopicPrefix` 설정값, `AMR001` 은 `NA_C_MQTT.Name`(= 차량 CommId).

---

## Status (AMR → ACS)

**토픽:** `amr/AMR001/status`
**주기:** 1000ms (설정 가능)
**Retain:** true

> JSON 직렬화 규칙: camelCase 프로퍼티명, enum은 문자열로 직렬화

### JSON 구조

```json
{
  "state": {
    "runState": "Run",
    "fullState": "Full",
    "workState": "Idle",
    "vehicleDestNode": "N001"
  },
  
  "pose": {
    "x": 1.23,
    "y": 4.56,
    "angle": 0.78
  },
  
  "error": {
    "code": 0,
    "message": ""
  },
  
  "battery": {
    "levelPercent": 87.3,
    "voltage": 27.3,
    "current": 1.2,
    "temperatureCelsius": 32.1,
    "chargingState": "Charging"
  },
  
  "abnormal": {
    "type": "CHARGING_FAIL",
    "node": "N0001",
    "timestamp": "2026-03-25T16:05:05Z"
  } 
}
```

### 필드 상세

#### 최상위 필드

| 필드           | 타입     | 설명             |
|--------------|--------|----------------|
| `state`      | object | 로봇 동작 상태       |
| `error`      | object | 에러 코드 (0 = 정상) |
| `pose`       | object | 로봇 현재 위치       |
| `battery`    | object | 베터리 상태         |
| `abnormal`   | object | 비정상 상황 보고      |

#### `state` 객체

| 필드                  | 타입            | 단위 | 설명                         |
|---------------------|---------------|----|----------------------------|
| `runState`          | string (enum) | -  | Run / Stop (ACS: RunState 매핑, RUN→비RUN 전이가 도착 판정 트리거) |
| `fullState`         | string (enum) | -  | Full / Empty               |
| `workState`         | string (enum) | -  | Idle / Moving / Docking / Jog (숫자 1~4 도 허용, ACS 로그용) |
| `vehicleDestNode`   | string        | -  | 현재 설정된 목적지 (ACS: VehicleDestNodeId 참고용) |

#### `pose` 객체

| 필드 | 타입 | 단위 | 설명 |
|------|------|------|------|
| `x` | float | meters | X 좌표 |
| `y` | float | meters | Y 좌표 |
| `angle` | float | radian | 각도 |

#### `error` 객체

| 필드               | 타입     | 단위 | 설명                    |
|------------------|--------|----|-----------------------|
| `code`           | int    | -  | error code (0 = 정상, ≠0 → ACS 차량 ALARM) |
| `message`        | string | -  | error message         |

#### `battery` 객체

| 필드                  | 타입 | 단위        | 설명               |
|---------------------|------|-----------|------------------|
| `levelPercent`      | float | % (0~100) | 배터리 잔량           |
| `voltage`           | float | V         | 배터리 전압 V         |
| `current`           | float | A         | 배터리 전류 A (부호 있음) |
| `temperatureCelsius` | float | °C        | 배터리 온도           |
| `chargingState`     | string (enum) | -         | 충전 상태            |

### Enum 값 정의

#### `runState`

| 값      | 코드 | 설명    |
|--------|------|-------|
| `Stop` | 1 | 정지    |
| `Run`  | 2 | 시작    |

#### 'fullState'

| 값       | 코드 | 설명                  |
|---------|---|---------------------|
| `Empty` | 1 | 적재물 없음              |
| `Full`  | 2 | 적재중                 |


#### `workState`

| 값         | 코드 | 설명       |
|-----------|----|----------|
| `Idle`    | 1  | 대기 중     |
| `Moving`  | 2  | 이동 중     |
| `Docking` | 3  | 도킹 중     |
| `Jog`     | 4  | 조그 이동중   |

#### `chargingState`

| 값             | 코드 | 설명   |
|---------------|------|------|
| `Charging`    | 1 | 충전 중 |
| `Discharging` | 2 | 소비 중 |

---

## Command (ACS → AMR)

**토픽:** `amr/AMR001/command`

> 단일 토픽에 JSON 페이로드로 명령 종류와 파라미터를 전송한다. 값이 없는 선택 필드(`jobId`, `type`)는 생략된다.

### JSON 구조 (전체 필드)

```json
{
  "cmdId": "EX20260706103000123",   // 명령 일련번호 = ACS Job ID (한 job 의 모든 명령이 동일 값)
  "command": "moveCmd | actionCmd | cancelCmd",
  "jobId": "EX20260706103000123",   // (선택) actionCmd/cancelCmd — 진행 중 job 대조용 (= cmdId)
  "nodeId": "N0001",                // 명령 대상 노드 ID
  "port": "LEFT",                   // LEFT / RIGHT (선택)
  "jobType": "LOAD",                // TC 작업 유형: LOAD / UNLOAD / EXCHANGE
  "type": "UNLOAD",                 // (선택) actionCmd 액션 종류 (UNLOAD=취출 허가 / LOAD=투입 허가). EXCHANGE 는 jobType=EXCHANGE, type=UNLOAD|LOAD
  "portType": "EQP",                // LocationEx.Type 그대로: EQP / BUFFER / INPUT / OUTPUT / CHARGE / VBUFFER
  "model": "CF203W",                // 매거진 모델 (Offset 보정, 비어있을 수 있음)
  "amrSlot": 1                      // AMR 슬롯 1~4 (기본 1)
}
```

| 필드        | 타입   | 필수           | 설명                     |
|-----------|--------|--------------|------------------------|
| `cmdId`   | string | O            | 명령 일련번호. ACS 는 TC JobId(= MES JobID)를 그대로 사용. reply 에 그대로 반환할 것 |
| `command` | string | O            | `moveCmd` / `actionCmd` / `cancelCmd` |
| `jobId`   | string | actionCmd·cancelCmd | ACS Job ID (= cmdId). 진행 중 job 과 대조 |
| `nodeId`  | string | moveCmd·actionCmd | 대상 노드 (위치 태그) |
| `port`    | string | -            | LEFT or RIGHT (설비 포트) |
| `jobType` | string | -            | 목적지에 도착해서 할 일: LOAD / UNLOAD / EXCHANGE(설비행, 도착 후 actionCmd 대기) |
| `type`    | string | actionCmd    | UNLOAD(기존 매거진 취출 허가) / LOAD(신규 매거진 투입 허가). AMR 은 PICK/PLACE 결정에 `type` 우선, 없으면 `jobType` |
| `portType`| string | -            | 포트 유형 (`ACS-AMR_mqtt_movecmd.md` §PortType 시퀀스 참조). 미지정 시 자재포트 |
| `model`   | string | -            | 매거진 모델 |
| `amrSlot` | int    | -            | 조작 슬롯 1~4 (기본 1). EXCHANGE: 투입 1\|2, 회수 3\|4 |

### 명령 목록

#### `moveCmd` — 로봇 이동 명령 (상세: `ACS-AMR_mqtt_movecmd.md`)

```json
{"cmdId":"EX20260706103000123","command":"moveCmd","nodeId":"N0001","port":"LEFT",
 "jobType":"LOAD","portType":"EQP","model":"CF203W","amrSlot":1}
```

#### `actionCmd` — 로봇 행동 명령 (설비 게이트 허가, 상세: `ACS-AMR_mqtt_exchange.md` §4.2)

```json
{"cmdId":"EX20260706103000123","command":"actionCmd","jobId":"EX20260706103000123",
 "nodeId":"N0001","port":"LEFT","jobType":"EXCHANGE","type":"UNLOAD","model":"CF203W","amrSlot":3}
```

<<<<<<< Updated upstream
- MES ACTIONCMD(Type=UNLOAD/LOAD)를 ACS 가 중계. EXCHANGE 는 ACS 가 STEP=20 에서 UNLOAD, STEP=30 에서 LOAD 만 중계한다.
- AMR 은 portType=EQP 도착 후 대기 중인 게이트와 `type` 이 일치할 때만 수용하고, 그 외는 무시(로그)한다.

#### `cancelCmd` — 진행 중 명령 취소 (JOBCANCEL C2/C3, 상세: `ACS-AMR_mqtt_exchange.md` §4.3)

```json
{"cmdId":"EX20260706103000123","command":"cancelCmd","jobId":"EX20260706103000123"}
```

- AMR 은 진행 중 명령(moveCmd/actionCmd)을 폐기하고 **현 위치에 정지 후 대기(Idle) 상태로 복귀**한 뒤 `CANCELED` reply 를 발행한다.
- 진행 중 job 과 `jobId` 불일치/미진행이면 `CANCELED` resultCode=40(CANCEL_REJECTED).
- 복귀 이동(충전소 등)은 AMR 이 하지 않는다 — ACS 가 별도 moveCmd 로 지시한다.
=======
#### `exchangeCmd` — 매거진 교환 명령 (EXCHANGE 전용, ACS-AMR_mqtt_exchangecmd.docx §4)

3-waypoint(픽업/설비/반납)와 슬롯 배정을 단일 명령으로 전달하고, AMR 이 시퀀스 전체
(게이트 대기 포함)를 자율 수행하며 단계 보고(reply)로 진행을 알린다. Loc→NodeId 변환은 ACS 담당 (협의 #1 확정).

```json
{
  "cmdId": "20260811_103000_001",
  "command": "exchangeCmd",
  "jobId": "EX20260706103000123",
  "loadSourceNode": "N0010",
  "equipNode": "N0003",
  "unloadDestNode": "N0011",
  "port": "RIGHT",
  "model": "CF203W",
  "loadSlot": 1,
  "unloadSlot": 3,
  "loadSourcePortType": "MATERIAL",
  "unloadDestPortType": "MATERIAL"
}
```

- 수락 조건(모두 만족 시 ACCEPTED): Modbus 연결 · Idle · Cobot Auto/Run · 3개 노드 위치 태그 매핑 · loadSlot/unloadSlot 비어 있음
- 단계 매핑·게이트·오류 코드 상세는 `docs/ACS-AMR_mqtt_exchangecmd.docx` 참조

#### `actionCmd` EXCHANGE 게이트 확장 (§6)

교환 시퀀스 중 설비 준비 허가를 전달할 때 `type`/`jobId` 를 사용한다 (port/amrSlot 은 교환 시 무시):

```json
{"cmdId": "...", "command": "actionCmd", "jobId": "EX...", "type": "UNLOAD"}
```

- `type`: `UNLOAD` = 기존 매거진 취출 허가(게이트1) / `LOAD` = 신규 매거진 투입 허가(게이트2)
- AMR 은 현재 게이트 상태와 type 이 일치할 때만 수용, jobId 불일치 시 무시(로그만)

#### `cancelCmd` — 진행 중 명령 취소 (JOBCANCEL C2/C3, §7)

```json
{"cmdId": "...", "command": "cancelCmd", "jobId": "EX...", "returnNode": "N1001"}
```

- AMR 은 진행 중 명령/시퀀스를 폐기하고 정지한다. `jobId` 로 취소 대상 Job 을 식별한다.
- `returnNode`(선택): 적재 후 취소(C3) 시 복귀 노드. 생략 시 AMR 자동충전 노드 사용 (협의 #3).
- 신 스펙 AMR 은 reply `status=CANCELED`(정상 resultCode=0 / 거부 40 CANCEL_REJECTED) 로 회신한다.
  (구 스펙 시뮬레이터는 reply 없이 Idle 복귀 — 하위 호환.)
>>>>>>> Stashed changes

## Command Reply (AMR → ACS)

**토픽:** `amr/AMR001/reply`

<<<<<<< Updated upstream
모든 command 에 대한 진행/완료 응답. `cmdId` 는 받은 명령의 값을 그대로 반환한다.
확장 필드(`jobId`, `jobType`, `step`, `stepName`, `carrierSlot`)는 선택 — 없으면 ACS 가 TC 상태/STEP 으로 보완한다. (상세: `ACS-AMR_mqtt_exchange.md` §5)
=======
### EXCHANGE 단계 보고 확장 (ACS-AMR_mqtt_exchangecmd.docx §5)

exchangeCmd 진행 보고에는 기존 reply 에 4개 필드가 추가된다 (moveCmd 응답에는 없음 — null 생략):
`jobId`(Exchange Job ID), `step`(10/20/30/40/50/60), `stepName`, `carrierSlot`(STEP_COMPLETE 30/40/50 필수).
status 신규 값: `STEP_COMPLETE`(단계 완료), `CANCELED`(취소 처리 완료).

```json
{"cmdId":"...","jobId":"EX...","status":"STEP_COMPLETE","step":30,"stepName":"UNLOAD_OLD","carrierSlot":3,"resultCode":0,"message":"...","timestamp":"..."}
```

### 'moveCmd_Reply' - 로봇 이동 명령에 대한 응답
>>>>>>> Stashed changes

```json
{
  "cmdId": "EX20260706103000123",
  "status": "COMPLETED",   // ACCEPTED, EXECUTING, ARRIVED, COMPLETED, STEP_COMPLETE, REJECTED, FAILED, CANCELED
  "resultCode": 0,         // 0: 성공, 기타: 에러 코드 (아래 표)
  "message": "Success",    // 상세 사유
  "jobId": "EX20260706103000123",  // (선택) command.jobId
  "jobType": "UNLOAD",     // (선택) command.jobType
  "step": 30,              // (선택) EXCHANGE 단계 10~60. STEP_COMPLETE 에서는 필수
  "stepName": "UNLOAD_OLD",// (선택)
  "carrierSlot": 3,        // (선택) 조작한 AMR 슬롯 1~4
  "timestamp": "2026-08-19T10:31:20Z"
}
```

### status

| status | 의미 | ACS 처리 |
|---|---|---|
| ACCEPTED | 명령 수락 | 무시 |
| EXECUTING | 실행 시작 | 무시 |
| ARRIVED | 목적 노드 도착 (moveCmd) — 권장 | 도착 판정 진입점으로 수렴 (pose 판정과 OR, 중복 보고 방지) |
| COMPLETED | 명령 완료 — moveCmd 는 **작업(PICK/PLACE) 완료 시점**, actionCmd 는 액션 완료 시점 | jobType/TC 상태에 따라 acquire/deposit/exchange 완료 처리 |
| STEP_COMPLETE | COMPLETED 별칭 (step 필수) | COMPLETED 와 동일 |
| REJECTED | 수락 거부 | EXCHANGE 첫 moveCmd(STEP=10)면 배차 롤백·재배차, 그 외 로그(정지+운영자) |
| FAILED | 실패 종결 | EXCHANGE 픽업(STEP=10)면 MAGAZINE_NOT_FOUND 종결, 그 외 로그(정지+운영자) |
| CANCELED | cancelCmd 처리 결과 | 로그 (40 이면 경고) |

### resultCode

| resultCode | status | 의미 |
|---|---|---|
| 0 | - | 정상 |
| 2 | REJECTED | 지원하지 않는 command |
| 10 | REJECTED | AMR Modbus 미연결 |
| 11 | REJECTED | 작업 중 (Idle 아님) |
| 20 | REJECTED | NodeId 위치 태그 매핑 없음 |
| 21 | REJECTED | 슬롯 상태 불일치 (amrSlot 점유/비어있음) |
| 22 | REJECTED | Cobot 준비 안 됨 |
| 30 | FAILED | MAGAZINE_NOT_FOUND — 픽업지 매거진 부재 |
| 31 | FAILED | 시퀀스 중 슬롯/센서 상태 불일치 |
| 32 | FAILED | actionCmd 게이트 대기 상한 초과 (상한 설정 시) |
| 40 | CANCELED | CANCEL_REJECTED — 취소 불가 (종료 상태/jobId 불일치) |
| 99 | FAILED | 내부 예외 |
