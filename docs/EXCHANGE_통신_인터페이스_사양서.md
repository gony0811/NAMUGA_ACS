# NAMUGA ACS — EXCHANGE 통신 인터페이스 사양서

> MES ↔ HS ↔ TS ↔ EI ↔ AMR 전 구간 통신 인터페이스 및 프로토콜 통합 정의
> 대상 사양: **EXCHANGECMD** (매거진 교체 — 기존 매거진 취출과 신규 매거진 투입을 하나의 Job 으로 동시 진행)
> 작성일: 2026-07-13 · 상태: **신규 사양 (구현 예정)**

---

## 1. 개요

EXCHANGE 는 하나의 설비 포트에서 **기존 매거진 취출(UNLOAD)** 과 **신규 매거진 투입(LOAD)** 을 한 번의 AMR 방문으로 처리하는 신규 사양이다. 기존 MOVECMD 는 LOAD/UNLOAD 를 각각 별개의 Job 으로 요청했으나, EXCHANGE 는 단일 `EXCHANGECMD` 로 다음 5개 물리 단계를 하나의 ACS Job 으로 묶는다.

```
신규 매거진 픽업(투입슬롯 1·2) → 설비 이동 → 기존 매거진 취출(회수슬롯 3·4) → 신규 매거진 투입(투입슬롯) → 기존 매거진 반납(회수슬롯)
```

이 문서는 EXCHANGECMD 수신 시점부터 완료 보고까지, 아래 5개 프로세스 사이를 오가는 **명령 흐름(정방향)** 과 **보고·상태 흐름(역방향)** 의 전체 인터페이스·프로토콜을 하나로 정리한다.

### 1.1 프로세스 구성

| 약칭 | 프로세스 | 역할 | 문서 내 표기 |
|---|---|---|---|
| MES | 상위 제조 실행 시스템 | 설비 요청을 받아 ACS 에 교체 작업 지시, 단계 보고 수신 | 외부 |
| **HS** | Host (ACS.App, `Acs:Process:Type=host`) | MES 와의 XML 프로토콜 종단. EXCHANGECMD 파싱 → TransportCommand 생성, JOBREPORT 를 MES 로 송신 | Host |
| **TS** | Trans (`Acs:Process:Type=trans`) | 도메인 상태 관리(NA_T_TRANSPORTCMD, NA_R_VEHICLE), Vehicle 할당·경로·재푸시, 단계 판정 | Trans |
| **EI** | Trans-EI / ES (`Acs:Process:Type=ei`) | RabbitMQ ↔ MQTT 브릿지. RAIL-* JSON 을 MQTT moveCmd/actionCmd 로 변환, AMR status/reply 를 RAIL-* 로 변환 | EI |
| AMR | 자율주행 로봇 | 실제 픽업/플레이스/이동 수행. 매거진 슬롯 4개(1·2 투입 / 3·4 회수) 운용 | 외부 |

### 1.2 홉(Hop) 요약

```
[정방향 — 명령]
MES ──EXCHANGECMD(XML)──▶ HS ──내부 워크플로우──▶ TS ──RAIL-CARRIERTRANSFER(JSON)──▶ EI ──moveCmd/actionCmd(MQTT)──▶ AMR

[역방향 — 보고/상태]
AMR ──reply/status(MQTT)──▶ EI ──RAIL-VEHICLEUPDATE / *REPLY / *ABNORMAL(JSON)──▶ TS ──내부──▶ HS ──JOBREPORT(XML)──▶ MES
```

---

## 2. 전체 통신 아키텍처 (시퀀스)

아래는 EXCHANGE 1건의 전체 라이프사이클이다. 좌→우로 5개 프로세스, 위→아래로 시간 순.

```
EQUIP        MES              HS(Host)          TS(Trans)          EI              AMR
  │           │                 │                 │                 │               │
  │ EXCHANGE_REQUEST            │                 │                 │               │
  ├──────────▶│                 │                 │                 │               │
  │           │ EXCHANGECMD(XML)│                 │                 │               │
  │           ├────────────────▶│                 │                 │               │
  │           │                 │ TransportCommand 생성 (JobID)     │               │
  │           │                 ├────────────────▶│                 │               │
  │           │ JOBREPORT/RECEIVE Step=10          │                 │               │
  │           │◀────────────────┤                 │                 │               │
  │           │                 │                 │ Vehicle 할당     │               │
  │           │                 │                 │ RAIL-CARRIERTRANSFER(jobType=EXCHANGE)
  │           │                 │                 ├────────────────▶│ moveCmd(EXCHANGE, portType=EQP)
  │           │                 │                 │                 ├──────────────▶│ 신규 매거진 픽업(투입슬롯 1)
  │           │                 │                 │                 │◀──reply───────┤ → 설비로 이동
  │           │                 │                 │  status(node 도착)                │
  │           │                 │                 │◀──RAIL-VEHICLEUPDATE─────────────┤
  │           │ JOBREPORT/ARRIVED Step=20          │                 │               │
  │           │◀────────────────┤◀────────────────┤                 │  (EQP 도착, ActionCmd 대기)
  │  ...MES가 설비에 EXCHANGE_READY / UNLOAD_READY 전송...            │               │
  │ FINAL_UNLOAD_REQUEST        │                 │                 │               │
  ├──────────▶│ ACTIONCMD(UNLOAD)│                │                 │               │
  │           ├────────────────▶│────────────────▶│ actionCmd(UNLOAD)               │
  │           │                 │                 ├────────────────▶├──────────────▶│ 기존 매거진 취출(회수슬롯 3)
  │           │ JOBREPORT/STEP_COMPLETE UNLOAD Step=30, Slot=3       │◀──reply───────┤
  │           │◀────────────────┤◀────────────────┤                 │               │
  │ UPLOAD_REQUEST              │ ACTIONCMD(LOAD) │                 │               │
  ├──────────▶│────────────────▶│────────────────▶│ actionCmd(LOAD) │               │
  │           │                 │                 ├────────────────▶├──────────────▶│ 신규 매거진 투입(투입슬롯 1)
  │           │ JOBREPORT/STEP_COMPLETE LOAD Step=40, Slot=1         │◀──reply───────┤
  │           │◀────────────────┤◀────────────────┤                 │               │
  │           │                 │                 │ moveCmd(MOVE → UnloadDestLoc)     │
  │           │                 │                 ├────────────────▶├──────────────▶│ 기존 매거진 반납(회수슬롯 3)
  │           │ JOBREPORT/STEP_COMPLETE MOVE Step=50, Slot=3         │◀──reply───────┤
  │           │◀────────────────┤◀────────────────┤                 │               │
  │           │ JOBREPORT/COMPLETE EXCHANGE Step=60                  │               │
  │           │◀────────────────┤◀────────────────┤                 │               │
```

