# Vehicle Abnormal 처리 (AMR → ES → TS)

## 1. Overview

AMR(Autonomous Mobile Robot) 이 운행 중 비정상 상황을 감지하면 MQTT `amr/{id}/status` 토픽의 `abnormal` 블록에 사건을 실어 보낸다. 대표적인 트리거는 **운영자가 AMR 조작 패널에서 강제로 작업을 중단(OPERATOR_ABORT)** 한 경우다.

이 흐름은 다음 3 홉을 거친다.

```
AMR ──(MQTT: amr/{id}/status .abnormal)──▶ ES (Trans-EI 프로세스)
                                            └ RAIL-VEHICLEABNORMAL JSON 생성
                                            └ tsAgent.Send() ───────────────▶ TS (Trans 프로세스)
                                                                              └ RailVehicleAbnormalWorkflow
                                                                                └ type 별 분기
                                                                                  └ OPERATOR_ABORT: TC 삭제 + Vehicle 초기화
```

ES 는 메시지 전달만 담당하고, **도메인 상태 변경(NA_T_TRANSPORTCMD 삭제, NA_R_VEHICLE 갱신) 은 모두 TS 책임**이다. 이는 기존 `RAIL-VEHICLEUPDATE` / `RAIL-VEHICLEALARM` 메시지와 동일한 분리 패턴이다.

## 2. Vehicle → ES (MQTT)

**토픽**: `amr/{vehicleId}/status`

**`abnormal` 블록 스키마** (모델: `AmrAbnormal`, `src/ACS/ACS.Communication/Mqtt/Model/AmrStatusMessage.cs`):

| 필드 | 타입 | 의미 |
|------|------|------|
| `type` | string | 비정상 유형. 이름(예: `OPERATOR_ABORT`) 또는 코드(예: `200`) 중 어느 쪽이든 가능 |
| `node` | string | AMR 이 보고한 발생 노드 ID |
| `timestamp` | DateTime | 발생 시각 |

**알려진 abnormal 코드 표**

| Code | Type 이름 | 의미 | TS 처리 |
|------|-----------|------|---------|
| 200 | OPERATOR_ABORT | 운영자가 AMR 조작 패널에서 작업을 강제 중단 | TC 삭제 + Vehicle 초기화 |

> 새 코드가 추가될 때 표에 행을 추가하고 §4 의 분기 case 도 함께 추가한다.

**예시 페이로드 (정상 토픽 + abnormal 블록)**:
```json
{
  "state": { "runState": "Stop", "fullState": "Full" },
  "pose":  { "x": 12.5, "y": 34.2, "angle": 1.57 },
  "abnormal": {
    "type": "OPERATOR_ABORT",
    "node": "N02",
    "timestamp": "2026-06-09T10:23:45Z"
  }
}
```

## 3. ES → TS (RabbitMQ)

ES (`ParseAmrStatusActivity` → `SendVehicleAbnormalActivity`, `src/ACS/ACS.Elsa/Activities/MqttActivities.cs`) 는 abnormal 블록이 존재하면 **type 무관하게** `RAIL-VEHICLEABNORMAL` 메시지를 만들어 `IMessageManagerEx.SendVehicleAbnormalJson(json)` 으로 TS 에 전송한다. 송신 채널은 `tsAgent` (RabbitMQ).

**메시지 envelope** (모델: `RailVehicleAbnormalMessage`, `src/ACS/ACS.Communication/Mqtt/Model/RailVehicleAbnormalMessage.cs`):

```json
{
  "header": {
    "messageName": "RAIL-VEHICLEABNORMAL",
    "transactionId": "<Guid>",
    "timestamp": "<ES UTC>",
    "sender": "EI"
  },
  "data": {
    "vehicleId":   "<DB PK (VehicleEx.VehicleId)>",
    "commId":      "<MQTT vehicleId (CommId)>",
    "type":        "OPERATOR_ABORT",
    "code":        "200",
    "node":        "N02",
    "abnormalTime": "<AMR 보고 시각>",
    "eventTime":   "<ES 송신 시각 UTC>"
  }
}
```

- ES 는 AMR 의 `Type` 을 `data.type` 으로 그대로 싣고, 알려진 이름(`OPERATOR_ABORT`)일 때 대응 코드(`200`)를 `data.code` 에도 채운다. AMR 이 코드만 보내는 변종에 대해서도 TS 가 `type` 또는 `code` 둘 중 어느 쪽이든 매칭하여 분기할 수 있다.
- abnormal 블록이 없으면 ES 는 메시지를 보내지 않는다 (정상 흐름 노이즈 차단).

## 4. TS 처리 (RailVehicleAbnormalWorkflow)

**위치**: `src/ACS/ACS.Elsa/Workflows/Trans/RailVehicleAbnormalWorkflow.cs`

**DefinitionId**: `"RAIL-VEHICLEABNORMAL"` — `ESListener` 가 header.messageName 으로 워크플로우를 자동 라우팅.

**분기 규칙** (`RailVehicleAbnormalActivity.Execute`):

1. JSON 역직렬화 → `data.VehicleId` 로 `IResourceManagerEx.GetVehicle(...)` 조회. 없으면 Warn 후 종료.
2. `type == "OPERATOR_ABORT"` 또는 `code == "200"` 매칭이면 OPERATOR_ABORT 처리 (아래 5 단계).
3. 그 외 type 은 `logger.Warn("미처리 type=...")` 후 종료.

**OPERATOR_ABORT 6 단계 처리** (`HandleOperatorAbort`):

