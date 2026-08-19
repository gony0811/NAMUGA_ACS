# ACS-AMR MQTT EXCHANGE 인터페이스 정의서

| 항목 | 내용 |
|---|---|
| 문서 상태 | **v0.3 (2026-08-19)** — ACS 구현(§46/§48/§49, E2E 완주 8/15·8/18)에 정합. AMR 측 반영 협의용 |
| 개정 이력 | v0.2 (2026-08-11): exchangeCmd 단일 명령 모델 초안 (폐기) → **v0.3: ACS 구간별 moveCmd/actionCmd 오케스트레이션 모델 + reply 확장** |
| 상위 사양 | 나무가 ACS_MES 매거진 교체 시나리오 사양서 v2 (2026-07-29) |
| 관련 문서 | `mqtt_interface.md`(토픽/status/command/reply 공통), `ACS-AMR_mqtt_movecmd.md`(moveCmd·Cobot DI), `vehicle_alarm.md`, `vehicle-abnormal.md` |
| 구현 참조 | `ACS.Core/Transfer/AmrReplyPolicy.cs`, `ExchangeSteps.cs`, `ExchangeInfo.cs` / `ACS.Elsa/Activities/ExchangeTransHandlers.cs`, `MqttActivities.cs(HandleAmrReplyActivity)` / `ACS.Communication/Mqtt/Model/AmrCommandMessage.cs`, `AmrReplyMessage.cs` |

MES↔ACS EXCHANGECMD(매거진 교환) 시나리오를 수행하기 위한 ACS↔AMR MQTT 인터페이스를 정의한다.
**v0.3 결정: AMR 에 별도 `exchangeCmd` 를 두지 않는다.** ACS 가 교환 여정을 구간(Origin→설비→반납)으로 나누어 기존 `moveCmd` / `actionCmd` 로 지시하고, 단계(STEP 10~60)·MES 보고는 ACS 가 추적·생성한다. AMR 은 구간 단위 실행 + reply 만 담당한다. (근거: 2건 배칭 트립 확장성, 검증된 E2E, AMR 개발 최소화 — `docs/memory.md` 참조)

---

## 1. 개요

- **토픽 (기존 재사용)**: ACS→AMR `amr/{ClientId}/command` (moveCmd / actionCmd / cancelCmd) · AMR→ACS `amr/{ClientId}/reply` (status 확장)
- **역할 분담**: MES↔ACS 메시지(EXCHANGECMD/JOBREPORT/ACTIONCMD/JOBCANCEL)는 ACS 처리. AMR 은 ACS 가 준 노드/포트/슬롯 기준으로 물리 동작 + reply.
- **Job 상관키**: `cmdId` = `jobId` = MES EXCHANGECMD.JobID = ACS TC JobId. 한 교환 job 의 모든 moveCmd/actionCmd 는 **같은 cmdId** 를 쓴다. AMR 은 reply 에 받은 cmdId 를 그대로 되돌린다.
- **STEP 추적 주체는 ACS**: AMR reply 의 `step/carrierSlot` 은 선택(있으면 대조·로그, 없어도 동작).

## 2. 단계(Step) 매핑 — MES 사양 ↔ ACS 명령 ↔ AMR reply