> **주의 (Excel Scenario 시트 §주의):** UNLOAD 와 LOAD 는 설비 기구 상태 때문에 반드시 설비의 후속 요청(`FINAL_UNLOAD_REQUEST`, `UPLOAD_REQUEST`)을 받은 뒤 실행한다. 즉 Step 30/40 은 AMR 이 설비에 도착(Step 20)한 뒤에도 설비 준비 신호를 기다렸다가 진행한다. 이 대기는 AMR MQTT 규약의 `portType=EQP` 도착 후 **ActionCmd 최대 120초 대기** 동작으로 구현된다.

---

## 3. 프로토콜 매트릭스

| 구간 | 방향 | 메시지 | 전송 방식 | 포맷 | 채널 / 토픽 |
|---|---|---|---|---|---|
| MES ↔ HS | MES→ACS | `EXCHANGECMD`, `ACTIONCMD`, `MOVECANCEL` | MSB (RabbitMQ) | **XML** `<Msg>` | `DestSubject` = `/HQ/ACS01` |
| MES ↔ HS | ACS→MES | `JOBREPORT` | MSB (RabbitMQ) | **XML** `<Msg>` | `DestSubject` = `/HQ/MES01` |
| HS ↔ TS | 내부 | TransportCommand 생성/워크플로우 라우팅 | MSB (RabbitMQ) | XML/JSON | Elsa 워크플로우 (DefinitionId 라우팅) |
| TS → EI | TS→EI | `RAIL-CARRIERTRANSFER` | MSB (RabbitMQ) | **JSON** | `application.DestinationName + "/" + Name` |
| TS → EI | TS→EI | `RAIL-ACTIONCMD` *(신규, §7.4)* | MSB (RabbitMQ) | **JSON** | 상동 |
| EI → TS | EI→TS | `RAIL-CARRIERTRANSFERREPLY`, `RAIL-VEHICLEUPDATE`, `RAIL-VEHICLEABNORMAL` | MSB (RabbitMQ) | **JSON** | `ESListener` (header.messageName 라우팅) |
| EI ↔ AMR | EI→AMR | `moveCmd`, `actionCmd` | **MQTT** (QoS 1) | JSON | `amr/{ClientId}/command` |
| EI ↔ AMR | AMR→EI | reply | MQTT | JSON | `amr/{ClientId}/reply` |
| EI ↔ AMR | AMR→EI | status (+ `abnormal`) | MQTT (Retain) | JSON | `amr/{ClientId}/status` |

핵심 규칙:

- **MES 경계는 XML, ACS 내부(TS↔EI)와 AMR 경계는 JSON.** HS 가 두 세계를 번역한다.
- **도메인 상태 변경은 전적으로 TS 책임.** EI 는 프로토콜 변환·전달만 하며 DB 를 직접 바꾸지 않는다 (기존 RAIL-VEHICLEUPDATE / RAIL-VEHICLEABNORMAL 과 동일한 분리 패턴).
- JobID 는 EXCHANGECMD 가 발급한 값이 전 구간 상관키(correlation key)로 사용된다. HS 의 TransportCommand JobId, TS 의 `commandId`, JOBREPORT 의 `JobID` 가 모두 동일해야 한다.

---

## 4. 정방향 — 명령 흐름

### 4.1 MES → HS : `EXCHANGECMD` (XML)

교체 작업 전체를 한 번에 지시한다.

```xml
<Msg>
  <Command>EXCHANGECMD</Command>
  <Header>
    <DestSubject>/HQ/ACS01</DestSubject>      <!-- MES → ACS -->
    <ReplySubject>/HQ/MES01</ReplySubject>    <!-- ACS → MES -->
  </Header>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <JobID>EX20260706103000123</JobID>                                  <!-- 전 구간 상관키 -->
    <EquipID>192.168.32.36</EquipID>                                    <!-- 대상 설비 IP 또는 ID -->
    <Port>RIGHT</Port>                                                  <!-- LEFT / RIGHT -->
    <Model>CF203W</Model>                                               <!-- 매거진 모델 -->
    <LoadEquipJobID>PRD-MXFOCUSWIDE01_LOAD_20260706103000</LoadEquipJobID>     <!-- 설비 LOAD 보고용 -->
    <UnloadEquipJobID>PRD-MXFOCUSWIDE01_UNLOAD_20260706103000</UnloadEquipJobID><!-- 설비 UNLOAD 보고용 -->
    <LoadSourceLoc>IN_BUF_01</LoadSourceLoc>                            <!-- 신규 매거진 픽업 위치 -->
    <UnloadDestLoc>OUT_BUF_01</UnloadDestLoc>                           <!-- 기존 매거진 반납 위치 -->
    <LoadCarrierSlot>1</LoadCarrierSlot>                                <!-- 신규 매거진 투입슬롯 (1|2) -->
    <UnloadCarrierSlot>3</UnloadCarrierSlot>                            <!-- 기존 매거진 회수슬롯 (3|4) -->
    <MaterialType>MAGAZINE</MaterialType>
    <ActionType>EXCHANGE</ActionType>
    <UserID>MES01</UserID>
  </DataLayer>
</Msg>
```