| Step | 동작 | 대상 |
|------|------|------|
| 1 | **멱등성 가드** — `vehicle.TransportCommandId` 가 공백이면 silent skip (로그 없음, DB 무변동) | — |
| 2 | TC 조회 (`ITransferManagerEx.GetTransportCommand`) | — |
| 3 | **JOBREPORT(COMPLETE, ErrorCode=200, ErrorMsg=OPERATOR_ABORT) → HS → MES** — 7-arg 오버로드 `IMessageManagerEx.SendJobReportToHost("COMPLETE", tc.JobId, vehicle.VehicleId, tc.JobType, tc.Description, errCode:"200", errMsg:"OPERATOR_ABORT")` 호출. HS 의 `HostJobReportWorkflow` → `ForwardJobReportToMesActivity` 가 XML 로 MES 송신. MES 로 가는 XML 에 `<ErrorCode>200</ErrorCode><ErrorMsg>OPERATOR_ABORT</ErrorMsg>` 가 채워져 정상 종료와 abort-driven COMPLETE 가 구분된다. **반드시 history/delete 보다 먼저** 실행 (삭제 후엔 tc.JobType/Description 무효해질 수 있음) | RabbitMQ → HS → MES |
| 4 | TC 히스토리 이관 → 삭제 | `NA_T_TRANSPORTCMD` (삭제), `NA_H_TRANSPORTCMDHISTORY` (추가) |
| 5 | Vehicle 할당 정보 초기화: `TransportCommandId=""`, `Path=""`, `AcsDestNodeId=""` | `NA_R_VEHICLE` |
| 6 | `TransferState = NOTASSIGNED`, `ProcessingState = IDLE` | `NA_R_VEHICLE` |

각 갱신은 message name `"RAIL-VEHICLEABNORMAL"` 로 히스토리(`UpdateVehicle*` 의 두 번째 인자) 에 기록되어 추적 가능하다. JOBREPORT 의 envelope 는 `IMessageManagerEx.SendJobReportToHost` 가 내부에서 `messageName="JOBREPORT"`, `data.Type="COMPLETE"` 로 구성한다.

**멱등성 의도**: AMR 이 운영자 조작 후에도 abnormal 블록을 매 status 주기마다 반복 송신할 수 있다. Step 1 가드가 없으면 TS 가 매 사이클마다 NA_R_VEHICLE 을 갱신하면서 히스토리 노이즈를 양산한다. 같은 vehicle 에 대해 TC 가 이미 정리된 상태(공백) 라면 이후의 abnormal 은 무시한다.

## 5. 데이터 영향 요약

| 대상 | 변경 | 조건 |
|------|------|------|
| HS → MES | JOBREPORT(Type=COMPLETE, JobID=tc.JobId, AmrId=vehicle.VehicleId, **ErrorCode=200, ErrorMsg=OPERATOR_ABORT**) RabbitMQ 송신 | OPERATOR_ABORT, TC 존재 |
| `NA_T_TRANSPORTCMD` | 행 삭제 (`JobId = vehicle.TransportCommandId`) | OPERATOR_ABORT, TC 존재 |
| `NA_H_TRANSPORTCMDHISTORY` | 행 추가 (CompletedMessage=`RAIL-VEHICLEABNORMAL`) | OPERATOR_ABORT, TC 존재, HistoryManager 등록됨 |
| `NA_R_VEHICLE` | `TransportCommandId=""`, `Path=""`, `AcsDestNodeId=""`, `TransferState=NOTASSIGNED`, `ProcessingState=IDLE` | OPERATOR_ABORT, vehicle.TransportCommandId 비어있지 않음 |
| 모두 | 변경 없음 | OPERATOR_ABORT, vehicle.TransportCommandId 공백 (멱등성 skip) — JOBREPORT 도 송신되지 않음 |
| 모두 | 변경 없음 | OPERATOR_ABORT 가 아닌 type (Warn 로그만) |

## 6. 확장 가이드

새 abnormal type 을 추가하려면:

1. **§2 의 코드 표에 행 추가** — 새 Code/Type 이름과 의미, TS 처리 방향을 기록.
2. **`RailVehicleAbnormalActivity.Execute` 의 분기에 case 추가** — `isOperatorAbort` 체크 패턴을 따라 새 매칭 조건과 처리 헬퍼 호출을 더한다.
3. (필요 시) `RailVehicleAbnormalData` 에 새 상수(Type 이름, Code) 추가.

ES 측은 변경 불필요 — abnormal 블록 자체가 있으면 항상 type 그대로 메시지를 만들기 때문에, 새 type 도 자동으로 TS 에 전달된다.

## 관련 파일

- ES Parse: `src/ACS/ACS.Elsa/Activities/MqttActivities.cs` — `ParseAmrStatusActivity`, `SendVehicleAbnormalActivity`
- ES Workflow: `src/ACS/ACS.Elsa/Workflows/Ei/VehicleStatusWorkflow.cs`
- 메시지 모델: `src/ACS/ACS.Communication/Mqtt/Model/RailVehicleAbnormalMessage.cs`, `AmrStatusMessage.cs`
- 송신 API: `IMessageManagerEx.SendVehicleAbnormalJson`, `MessageManagerExImplement` 구현
- TS Workflow: `src/ACS/ACS.Elsa/Workflows/Trans/RailVehicleAbnormalWorkflow.cs`
- 참고 패턴: `RailVehicleAlarmWorkflow.cs` (envelope·dispatch 구조), `RailVehicleDepositCompletedWorkflow.cs` Step 13-17 (TC 정리·Vehicle 초기화 시퀀스)