| Step | StepName | ACS→AMR 명령 | AMR 동작 | AMR→ACS reply | ACS→MES 보고 |
|---|---|---|---|---|---|
| 10 | PICKUP_NEW | `moveCmd` jobType=UNLOAD, nodeId=Origin, portType=자재, amrSlot=**loadSlot(1\|2)** | 픽업지 이동 → QR 보정 → NEW PICK → AMR 슬롯 PLACE | ACCEPTED → EXECUTING → (ARRIVED) → **COMPLETED** | RECEIVE / START (ACS 시점) |
| 20 | MOVE_TO_EQUIP | `moveCmd` jobType=**EXCHANGE**, nodeId=설비, port, portType=**EQP**, amrSlot=loadSlot | 설비 노드 이동 → 도킹 → **actionCmd 대기** | ACCEPTED → EXECUTING → **ARRIVED**(권장) → COMPLETED(도킹 완료, ACS 무시) | ARRIVED(20) |
| — | (게이트1) | `actionCmd` **type=UNLOAD**, jobType=EXCHANGE, port, amrSlot=**unloadSlot(3\|4)** | QR 보정 → OLD PICK(설비) → 회수슬롯 PLACE | ACCEPTED → **COMPLETED** (또는 STEP_COMPLETE step=30 carrierSlot=3\|4) | STEP_COMPLETE(30, CarrierSlot=회수슬롯) |
| — | (게이트2) | `actionCmd` **type=LOAD**, jobType=EXCHANGE, port, amrSlot=**loadSlot(1\|2)** | NEW PICK(투입슬롯) → 설비 PLACE | ACCEPTED → **COMPLETED** (또는 STEP_COMPLETE step=40 carrierSlot=1\|2) | STEP_COMPLETE(40, CarrierSlot=투입슬롯) |
| 50 | RETURN_OLD | `moveCmd` jobType=LOAD, nodeId=Dest, portType=자재, amrSlot=**unloadSlot** | 반납지 이동 → QR 보정 → OLD PICK(회수슬롯) → 반납지 PLACE | ACCEPTED → EXECUTING → (ARRIVED) → **COMPLETED** | STEP_COMPLETE(50) |
| 60 | DONE | (없음 — ACS 종결) | 홈 복귀·대기 | — | COMPLETE(60) |

주의 (사양서 원문): UNLOAD_OLD·LOAD_NEW 는 설비 기구 상태 때문에 반드시 설비의 후속 요청(FINAL_UNLOAD_REQUEST / UPLOAD_REQUEST)이 MES→ACS ACTIONCMD 로 중계된 뒤에만 실행한다. **AMR 은 portType=EQP 도착 후 actionCmd 를 수신할 때까지 다음 단계로 진행하지 않는다.** ACS 는 ACTIONCMD(UNLOAD)를 STEP=20 에서만, ACTIONCMD(LOAD)를 STEP=30 에서만 수용해 AMR 에 중계한다(이중 방어).

## 3. 처리 흐름

```
MES                ACS(TS/EI)                          AMR
 | EXCHANGECMD      |                                   |
 |----------------->| RECEIVE(10) 회신, 배차·슬롯 배정   |
 |<- START(10) -----| moveCmd(UNLOAD, Origin, slot=1)   |
 |                  |---------------------------------->| ACCEPTED/EXECUTING → 픽업 → COMPLETED
 |                  |<--- COMPLETED --------------------|
 |                  | STEP 10→20, moveCmd(EXCHANGE, EQP)|
 |                  |---------------------------------->| 이동 → ARRIVED → 도킹 → COMPLETED(무시)
 |<- ARRIVED(20) ---|<--- ARRIVED ----------------------|   (pose 도착 판정과 OR, 1회만 보고)
 | FINAL_UNLOAD_REQ |                                   | (게이트1 대기)
 | ACTIONCMD UNLOAD |                                   |
 |----------------->| 게이트 STEP=20 ✓ → actionCmd(UNLOAD, slot=3)
 |                  |---------------------------------->| OLD 취출 → 회수슬롯
 |<- STEP_COMPLETE 30<-- COMPLETED (carrierSlot=3) -----|
 | UPLOAD_REQUEST   |                                   | (게이트2 대기)
 | ACTIONCMD LOAD   |                                   |
 |----------------->| 게이트 STEP=30 ✓ → actionCmd(LOAD, slot=1)
 |                  |---------------------------------->| NEW 투입 ← 투입슬롯
 |<- STEP_COMPLETE 40<-- COMPLETED (carrierSlot=1) -----|
 |                  | STEP→50, moveCmd(LOAD, Dest, slot=3)
 |                  |---------------------------------->| 반납지 이동·하역
 |<- STEP_COMPLETE 50<-- COMPLETED ---------------------|
 |<- COMPLETE(60) --| TC 종결, 슬롯 해제, 차량 IDLE      |
```