| 필드 | 타입 | 필수 | 의미 |
|---|---|---|---|
| `AcsId` | String | O | 대상 ACS 프로세스명 |
| `JobID` | String | O | Exchange TransportCommand Id (ACS 기준 Job ID, 상관키) |
| `EquipID` | String | O | 대상 설비 IP 또는 설비 ID |
| `Port` | String | O | 대상 설비 포트 `LEFT` / `RIGHT` |
| `Model` | String | O | 매거진 모델명 |
| `LoadEquipJobID` | String | O | 설비 LOAD 보고용 JobID |
| `UnloadEquipJobID` | String | O | 설비 UNLOAD 보고용 JobID |
| `LoadSourceLoc` | String | O | 신규 매거진 픽업 위치 ID |
| `UnloadDestLoc` | String | O | 기존 매거진 반납 위치 ID |
| `LoadCarrierSlot` | String | - | **공백 허용 — ACS 자동배정 (2026-07-22 확정)**. 값이 있어도 ACS 는 자동배정하며 사용 슬롯은 JOBREPORT `CarrierSlot` 으로 통보 |
| `UnloadCarrierSlot` | String | O | 회수 매거진용 AMR 슬롯 (`3` 또는 `4` — 회수슬롯) |
| `MaterialType` | String | O | `MAGAZINE` |
| `ActionType` | String | O | `EXCHANGE` |
| `UserID` | String | O | MES 프로세스명 |

### 4.2 HS 내부 처리 — TransportCommand 생성

HS 는 `CreateTransportCommandActivity` (참조: `src/ACS/ACS.Elsa/Activities/HostActivities.cs`) 계열 로직으로 EXCHANGECMD 를 검증·해석한다. 기존 MOVECMD 처리와 공유하는 규칙:

- **JobID 중복 검증** — 동일 JobID 존재 시 `102 COMMANDALREADYREQUESTED` NACK.
- **위치 존재/타입 검증** — `LoadSourceLoc`, `UnloadDestLoc`, `EquipID:Port` 가 `NA_R_LOCATION`/`NA_R_STATION` 에 등록되어 있어야 함.
- **source == dest 차단** — `106 SOURCEDESTMACHINEDUPLICATE`.
- **Bay 정합** — Source/Dest 가 공통 Bay 없으면 `22 NOTSAMEBAY`.

MOVECMD 와 달리 EXCHANGE 는 단일 명령에 **두 개의 물리 캐리어(신규/기존)와 투입/회수 슬롯** 이 관여하므로, TransportCommand 는 `JobType=EXCHANGE` 로 생성하고 `Model`, `LoadCarrierSlot`, `UnloadCarrierSlot`, `LoadSourceLoc`, `UnloadDestLoc` 를 함께 보관한다.

> **BUFFER 명명규칙 (movecmd 송신 규약):** 버퍼 Location ID 는 `{역할}_BUF{번호}` 형식(`IN_BUF01`, `OUT_BUF01`). 같은 물리 테이블의 IN/OUT 은 동일 번호를 공유한다. `IN` = 버퍼로 들어오는 방향(설비 반출물 하치), `OUT` = 버퍼에서 나가는 방향(설비 투입물 픽업). ACS 는 IN/OUT prefix 를 강제 검증하지 않으며 등록된 Location ID 면 동작한다.

접수 성공 시 즉시 **JOBREPORT/RECEIVE (Step=10)** 을 MES 로 송신하고(§6), TransportCommand 를 TS 로 넘긴다.

### 4.3 HS → TS : 워크플로우 라우팅

HS 가 생성한 TransportCommand 를 TS 가 인계받아 Vehicle 할당·경로 계산을 수행한다. 라우팅은 Elsa 워크플로우 엔진이 `Header.MessageName` (DefinitionId) 으로 처리하며, TS 는 할당 결정 후 EI 로 반송 명령(RAIL-CARRIERTRANSFER)을 발행한다. 이 구간은 프로세스 내부 MSB(RabbitMQ) 통신으로, MES 로 노출되지 않는다.

### 4.4 TS → EI : `RAIL-CARRIERTRANSFER` (JSON)

TS 가 할당된 Vehicle 에게 이동/반송을 지시하는 실제 명령. 모델: `RailCarrierTransferMessage` (`src/ACS/ACS.Communication/Mqtt/Model/RailCarrierTransferMessage.cs`).

```json
{
  "header": {
    "messageName": "RAIL-CARRIERTRANSFER",
    "transactionId": "<Guid>",
    "timestamp": "<UTC>",
    "sender": "TS"
  },
  "data": {
    "commandId":  "EX20260706103000123",     // = EXCHANGECMD JobID
    "vehicleId":  "AMR001",
    "destPortId": "192.168.32.36:RIGHT",      // eqpId:portId
    "destNodeId": "<Station ID>",
    "priority":   "0",
    "carrierType":"MAGAZINE",
    "port":       "RIGHT",                     // LEFT / RIGHT
    "jobType":    "EXCHANGE",                  // LOAD / UNLOAD / EXCHANGE
    "portType":   "EQP",                       // LocationEx.Type: EQP/BUFFER/INPUT/OUTPUT/CHARGE/VBUFFER
    "model":      "CF203W",
    "resultCode": ""                           // 초기 전송 시 빈 문자열
  }
}
```

- `jobType` 은 이미 `EXCHANGE` 값을 지원한다(모델 주석 기준). 신규 매거진 픽업 단계는 `LoadSourceLoc`(portType=BUFFER 계열) 목적지의 별도 반송으로, 설비 도착 단계는 `EquipID:Port`(portType=EQP) 목적지로 전송한다.
- **송신 경로** (`MessageManagerExImplement.SendCarrierTransferJson`): `data.vehicleId` → `VehicleEx.CommId` → `MqttConfig`·`Application` 조회 → `destinationName = DestinationName + "/" + Name` 조립 → `esAgent.Send(...)` 로 EI 에 단발 publish. 신규 할당 경로는 `SendCarrierTransferWithRetryActivity` 로 **5초 timeout × 3회 재시도**, stuck 복구 재푸시는 단발 송신.

### 4.5 EI → AMR : MQTT `moveCmd` / `actionCmd`

EI 는 RAIL-CARRIERTRANSFER 를 MQTT 명령으로 변환하여 `amr/{ClientId}/command` 에 발행한다.

**`moveCmd` — 이동 (설비/버퍼로 이동)**

```json
{
  "cmdId": "20260706_103005_001",
  "command": "moveCmd",
  "nodeId": "N0001",
  "port": "RIGHT",
  "jobType": "EXCHANGE",
  "portType": "EQP",
  "amrSlot": 1
}
```

| 필드 | 타입 | 필수 | 설명 |
|---|---|---|---|
| `cmdId` | string | O | 명령 일련번호 `년월일_시분초_일련번호` |
| `command` | string | O | `moveCmd` 고정 |
| `nodeId` | string | O | 목적지 위치 태그. AMR 이 매핑 테이블에서 TaskIndex/JobIndex 로 변환 |
| `port` | string | - | `LEFT` / `RIGHT` |
| `jobType` | string | - | `LOAD` / `UNLOAD` / `EXCHANGE` |
| `portType` | string | - | `EQP`(설비, 도착 후 ActionCmd 120초 대기) / `BUFFER`·`INPUT`·`OUTPUT`·`VBUFFER`(자재포트, 즉시 진행) / `CHARGE`(충전) |
| `amrSlot` | int | - | AMR 슬롯 1~4 (기본 1). 투입슬롯=1·2, 회수슬롯=3·4 (D3 확정) |

**`actionCmd` — 설비에서의 취출/투입 (설비 준비 신호 수신 후)**

```json
{ "command": "actionCmd", "nodeId": "N0001", "port": "RIGHT", "jobType": "UNLOAD" }
```

- `portType=EQP` 로 도착한 AMR 은 최대 120초간 actionCmd 를 대기한다. EI 는 MES→ACS 의 설비 준비 신호(FINAL_UNLOAD_REQUEST → UNLOAD, UPLOAD_REQUEST → LOAD)에 맞춰 actionCmd 를 발행한다.
- **Cobot DI 매핑** (`AMR/Service/MoveSequenceRunner.cs`): `jobType`/`port`/`amrSlot` 조합으로 PICK/PLACE DI 결정. 예) UNLOAD·EQP·LEFT → 설비 PICK DI10, PLACE 는 AMR 슬롯 `4+amrSlotOffset`. LOAD·EQP·LEFT → 설비 PLACE DI8, PICK 은 AMR 슬롯 `0+amrSlotOffset`.

**AMR reply (`amr/{ClientId}/reply`)** — §5.1.

---

## 5. 역방향 — 보고 / 상태 흐름

### 5.1 AMR → EI : MQTT reply / status

**명령 응답 (`amr/{ClientId}/reply`)**

```json
{
  "cmdId": "20260706_103005_001",
  "status": "ACCEPTED",
  "resultCode": 0,
  "message": "이동 명령 수락: N0001 (Task=1, Job=2)",
  "timestamp": "2026-07-06T10:30:05.000Z"
}
```

| status | resultCode | 조건 |
|---|---|---|
| `ACCEPTED` | 0 | 정상 수락, 이동/동작 시작 |
| `EXECUTING` / `COMPLETED` | 0 | 실행 중 / 완료 |
| `REJECTED` | 2 | 지원하지 않는 command |
| `REJECTED` | 10 | AMR 미연결 (Modbus TCP 끊김) |
| `REJECTED` | 11 | 작업 중 (WorkStatus ≠ Idle) |
| `REJECTED` | 20 | NodeId 매핑 없음 |
| `FAILED` | 99 | 내부 오류 |

**주기 상태 (`amr/{ClientId}/status`, 1000ms, Retain)** — `state`/`pose`/`error`/`battery` 와, 비정상 시 `abnormal` 블록.

```json
{
  "state": { "runState": "Run", "fullState": "Full", "workState": "Moving", "vehicleDestNode": "N001" },
  "pose":  { "x": 12.5, "y": 34.2, "angle": 1.57 },
  "error": { "code": 0, "message": "" },
  "battery": { "levelPercent": 87.3, "voltage": 27.3, "chargingState": "Discharging" },
  "abnormal": { "type": "OPERATOR_ABORT", "node": "N02", "timestamp": "2026-07-06T10:35:00Z" }
}
```

### 5.2 EI → TS : RAIL-* (JSON)

EI 는 status/reply 를 도메인 메시지로 변환하여 TS 에 전달한다(`ESListener` 가 `header.messageName` 으로 워크플로우 라우팅). DB 변경은 TS 가 수행한다.

**(a) `RAIL-CARRIERTRANSFERREPLY`** — 반송 명령 처리 결과 회신. 모델: `RailCarrierTransferReplyMessage`.

```json
{ "header": { "messageName": "RAIL-CARRIERTRANSFERREPLY", "sender": "EI", ... },
  "data": { "commandId": "EX20260706103000123", "resultCode": "OK" } }   // OK / FAIL
```

**(b) `RAIL-VEHICLEUPDATE`** — AMR 상태·위치 통합 갱신. 노드 도착 감지(`nodeChanged=true`)가 EXCHANGE 의 "설비 도착"·"반납지 도착" 판정 근거가 된다.

```json
{ "header": { "messageName": "RAIL-VEHICLEUPDATE", "sender": "EI", ... },
  "data": {
    "vehicleId": "AMR001", "commId": "amr001",
    "runState": "RUN", "fullState": "FULL", "alarmState": "NOALARM",
    "batteryRate": 85, "batteryVoltage": 27.3,
    "vehicleDestNodeId": "N0002", "currentNodeId": "N0001",
    "nodeChanged": true, "connectionState": "CONNECT", "eventTime": "<UTC>"
  } }
```

노드 변경 감지: `NearestNodeFinder` 가 Pose(X,Y) 최근접 노드를 유클리드 거리로 판별(임계 2.0m, 설정 `Acs:Amr:NearestNodeThresholdMeters`). TS 의 `RailVehicleUpdateActivity` 가 값이 바뀐 항목만 순차 갱신.

**(c) `RAIL-VEHICLEABNORMAL`** — abnormal 블록 발생 시. type/code 로 TS 가 분기(예: `OPERATOR_ABORT`/`200` → TC 삭제 + Vehicle 초기화 + JOBREPORT(COMPLETE, ErrorCode=200) → MES). §8 참조.