## 4. Command (ACS → AMR) — 토픽 `amr/{ClientId}/command`

공통 필드: `cmdId`(필수, = jobId), `command`(필수). 아래 선택 필드는 **값이 없으면 JSON 에서 생략**된다.
`jobType` 은 **TC 의 작업 유형**(일반 반송 LOAD/UNLOAD, 교환 EXCHANGE)이고, actionCmd 의 `type` 은 **이번 액션**(UNLOAD=취출/LOAD=투입)이다. AMR 은 PICK/PLACE 결정에 `type` 이 있으면 `type` 을, 없으면 `jobType` 을 쓴다.

### 4.1 moveCmd (기존 — `ACS-AMR_mqtt_movecmd.md` 참조)
```json
{"cmdId":"EX20260706103000123","command":"moveCmd","nodeId":"N2002","port":"RIGHT",
 "jobType":"EXCHANGE","portType":"EQP","model":"CF203W","amrSlot":1}
```
| 필드 | 필수 | 설명 |
|---|---|---|
| nodeId | O | 목적 노드 |
| port | - | LEFT / RIGHT (설비 포트) |
| jobType | - | LOAD / UNLOAD / **EXCHANGE**(설비행: 도착 후 actionCmd 대기) |
| portType | - | LocationEx.Type 그대로: EQP / BUFFER / INPUT / OUTPUT / CHARGE / VBUFFER |
| model | - | 매거진 모델 (Offset 보정) |
| amrSlot | - | 이 구간에서 조작할 AMR 슬롯 1~4 (기본 1). UNLOAD(픽업)=투입슬롯, LOAD(반납)=회수슬롯 |

### 4.2 actionCmd — 게이트 허가 (확장)
```json
{"cmdId":"EX20260706103000123","command":"actionCmd","jobId":"EX20260706103000123",
 "nodeId":"N2002","port":"RIGHT","jobType":"EXCHANGE","type":"UNLOAD","model":"CF203W","amrSlot":3}
```
| 필드 | 필수 | 설명 |
|---|---|---|
| jobId | O(교환) | 진행 중 job 과 대조. 불일치 시 무시(로그) |
| type | O(교환) | **UNLOAD**=기존 매거진 취출 허가(게이트1) / **LOAD**=신규 매거진 투입 허가(게이트2). Cobot PICK/PLACE 결정 기준 |
| jobType | O | TC 작업 유형: 교환이면 **EXCHANGE**, 일반 반송이면 LOAD/UNLOAD(=type). AMR 은 `type` 이 있으면 `type` 우선 |
| nodeId, port | O | 설비 노드/포트 |
| amrSlot | O(교환) | UNLOAD → 회수슬롯 3\|4 에 PLACE, LOAD → 투입슬롯 1\|2 에서 PICK |
| model | - | 매거진 모델 |

수용 조건: AMR 은 자신의 게이트 상태와 type 이 일치할 때만 수용한다(게이트1 대기 중 UNLOAD 만, 게이트2 대기 중 LOAD 만). 그 외는 무시+로그. ACS 도 STEP=20/30 게이트로 이중 방어한다.
게이트 대기 정책: **기본 무제한 대기 + 주기 경고(120초)**. 상한 설정 시 초과하면 FAILED(resultCode=32) 종결.

### 4.3 cancelCmd — 취소 (JOBCANCEL C2/C3)
```json
{"cmdId":"EX20260706103000123","command":"cancelCmd","jobId":"EX20260706103000123"}
```
| 필드 | 필수 | 설명 |
|---|---|---|
| jobId | O | 취소 대상 job. 진행 중 job 과 불일치/미진행이면 CANCELED(resultCode=40) |