```json
{ "header": { "messageName": "RAIL-VEHICLEABNORMAL", "sender": "EI", ... },
  "data": { "vehicleId": "AMR001", "commId": "amr001", "type": "OPERATOR_ABORT",
            "code": "200", "node": "N02", "abnormalTime": "<AMR>", "eventTime": "<UTC>" } }
```

### 5.3 TS → HS → MES : `JOBREPORT` (XML)

TS 가 단계 판정(할당/도착/취출완료/투입완료/반납완료/전체완료)에 도달하면 `IMessageManagerEx.SendJobReportToHost(...)` 로 HS 에 알리고, HS 의 `HostJobReportWorkflow → ForwardJobReportToMesActivity` 가 이를 XML JOBREPORT 로 조립하여 MES 로 송신한다. EXCHANGE 의 단계별 JOBREPORT 매핑은 §6.

---

## 6. EXCHANGE 단계 시퀀스 ↔ JOBREPORT 매핑

Excel `Scenario` 시트 기준. 하나의 EXCHANGECMD 가 아래 6개 보고를 순차 생성한다.

| Step | JOBREPORT `Type` | `StepName` | `ActionType` | `CarrierSlot` | 트리거 / 의미 |
|---|---|---|---|---|---|
| **10** | `RECEIVE` | `PICKUP_NEW` | `EXCHANGE` | - | HS 가 EXCHANGE Job 접수 (신규 매거진 픽업 시작 전) |
| **20** | `ARRIVED` | `MOVE_TO_EQUIP` | `EXCHANGE` | - | AMR 이 설비 도착 → MES 는 설비에 EXCHANGE_READY / UNLOAD_READY 전송 |
| **30** | `STEP_COMPLETE` | `UNLOAD_OLD` | `UNLOAD` | `3` | 설비 `FINAL_UNLOAD_REQUEST` 후 기존 매거진 취출 완료 |
| **40** | `STEP_COMPLETE` | `LOAD_NEW` | `LOAD` | `1` | 설비 `UPLOAD_REQUEST` 후 신규 매거진 투입 완료 |
| **50** | `STEP_COMPLETE` | `RETURN_OLD` | `MOVE` | `3` | 기존 매거진을 `UnloadDestLoc` 에 반납 완료 |
| **60** | `COMPLETE` | `DONE` | `EXCHANGE` | - | 교환 작업 전체 완료 |

전체 시나리오(설비 요청 포함):

| # | 방향 | Command/Report | ActionType | 조건 | 설명 |
|---|---|---|---|---|---|
| 1 | EQUIP→MES | `EXCHANGE_REQUEST` | EXCHANGE | EquipID+Port+Model | 설비가 교환 작업을 단일 요청으로 시작 |
| 2 | MES→ACS | `EXCHANGECMD` | EXCHANGE | JobID 전달 | 픽업·이동·회수·투입·반납을 하나의 ACS Job 으로 요청 |
| 3 | ACS→MES | `JOBREPORT/RECEIVE` | EXCHANGE | Step=10 | ACS 접수 |
| 4 | ACS→MES | `JOBREPORT/ARRIVED` | EXCHANGE | Step=20 | AMR 설비 도착 → MES 가 설비에 READY 전송 |
| 5 | EQUIP→MES | `FINAL_UNLOAD_REQUEST` | UNLOAD | 동일 JobID | 설비가 기존 매거진 회수 가능 상태 통지 |
| 6 | ACS→MES | `JOBREPORT/STEP_COMPLETE` | UNLOAD | Step=30, Slot=3 | 기존 매거진 회수 완료 |
| 7 | EQUIP→MES | `UPLOAD_REQUEST` | LOAD | 동일 JobID | 설비가 신규 매거진 투입 가능 상태 통지 |
| 8 | ACS→MES | `JOBREPORT/STEP_COMPLETE` | LOAD | Step=40, Slot=1 | 신규 매거진 투입 완료 |
| 9 | ACS→MES | `JOBREPORT/STEP_COMPLETE` | MOVE | Step=50, Slot=3 | 기존 매거진 반납 완료 |
| 10 | ACS→MES | `JOBREPORT/COMPLETE` | EXCHANGE | Step=60 | 전체 완료 |

> **✓ 확정 (2026-07-22):** 5·7번 설비 요청(`FINAL_UNLOAD_REQUEST`/`UPLOAD_REQUEST`)은 **기존 `ACTIONCMD`**(MES→ACS, `Type=UNLOAD/LOAD`, `JobId`) 재사용으로 중계한다. ACS 는 STEP 상태(20→UNLOAD 허용, 30→LOAD 허용) 게이팅 후 EI 경유 actionCmd 를 발행한다. — 기존 TBD 해소.

---

### 6.1 실행 시나리오 (단일 EXCHANGE, 1-TC 기준)

하나의 EXCHANGE(= 1 TC, Origin→Mid→Dest)가 하나의 AMR에서 **설비에서 교차 실행**되는 순서. 핵심은 (a) ARRIVED 후 첫 ActionCmd는 신규 투입이 아니라 **OLD 취출**이고, (b) 마지막 반납은 STEP_COMPLETE(50) → COMPLETE(60) **두 보고**로 닫힌다는 점. v2 배칭 시 이 6단계가 두 EXCHANGE(JobID)별로 인터리브된다.

| # | 보고 (ACS→MES) | 물리 동작 | waypoint | slot |
|---|---|---|---|---|
| — | RECEIVE **10** | HS 접수 (1행 insert, `EXCHANGE_QUEUED`) | — | — |
| — | START | 차량 할당·출발 | — | — |
| 1 | | `source`(=originLoc, 버퍼) 이동 → **신규 픽업** | Origin | 투입(1) |
| 2 | | `midLoc`(설비) 이동 → 도착 | →Mid | |
| — | ARRIVED **20** | 설비 도착, ActionCmd 대기(≤120s) | Mid | — |
| 3 | *(설비 FINAL_UNLOAD_REQUEST)* → ACTIONCMD(UNLOAD) | **OLD 취출** (설비→AMR) | Mid | 회수(3) |
| — | STEP_COMPLETE **30** | 기존 취출 완료 | Mid | 회수(3) |
| 4 | *(설비 UPLOAD_REQUEST)* → ACTIONCMD(LOAD) | **신규 투입** (AMR→설비) | Mid | 투입(1) |
| — | STEP_COMPLETE **40** | 신규 투입 완료 | Mid | 투입(1) |
| 5 | ACS → CARRIERTRANSFER(moveCmd, UNLOAD) | `dest`(버퍼) 이동 → **OLD 반납** | Mid→Dest | 회수(3) |
| — | STEP_COMPLETE **50** | 기존 반납 완료 | Dest | 회수(3) |
| — | COMPLETE **60** | EXCHANGE 종료, 슬롯 반환 | — | — |

보충: 두 번의 ACTIONCMD(취출·투입)는 ARRIVED 직후 자동이 아니라 **설비 핸드셰이크**가 방아쇠다. ARRIVED → MES가 설비에 READY 통지 → 설비가 `FINAL_UNLOAD_REQUEST`/`UPLOAD_REQUEST` → MES가 ACTIONCMD를 ACS로 내림. AMR의 EQP 120초 ActionCmd 대기가 이 구간을 흡수한다. 신규 픽업(Origin)·설비 이동(Mid)은 ACS가 moveCmd 2개로 각각 지시한다(AMR 자율 연속 아님).

---

## 7. 메시지 상세 사양

### 7.1 `JOBREPORT` (ACS → MES, XML)

EXCHANGE 응답·단계 공통 메시지.

```xml
<Msg>
  <Command>JOBREPORT</Command>
  <Header>
    <DestSubject>/HQ/MES01</DestSubject>
    <ReplySubject>/HQ/ACS01</ReplySubject>
  </Header>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <Type>STEP_COMPLETE</Type>            <!-- RECEIVE/START/ARRIVED/STEP_COMPLETE/COMPLETE/CANCEL -->
    <Step>30</Step>                       <!-- 10/20/30/40/50/60 -->
    <StepName>UNLOAD_OLD</StepName>       <!-- PICKUP_NEW/MOVE_TO_EQUIP/UNLOAD_OLD/LOAD_NEW/RETURN_OLD/DONE -->
    <JobID>EX20260706103000123</JobID>    <!-- EXCHANGECMD JobID 와 동일 -->
    <ActionType>UNLOAD</ActionType>       <!-- EXCHANGE/LOAD/UNLOAD/MOVE -->
    <CarrierSlot>3</CarrierSlot>          <!-- UNLOAD=3|4, LOAD=1|2 (Optional) -->
    <MaterialType>MAGAZINE</MaterialType>
    <UserID>ACS01</UserID>
    <ErrorCode>0</ErrorCode>              <!-- 0 또는 공백 = 정상 -->
    <ErrorMsg>ACK</ErrorMsg>
  </DataLayer>
</Msg>
```

| 필드 | 타입 | 필수 | 값 |
|---|---|---|---|
| `AcsId` | String | O | ACS 프로세스명 |
| `Type` | String | O | `RECEIVE` / `START` / `ARRIVED` / `STEP_COMPLETE` / `COMPLETE` / `CANCEL` |
| `Step` | String/Number | O | `10` / `20` / `30` / `40` / `50` / `60` |
| `StepName` | String | - | `PICKUP_NEW` / `MOVE_TO_EQUIP` / `UNLOAD_OLD` / `LOAD_NEW` / `RETURN_OLD` / `DONE` |
| `JobID` | String | O | EXCHANGECMD JobID 와 동일 |
| `ActionType` | String | O | `EXCHANGE` / `LOAD` / `UNLOAD` / `MOVE` |
| `CarrierSlot` | String | - | `1`~`4` (투입 1·2 / 회수 3·4) |
| `MaterialType` | String | O | `MAGAZINE` |
| `UserID` | String | - | MES 프로세스명 |
| `ErrorCode` | String | O | `0` 또는 공백 = 정상 |
| `ErrorMsg` | String | - | 오류 메시지 (정상 시 `ACK`) |

### 7.2 단계별 JOBREPORT 예시

접수(Step 10), 도착(Step 20), 완료(Step 60)는 `ActionType=EXCHANGE`, `CarrierSlot` 생략. 단계완료(30/40/50)는 각 `ActionType`(UNLOAD/LOAD/MOVE)과 `CarrierSlot`(회수/투입/회수 슬롯 번호, 예: 3/1/3)을 채운다. 필드값은 §6 매핑표를 그대로 따른다.

### 7.3 `RAIL-CARRIERTRANSFER` / `RAIL-CARRIERTRANSFERREPLY`

§4.4 / §5.2 참조. `jobType=EXCHANGE`, `model` 채워 전송. 회신은 `resultCode` = `OK`/`FAIL`.

### 7.4 신규 개발 포인트 — TS→EI actionCmd 채널

현재 코드에는 EXCHANGE 를 트리거하는 MES→ACS 파서(`EXCHANGECMD`)와, 설비 준비 신호에 따른 **actionCmd 게이팅**이 아직 없다(2026-07 기준 소스 grep 결과 `EXCHANGECMD` 미구현). 구현 시 필요한 신규 요소:

1. **HS**: `EXCHANGECMD` XML 파서 + `JobType=EXCHANGE` TransportCommand 생성 (기존 `CreateTransportCommandActivity` 확장).
2. **HS→MES**: JOBREPORT 에 `Step`/`StepName`/`CarrierSlot` 필드 추가 (기존 JOBREPORT 는 `Type`/`AmrId` 만 보유).
3. **TS**: EXCHANGE Job 의 단계 상태 머신(픽업→도착→취출→투입→반납→완료)과 각 전이에서 `SendJobReportToHost` 호출.
4. **TS→EI actionCmd**: EQP 도착 후 설비 준비 신호(ACTIONCMD 중계)에 맞춰 `actionCmd(UNLOAD/LOAD)` 를 발행하는 경로 (`RAIL-ACTIONCMD` JSON 또는 기존 CarrierTransfer 확장).
5. **AMR**: 이미 `jobType=EXCHANGE` 와 슬롯 1~4, EQP 120초 ActionCmd 대기, Cobot DI 매핑을 지원 — 신규 개발 최소.