AMR 처리: 진행 중 명령(moveCmd/actionCmd) 폐기 → **현 위치 정지 → Idle 복귀** → `CANCELED` reply. **복귀 이동은 AMR 이 하지 않는다** — C3(적재 후 취소)의 충전소 복귀는 ACS 가 별도 `moveCmd(portType=CHARGE)` 로 지시하고, 차량 ALARM 도 ACS 가 설정한다. (v0.2 의 `returnNode` 필드 삭제)

## 5. Reply (AMR → ACS) — 토픽 `amr/{ClientId}/reply`

기본 필드(기존): `cmdId, status, resultCode(int), message, timestamp`. **확장 필드(선택, 없으면 생략 가능)**:

| 필드 | 타입 | 설명 |
|---|---|---|
| jobId | string | command 의 jobId 그대로 (없으면 ACS 는 cmdId 사용) |
| jobType | string | command 의 jobType 그대로 (LOAD/UNLOAD/EXCHANGE). 있으면 ACS 구간 판정에 우선 사용 |
| step | int | 완료/도착 단계 10~60. **STEP_COMPLETE 에서는 필수** |
| stepName | string | PICKUP_NEW / MOVE_TO_EQUIP / UNLOAD_OLD / LOAD_NEW / RETURN_OLD / DONE |
| carrierSlot | int | 해당 단계에서 조작한 AMR 슬롯 1~4 (완료 계열 권장) |

### 5.1 status 목록과 ACS 처리

| status | 신규 | 의미 | ACS 처리 |
|---|---|---|---|
| ACCEPTED | | 명령 수락 | 무시(로그) |
| EXECUTING | | 실행 시작 | 무시(로그) |
| **ARRIVED** | ★ | 목적 노드 도착 (moveCmd) | `RAIL-VEHICLEARRIVED` → 도착 판정 진입점(RAIL-VEHICLEDESTARRIVED)으로 수렴. pose 판정과 OR, **같은 도착은 1회만 보고**(TC ARRIVED 마커) |
| COMPLETED | | 명령 완료 (구간별) | jobType(없으면 TC STEP/State 역추정)에 따라 ACQUIRE/DEPOSIT/EXCHANGE 완료 처리. EXCHANGE 설비 구간: ACT(진행 중 actionCmd) 없으면 도킹 완료로 간주 무시, ACT=UNLOAD→STEP 30, ACT=LOAD→STEP 40→50 |
| **STEP_COMPLETE** | ★ | COMPLETED 의 별칭 (step 필수) | COMPLETED 와 동일 경로. step 이 ACS 기대 단계(UNLOAD→30, LOAD→40)와 다르면 경고+무시 |
| REJECTED | | 수락 거부 | EXCHANGE TC: **STEP=10(첫 moveCmd)이면 배차 롤백(EXCHANGE_QUEUED, 슬롯 해제, 차량 IDLE) → 다음 틱 재배차**, 그 외 로그(정지+운영자). 일반 TC: 로그 |
| FAILED | | 실패 종결 | EXCHANGE TC: **STEP=10 이면 MAGAZINE_NOT_FOUND 즉시 종결**(JOBREPORT COMPLETE+ErrorCode, TC 삭제, 차량 IDLE), 그 외 로그(정지+운영자). 일반 TC: 로그 |
| **CANCELED** | ★ | cancelCmd 처리 결과 | 로그만 (ACS 는 reply 를 기다리지 않고 취소 처리를 이미 완료). resultCode=40 이면 경고 |

예시 — 설비 OLD 취출 완료:
```json
{"cmdId":"EX20260706103000123","jobId":"EX20260706103000123","status":"COMPLETED",
 "step":30,"stepName":"UNLOAD_OLD","carrierSlot":3,"resultCode":0,"message":"OLD magazine retrieved (slot 3)",
 "timestamp":"2026-08-19T10:31:20.000Z"}
```
예시 — 설비 도착:
```json
{"cmdId":"EX20260706103000123","jobId":"EX20260706103000123","status":"ARRIVED","step":20,
 "resultCode":0,"message":"","timestamp":"2026-08-19T10:30:05.000Z"}
```