---

## 8. 에러 코드 및 예외 처리

### 8.1 EXCHANGECMD / MOVECMD 검증 에러 (HS → MES, JOBREPORT)

| 상황 | ErrorCode | ErrorMsg |
|---|---|---|
| LOAD/EXCHANGE 인데 대상 Station 타입 불일치 | `21` | `DESTMACHINENOTFOUND` |
| DestLoc Location 미조회 | `21` | `DESTMACHINENOTFOUND` |
| SourceLoc Location 미조회 / 비어있음 | `25` | `SOURCEMACHINENOTFOUND` |
| Source == Dest (대소문자 무시) | `106` | `SOURCEDESTMACHINEDUPLICATE` |
| Source·Dest 공통 Bay 없음 | `22` | `NOTSAMEBAY` |
| 동일 JobID 중복 송신 | `102` | `COMMANDALREADYREQUESTED` |
| 정상 | `0` | (공백) |

에러 코드 상수: `src/ACS/ACS.Core/Base/AbstractManager.cs` 의 `ID_RESULT_*` 튜플.

### 8.2 운영자 강제 중단 (OPERATOR_ABORT)

EXCHANGE 진행 중 운영자가 AMR 패널에서 작업을 강제 중단하면, AMR 이 `abnormal.type=OPERATOR_ABORT`(code 200)를 status 로 반복 송신한다. 처리 흐름(기존 `RailVehicleAbnormalWorkflow`):

1. EI 가 `RAIL-VEHICLEABNORMAL` 로 TS 전달.
2. TS 멱등성 가드 — `vehicle.TransportCommandId` 공백이면 silent skip.
3. **JOBREPORT(COMPLETE, ErrorCode=200, ErrorMsg=OPERATOR_ABORT) → HS → MES** — 정상 종료와 abort-driven 종료를 `<ErrorCode>200</ErrorCode>` 로 구분. (반드시 TC 삭제보다 먼저)
4. TC 히스토리 이관 후 삭제(`NA_T_TRANSPORTCMD` → `NA_H_TRANSPORTCMDHISTORY`).
5. Vehicle 초기화 — `TransportCommandId=""`, `Path=""`, `AcsDestNodeId=""`, `TransferState=NOTASSIGNED`, `ProcessingState=IDLE`.

### 8.3 통신 단절 / Stuck 복구 (SCHEDULE-CHECKVEHICLES)

Daemon 이 10초 주기로 점검한다.

- **통신 단절**: Vehicle `EventTime` 이 60초 이상 미갱신이면 `ConnectionState=DISCONNECT` (단, `ProcessingState ∈ {PARK, CHARGE}` 제외). 시간 비교는 반드시 `DateTime.UtcNow`.
- **Stuck 복구**: `ProcessingState=RUN` + `RunState=STOP` + `NOALARM` + `TransportCommandId` 유효한 차량을 찾아, TransferState/tc.State 매칭에 따라 `RAIL-CARRIERTRANSFER` 를 EI 로 재푸시(응답 대기·재시도 없는 단발). TC.VehicleId 불일치는 Vehicle 을 진실 원천으로 self-heal.

---

## 9. 부록 — 상관키 및 슬롯 규약

- **상관키**: `EXCHANGECMD.JobID` = HS `TransportCommand.JobId` = TS `RAIL-CARRIERTRANSFER.commandId` = 모든 `JOBREPORT.JobID`. 전 구간 동일 유지 필수.
- **슬롯 규약**: 투입슬롯 `1`·`2` = 신규 매거진, 회수슬롯 `3`·`4` = 기존 매거진. JOBREPORT `CarrierSlot` 은 실제 사용 슬롯 번호(UNLOAD=3|4, LOAD=1|2).
- **포트 규약**: 설비(EQP)는 `LEFT`/`RIGHT` 필수. 버퍼는 단일 포트로 처리되어 Port 값이 결과에 영향을 주지 않음(빈 문자열 허용).
- **portType 분기(AMR)**: `EQP` = 도착 후 ActionCmd 최대 120초 대기 / `BUFFER`·`INPUT`·`OUTPUT`·`VBUFFER` = 즉시 진행 / `CHARGE` = 충전.

---

## 10. 데이터 모델 & 배차(DS) 설계 (v2)

### 10.1 원칙 — 1 EXCHANGE = 1 TransportCommand (Origin→Mid→Dest)

EXCHANGE 는 신규/기존 두 매거진이 관여하지만 **하나의 TransportCommand** 로 표현한다. `NA_T_TRANSPORTCMD` 의 미사용 경유지 필드를 활용해 3-waypoint 여정을 한 행에 담는다. (이전 2-TC 그룹·원자 할당·활성 leg 스왑 방식은 폐기 — 차량↔TC 1:1 이 유지되어 기존 단일-TC 할당·롤백을 재사용.)

| 여정 | 위치 | 컬럼 | 동작 |
|---|---|---|---|
| ① 신규 픽업 | LoadSourceLoc(버퍼) | `source` = `originLoc` | NEW 픽업 → 투입슬롯 |
| ② 설비(교환) | EquipID:Port | `midLoc` / `midPortId` | OLD 취출(회수슬롯) + NEW 투입(투입슬롯) |
| ③ 기존 반납 | UnloadDestLoc(버퍼) | `dest` | OLD 하치 → 회수슬롯 |

### 10.2 컬럼 매핑 (insert 스냅샷)

`eqpId` 는 코드 관례대로 **AcsId**, `portId` 는 미세팅. 슬롯은 4개 구성(1·2 투입 / 3·4 회수) 중 실제 사용 번호를 `additionalInfo` 에 기록.