### 5.2 moveCmd COMPLETED 와 actionCmd COMPLETED 의 구분
reply 에는 어떤 command 에 대한 응답인지 명시하는 필드가 없다. EXCHANGE 설비 구간에서 ACS 는 **ACT(진행 중 actionCmd) 마커**로 구분한다 — actionCmd 를 보내기 전에 도착한 COMPLETED(도킹 완료)는 무시. **일반 반송(LOAD/UNLOAD moveCmd)** 에서는 AMR 이 moveCmd 도착 시점에 COMPLETED 를 보내면 안 되고(도착은 ARRIVED 로), **작업(PICK/PLACE) 완료 시점에만 COMPLETED** 를 보내야 한다.

## 6. 오류 코드 (resultCode)

| resultCode | status | 구분 | 의미 | ACS 처리 |
|---|---|---|---|---|
| 0 | - | 기존 | 정상 | |
| 2 | REJECTED | 기존 | 지원하지 않는 command | 로그 |
| 10 | REJECTED | 기존 | AMR Modbus 미연결 | STEP=10 롤백·재배차 / 그 외 로그 |
| 11 | REJECTED | 기존 | 작업 중 (Idle 아님) | 〃 |
| 20 | REJECTED | 기존 | NodeId 위치 태그 매핑 없음 | 〃 |
| 21 | REJECTED | 신규 | 슬롯 상태 불일치 (amrSlot 점유/비어있음) | 〃 |
| 22 | REJECTED | 신규 | Cobot 준비 안 됨 | 〃 |
| 30 | FAILED | 신규 | MAGAZINE_NOT_FOUND — 픽업지 매거진 부재 | STEP=10 → COMPLETE+MAGAZINE_NOT_FOUND 종결 |
| 31 | FAILED | 신규 | 시퀀스 중 슬롯/센서 상태 불일치 | 로그 (정지+운영자) |
| 32 | FAILED | 신규 | actionCmd 게이트 대기 상한 초과 (상한 설정 시) | 로그 (정지+운영자) |
| 40 | CANCELED | 신규 | CANCEL_REJECTED — 취소 불가 (종료 상태/jobId 불일치) | 경고 로그 |
| 99 | FAILED | 기존 | 내부 예외 | 로그 |

※ ACS 의 FAILED/REJECTED 처리는 **status 와 STEP** 으로 결정하며 resultCode 는 로그·MES ErrorMsg 에만 반영한다(`AmrReplyPolicy.DecideFailed`).

### 6.1 차량 알람 (vehicle_alarm.md 에 추가)
| 코드 | 이름 | 조건 / 동작 |
|---|---|---|
| ERR-114 | Pickup Source Magazine Not Found | 픽업지 매거진 미검출 → FAILED(30), 경광등 Red+부저, Reset 해제 |
| ERR-115 | Exchange Slot State Mismatch | 시퀀스 중 지정 슬롯 상태 불일치 → FAILED(31) |
| ERR-116 | ActionCmd Wait Timeout | 게이트 대기 상한 초과(상한 설정 시) → FAILED(32) |
ERR-110~113 슬롯/포트 알람은 기존 구현 재사용. 알람은 status 토픽 `error.code` 로 보고되어 ACS 차량 ALARM 으로 반영된다.

### 6.2 status abnormal (vehicle-abnormal.md 에 추가)
| 코드 | 타입 | 조건 | 해제 |
|---|---|---|---|
| 300 | EXCHANGE_CANCEL_HOLD | 적재 후 취소(C3) 로 정지, 작업자 실물 회수 대기 (latched) | 작업자 매거진 회수 후 Reset. ACS 차량 reset 시 슬롯/ALARM 해소 |

## 7. 협의 항목 (Open Issues) — v0.3 갱신

| # | 항목 | 상태 |
|---|---|---|
| 1 | Loc→NodeId 변환 주체 | ✅ 확정 — ACS 가 변환해 nodeId 로 전달 |
| 2 | 게이트(actionCmd) 대기 정책 | 기본 무제한 + 120초 주기 경고, 상한 옵션(FAILED 32) — AMR 측 확인 필요 |
| 3 | C3 취소 복귀 노드 | ✅ v0.3 확정 — AMR 은 정지·Idle 만, 복귀는 ACS moveCmd(CHARGE) |
| 4 | START 보고 시점 | ✅ v0.3 확정 — ACS 배차(슬롯 배정) 시점. AMR EXECUTING 은 미사용 |
| 5 | 슬롯 배정 주체 | ✅ ACS 지정(loadSlot/unloadSlot → amrSlot), AMR 은 검증만 |
| 6 | 픽업지·반납지 QR/티칭 | 버퍼 위치 신규 스테이션이면 티칭/매핑 추가 필요 (AMR 사용자 매뉴얼 5장) |
| 7 | (삭제) exchangeCmd 중 moveCmd 우선순위 | v0.3 에서 exchangeCmd 폐기로 해당 없음. 단 **AMR 은 신규 명령 수신 시 기존 명령을 폐기**하므로 ACS 는 게이트 통과 후에만 다음 명령을 발행한다 |
| 8 | ARRIVED reply 발행 | 권장(선택). 미발행 시 ACS 는 pose 기반 도착 판정으로 동작 |
| 9 | reply jobId/step/carrierSlot | 선택. 있으면 ACS 가 대조·로그 |
| 10 | 일반 moveCmd 의 COMPLETED 시점 | **작업 완료 시점만** (도착은 ARRIVED) — AMR 측 확인 필요 (§5.2) |

## 8. ACS 구현 매핑

| 구성 요소 | 파일 | 내용 |
|---|---|---|
| 명령/응답 모델 | `ACS.Communication/Mqtt/Model/AmrCommandMessage.cs`, `AmrReplyMessage.cs` | jobId/type, jobId/step/stepName/carrierSlot |
| 송신 | `ACS.Communication/Mqtt/MqttInterfaceManager.cs` | `SendDestination`(moveCmd), `SendAction`(type/jobId/amrSlot), `SendCancel`(jobId) |
| reply 라우팅(EI) | `ACS.Elsa/Activities/MqttActivities.cs HandleAmrReplyActivity` | status 별 RAIL-* 라우팅 (`AmrReplyPolicy.Route`) |
| 결정표 | `ACS.Core/Transfer/AmrReplyPolicy.cs` | status/resultCode 상수, Route, DecideFailed, ResolveExchangeJobType |
| 단계/마커 | `ACS.Core/Transfer/ExchangeSteps.cs`, `ExchangeInfo.cs` | STEP, ACT, ARRIVED 마커, RecoverySegment |
| 도착 | `ACS.Elsa/Workflows/Trans/RailVehicleArrivedWorkflow.cs`, `RailVehicleDestArrivedWorkflow.cs`, `RailVehicleUpdateWorkflow.cs` | reply ARRIVED / pose 판정 수렴 + 중복 가드 |
| 설비 구간 | `ACS.Elsa/Activities/ExchangeTransHandlers.cs`, `TransActionCmdActivities.cs` | ACT 기반 STEP 전이, ACTIONCMD 게이트, 롤백 헬퍼 |
| 실패/거부 | `ACS.Elsa/Workflows/Trans/RailVehicleJobfailedWorkflow.cs` | MAGAZINE_NOT_FOUND / 롤백 / 로그 |
| stuck 복구 | `ACS.Elsa/Activities/ScheduleActivities.cs RecoverStuckVehiclesActivity` | EXCHANGE 구간 재푸시 |
| 시뮬레이터 | `ACS.AMR.Simulator/Mqtt/VirtualAmr.cs` | ARRIVED/CANCELED reply, jobId/carrierSlot |
| 테스트 | `ACS.Core.Tests/Exchange/AmrReplyPolicyTests.cs`, `Mqtt/AmrMessageContractTests.cs` | 결정표·페이로드 계약 |