| 컬럼 | 값(예) | 근거 |
|---|---|---|
| `priority` | `3` | DEFAULT_PRIORITY |
| `state` | `EXCHANGE_QUEUED` | **값**만 신규(기존 스케줄러 배제용). 컬럼 변경 없음 |
| `vehicleId` | `NULL` | 배차 시 채움 |
| `source` / `originLoc` | `IN_BUF_01:LEFT` | ① 신규 픽업 |
| `midLoc` / `midPortId` | `192.168.32.36` / `RIGHT` | ② 설비(교환) |
| `dest` | `OUT_BUF_01:LEFT` | ③ 기존 반납 |
| `additionalInfo` | `TRIP=<tripId>;LOADSLOT=1;UNLOADSLOT=3;EQJOB_L=..._LOAD_...;EQJOB_U=..._UNLOAD_...` | 트립 묶기 + 슬롯 번호 + 설비보고 JobID |
| `eqpId` / `portId` | `ACS01` / `NULL` | AcsId 관례 |
| `jobType` | `EXCHANGE` | 스케줄러 분기 |
| `description` | `MODEL='CF203W';MAGAZINE` | GetModel() 파싱 |
| `bayId` | `BAY01` | 설비 Station 의 Bay |
| `jobId` | `EX20260706103000123` | 원본 JobID = JOBREPORT 상관키 |

> 기존 테이블 스키마 무변경. `state` 의 새 값(`EXCHANGE_QUEUED`, 14자)은 varchar(20) 안에 들어간다.

### 10.3 슬롯 점유 모델 — 신규 테이블 `NA_R_VEHICLE_SLOT`

배칭(트립당 최대 2 EXCHANGE = 4슬롯)을 위해 슬롯별 점유를 추적한다. `FullState`(FULL/EMPTY 이진)로는 "4칸 중 일부 점유"를 표현 못 하므로 **추가(additive) 테이블 1개**를 도입한다(기존 테이블 무수정).

| 컬럼 | 예 | 의미 |
|---|---|---|
| `vehicleId` | AMR001 | 차량 |
| `slotNo` | 1~4 | 물리 슬롯 |
| `role` | INSERT / RETRIEVE | 1·2=INSERT, 3·4=RETRIEVE |
| `state` | EMPTY / OCCUPIED | 점유 여부 |
| `jobId` | EX...X | 점유한 EXCHANGE |
| `phase` | NEW / OLD | 신규 적재 / 기존 회수 |
| `updatedTime` | UTC | 갱신 시각 |

영속화하므로 TS 재기동 시 트립 중 슬롯 상태를 재구성할 수 있다.

### 10.4 배차(DS) v2 — 같은 Bay, 대기창 없는 기회주의 배칭

`EXCHANGE_QUEUED` 는 기존 `GetQueuedTransportCommands*`(`State="QUEUED"`)에 안 걸리므로 **기존 스케줄러는 손대지 않는다**. EXCHANGE 전용 디스패처가 매 틱에 아래를 수행한다.

```
매 스케줄 틱(bay별):
  1. EXCHANGE_QUEUED TC 조회 (bay, priority·createTime 순)
  2. EMPTY 4슬롯 AMR (IDLE+CONNECT, 4칸 전부 free) 탐색
  3. 첫 EXCHANGE(A) 선택.
     같은 Bay 에 EXCHANGE_QUEUED 2번째(B)가 있으면 → A+B 배칭
                                        없으면       → A 단독(2슬롯만 사용)
     ※ 대기창 없음: B 가 지금 없으면 A 를 붙잡지 않고 즉시 단독 출발
  4. 배정:
     · tripId 발급, 각 TC additionalInfo 에 TRIP=tripId
     · 슬롯 할당 — A: 슬롯1(NEW)+3(OLD), B: 슬롯2(NEW)+4(OLD)
     · NA_R_VEHICLE_SLOT 기록, vehicle→RUN, state→ASSIGNED
     · vehicle.transportCommandId = tripId (코디네이터가 TRIP 로 두 TC 해석)
  5. EXCHANGE별로 JOBREPORT(RECEIVE/START) + 첫 moveCmd
```

배칭 여부를 매 틱에 즉시 평가하고 대기 상태를 들고 있지 않아 단순하다. 같은 Bay 2건이 안 잡히면 자연히 1건으로 진행한다.

### 10.5 코디네이터 — 다구간 투어

한 트립이 최대 2 EXCHANGE 를 담으므로 코디네이터는 다구간 투어를 몬다: 신규 픽업(들) → 설비X(취출·투입) → 설비Y(취출·투입) → 기존 반납(들). 설비별로 actionCmd 게이팅(**트립당 최대 4회**), JOBREPORT 는 두 JobID 가 각자 10~60 을 인터리브. `AcsDestNodeId` 를 경유지 순으로 전진시켜 도착 감지.

### 10.6 DB 변경 요약

| 대상 | 변경 |
|---|---|
| `NA_T_TRANSPORTCMD` | **무변경** (기존 컬럼 재활용, `state` 값만 추가) |
| `NA_R_VEHICLE` | **무변경** (슬롯은 별도 테이블) |
| `NA_R_VEHICLE_SLOT` | **신규 테이블 1개** (additive) |
| `NA_R_LOCATION`/`NA_R_STATION` | 설비·버퍼 미등록 시 행 INSERT (데이터, DDL 아님) |

---

## 참고 소스 / 사양

| 항목 | 위치 |
|---|---|
| EXCHANGE 메시지·시나리오 | `MESACS_매거진 교체 시나리오 사양서.xlsx` (MESSAGE / Scenario / EXCHANGE * 시트) |
| MES-ACS 기본 메시지 | `MESACS Message 사양서_20260325.xlsx` |
| MOVECMD 송신 규약 | `movecmd_송신_규약_이노로보틱스_NAMUGA_ACS.docx` |
| TS→EI 반송 모델 | `src/ACS/ACS.Communication/Mqtt/Model/RailCarrierTransferMessage.cs`, `RailCarrierTransferReplyMessage.cs` |
| EI↔AMR MQTT | `mqtt_interface.md`, `ACSAMR_mqtt_movecmd.md` |
| EI→TS 상태 | `trans_message.md` (RAIL-VEHICLEUPDATE), `vehicleabnormal.md` (RAIL-VEHICLEABNORMAL) |
| HS TransportCommand 생성 | `movecmd_source_empty.md`, `src/ACS/ACS.Elsa/Activities/HostActivities.cs` |
| Stuck/단절 복구 | `schedule_check_vehicle.md`, `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs` |
