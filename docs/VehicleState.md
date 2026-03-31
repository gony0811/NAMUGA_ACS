# ACS Vehicle Status Definition

## 1. NA_R_VEHICLE 데이터 구조

| 컬럼명 | 타입 | 최대 길이 | 설명 |
|--------|------|-----------|------|
| id (PK) | bigint | - | 자동 생성 시퀀스 |
| vehicleId | varchar | 64 | 차량 식별자 |
| COMMTYPE | varchar | 10 | 통신 타입 (기본값: "NIO") |
| COMMID | varchar | 64 | 통신 식별자 |
| vendor | varchar | 32 | 제조사 |
| version | varchar | 32 | 펌웨어 버전 |
| plcVersion | varchar | 32 | PLC 버전 |
| bayId | varchar | 64 | 베이 ID |
| carrierType | varchar | 8 | 캐리어 타입 (CARRIER/TRAY/FOUP) |
| connectionState | varchar | 16 | 연결 상태 (CONNECT/DISCONNECT) |
| alarmState | varchar | 8 | 알람 상태 (ALARM/NOALARM) |
| processingState | varchar | 20 | 처리 상태 (IDLE/RUN/CHARGE/PARK) |
| runState | varchar | 10 | 운행 상태 (RUN/STOP) |
| fullState | varchar | 10 | 적재 상태 (FULL/EMPTY) |
| state | varchar | 20 | 차량 상태 (INSTALLED/REMOVED 등) |
| batteryRate | int | - | 배터리 잔량 (%) |
| batteryVoltage | float | - | 배터리 전압 |
| currentNodeId | varchar | 64 | 현재 위치 노드 |
| acsDestNodeId | varchar | 64 | ACS 지정 목적지 노드 |
| vehicleDestNodeId | varchar | 64 | 차량 보고 목적지 노드 |
| transportCommandId | varchar | 64 | 반송 명령 ID |
| path | varchar | 2000 | 경로 정보 |
| nodeCheckTime | timestamp | - | 노드 확인 시각 |
| eventTime | timestamp | - | 이벤트 발생 시각 |
| installed | varchar | 20 | 설치 여부 (T/F) |
| transferState | varchar | 20 | 반송 상태 |
| LASTCHARGETIME | timestamp | - | 마지막 충전 시각 |
| LASTCHARGEBATTERY | float | - | 마지막 충전 배터리 잔량 |

---

## 2. AGV 코드별 처리 로직 (기존 Socket/NIO 방식)

AGV로부터 14바이트 바이너리 패킷을 수신하면 `MessageManagerExImplement.CreateVehicleMessage()`에서 Command Code를 파싱하여 `VehicleMessageEx`를 생성하고, 각 코드에 대응하는 서비스 로직을 실행한다.

> **소스 위치**: `ACS.Manager/Message/MessageManagerExImplement.cs` (lines 1355-1552)

### 2.1 C_CODE (Command Reply) — 명령 응답

AGV가 ACS의 이동/작업 명령을 수신 후 응답하는 코드.

**파싱 데이터**:
- `StationId`: Data[0..3] (4자리 스테이션 ID)

**CCodeType 서브타입**:

| CCodeType | 값 | 설명 |
|-----------|-----|------|
| C_CODE_TYPE_LEFTLOAD | 01 | 좌측 적재 |
| C_CODE_TYPE_LEFTUNLOAD | 02 | 좌측 하역 |
| C_CODE_TYPE_RIGHTLOAD | 03 | 우측 적재 |
| C_CODE_TYPE_RIGHTUNLOAD | 04 | 우측 하역 |
| C_CODE_TYPE_MANUAL | 05 | 수동 모드 |
| C_CODE_TYPE_LEFTLOAD_TURN | 06 | 좌측 적재 (회전) |
| C_CODE_TYPE_LEFTUNLOAD_TURN | 07 | 좌측 하역 (회전) |
| C_CODE_TYPE_RIGHTLOAD_TURN | 08 | 우측 적재 (회전) |
| C_CODE_TYPE_RIGHTUNLOAD_TURN | 09 | 우측 하역 (회전) |

**수정되는 DB 컬럼**: 직접 수정 없음 (TransportCommand 상태 관리에 사용)

**비즈니스 로직**:
- `InterfaceServiceEx.ReportAGVReplyCommand()` → Host에 **TRSJOBREPORT** XML 전송 (CmdType=JOBSTART)
  - JobID, CarrID, TransUnit(VehicleId), Source/Dest EQP/Port, CurrentLoc, ErrCode 포함
- `InterfaceServiceEx.ReportAGVArrivedReplyCommand()` → Host에 도착 확인 보고 (CmdType=ARRIVED)

### 2.2 S_CODE (AGV State) — 상태 보고

AGV가 주기적으로 자신의 운행/적재 상태를 보고하는 코드.

**파싱 데이터**:
- `StationId`: Data[0..3] (4자리 스테이션 ID)
- `RunState`: Data[4] (1자리) — 운행 상태
- `FullState`: Data[5] (1자리) — 적재 상태

**수정되는 DB 컬럼**:

| 컬럼 | 값 | 조건 |
|------|-----|------|
| `runState` | "RUN" / "STOP" | RunState 값 변경 시 |
| `fullState` | "FULL" / "EMPTY" | FullState 값 변경 시 |
| `processingState` | "RUN" / "IDLE" / "CHARGE" / "PARK" | 상태 전이에 따라 |
| `vehicleDestNodeId` | StationId 값 | 목적지 업데이트 시 |

**비즈니스 로직**:
- `ResourceServiceEx.UpdateVehicleRunState()` → `runState` 업데이트
- `ResourceServiceEx.UpdateVehicleFullState()` → `fullState` 업데이트
- `ResourceServiceEx.UpdateVehicleDestNodeId()` → 목적지 노드 업데이트
  - 충전 스테이션(06, 08)이면 → `ChangeVehicleProcessStateToCharge()` 호출
  - 할당된 TransportCommand가 있고 EMPTY 상태면 → 해당 Job을 QUEUED로 되돌림
- `ResourceServiceEx.ChangeVehicleProcessStateToRun()` → `processingState`=RUN, Idle 기록 삭제
- `ResourceServiceEx.ChangeVehicleProcessStateToIdle()` → `processingState`=IDLE, Idle 기록 생성/갱신

### 2.3 T_CODE (Tag Information) — 위치 태그 보고

AGV가 경로 상의 RFID/마그네틱 태그를 읽었을 때 현재 위치를 보고하는 코드.

**파싱 데이터**:
- `NodeId`: Data[2..5] (4자리 노드 ID)

**TCodeType 서브타입**:

| TCodeType | 값 | 설명 |
|-----------|-----|------|
| T_CODE_TYPE_REPORT | 0 | 보고 (AGV→ACS) |
| T_CODE_TYPE_COMMAND | 1 | 명령 (ACS→AGV) |
| T_CODE_TYPE_REPLY | 3 | 응답 |

**수정되는 DB 컬럼**:

| 컬럼 | 값 | 비고 |
|------|-----|------|
| `currentNodeId` | NodeId 값 | 항상 업데이트 |
| `nodeCheckTime` | 현재 시각 | 위치 확인 타임스탬프 |
| `processingState` | 상태 전이 | 충전 중 → 충전 스테이션 벗어나면 IDLE로 전환 |

**비즈니스 로직**:
- `ResourceServiceEx.ChangeVehicleLocation()` → `currentNodeId` 업데이트 (노드 존재 여부 검증)
- `ResourceServiceEx.ChangeVehicleProcessingState()` → 충전 상태에서 비충전 노드로 이동 시 IDLE 전환, CHARGEMOVE Job 삭제
- `ResourceServiceEx.GetTCodeType()` → KeyData에서 TCodeType 추출 (Report/Command/Reply 구분)
- 경로 재계산 및 TransferState 관리

### 2.4 R_CODE (Return Value) — 배터리 정보 보고

AGV가 배터리 전압 또는 잔량을 보고하는 코드.

**파싱 데이터**:
- `RCodeType`: Data[1] (1자리) — 보고 유형
- Type "1" (전압): `BatteryVoltage` = Data[2..3] + "." + Data[4] (예: "25.3")
- Type "2" (용량): `BatteryRate` = Data[3..4] (정수 %, 예: "85")

**수정되는 DB 컬럼**:

| 컬럼 | 값 | 조건 |
|------|-----|------|
| `batteryVoltage` | float (예: 25.3) | RCodeType="1" 이고 값 > 1 |
| `batteryRate` | int (예: 85) | RCodeType="2" |

**비즈니스 로직**:
- `ResourceServiceEx.UpdateVehicleVoltage()` → `batteryVoltage` 업데이트 (1V 미만은 무시)
- 배터리 이력 기록 (VehicleBatteryHistory — 현재 주석 처리됨)
- 저전압 임계값: `AVAIALBE_VOLTAGE` = 25.0V, `AVAIALBE_VOLTAGE_LIMIT` = 23.0V

### 2.5 E_CODE (Error Code) — 에러/알람 보고

AGV에 에러 발생 시 알람 코드를 보고하는 코드.

**파싱 데이터**:
- `ErrorCode`: Data[1..3] (3자리 알람 ID, NA_A_ALARMSPEC 테이블과 매칭)

**수정되는 DB 컬럼**:

| 컬럼 | 값 | 비고 |
|------|-----|------|
| `alarmState` | "ALARM" | 에러 발생 시 |

**비즈니스 로직**:
- `ResourceServiceEx.UpdateVehicleAlarmState()` → `alarmState`=ALARM으로 업데이트
- 알람 심각도 구분: `ALARM_SEVERITY_LIGHT`, `ALARM_SEVERITY_HEAVY`

### 2.6 M_CODE (Abnormal) — 이상 상태 보고

AGV에서 비정상 상황 발생 시 유형별로 보고하는 코드.

**파싱 데이터**:
- `MCodeType`: Data[1] (1자리 이상 유형)
- `NodeId`: Data[2..5] (4자리 노드 ID)

**M_CODE 서브타입별 처리**:

| MCodeType | 이벤트 | Host 보고 메서드 |
|-----------|--------|-----------------|
| 충전 실패 | AGV 충전 실패 | `SendVehicleMessageMCodeAgvChargingFail` |
| 마그태그 이탈 | 마그네틱 태그 미감지 | `SendVehicleMessageMCodeRecoverMissMagTag` |
| 마그태그 복구 성공/실패 | 태그 이탈 복구 결과 | `MCodeRecoverMissMagTagSuccess/Fail` |
| 레일 이탈 | AGV 레일 이탈 | `SendVehicleMessageMCodeRecoverAgvOutRail` |
| 레일 이탈 복구 성공/실패 | 이탈 복구 결과 | `MCodeRecoverAgvOutRailSuccess/Fail` |
| 자동 시작 | AGV 자동 시작 | `SendVehicleMessageMCodeAgvAutoStart` |
| 자동 시작 성공/실패 | 자동 시작 결과 | `MCodeAgvAutoStartSuccess/Fail` |
| PBS OFF | PBS 스위치 OFF | `SendVehicleMessageMCodeAgvTurnPbsOff` |
| PBS OFF 성공/실패 | PBS OFF 결과 | `MCodeAgvTurnPbsOffSuccess/Fail` |
| 후진 복구 | AGV 후진 | `SendVehicleMessageMCodeRecoveryAgvBack` |
| 후진 복구 성공/실패 | 후진 결과 | `MCodeRecoveryAgvBackSuccess/Fail` |
| 소닉 센서 | 초음파 센서 감지 | `SendVehicleMessageMCodeAgvSensorSonic` |
| 소닉 센서 성공/실패 | 센서 처리 결과 | `MCodeAgvSensorSonicSuccess/Fail` |
| HMI 버전 | HMI 버전 보고 | `SendVehicleMessageMCodeHmiVersion` |

**수정되는 DB 컬럼**: MCodeType에 따라 다름 (주로 `processingState`, `alarmState` 영향)

**비즈니스 로직**:
- 각 MCodeType별 XML 문서 생성 후 Host로 전송
- TransferServiceEx에서 반송 Job의 Source/Dest 포트 변경 가능 (M_CODE 트리거 시)

### 2.7 H_CODE (Alive Check Reply) — 하트비트 응답

ACS가 보낸 하트비트 요청에 대한 AGV 응답. 연결 상태 감시용.

**파싱 데이터**: 없음 (KeyData에 트랜잭션 ID 포함)

**수정되는 DB 컬럼**: 없음

**비즈니스 로직**:
- `VehicleInterfaceServiceEx.SendNioHeartCode()` → ACS→AGV 하트비트 전송 (트랜잭션 ID = 현재 시각 "HHmmss")
- `VehicleInterfaceServiceEx.ReceiveNioHeartCode()` → AGV 응답의 트랜잭션 ID 매칭 검증
  - 매칭 성공 → `transactionMap`에서 제거 (정상)
  - 매칭 실패 → 경고 로그 (연결 불안정 의심)
- `IsExistTransactionId()` → 미응답 하트비트 존재 여부 확인 (연결 끊김 판단)

### 2.8 L_CODE (Loading) — 적재 완료 보고

AGV가 Source 포트에서 화물 적재를 완료했을 때 보고.

**파싱 데이터**:
- `NodeId`: Data[0..3] (4자리 노드 ID)

**수정되는 DB 컬럼**: `currentNodeId`, 반송 상태 관련 컬럼

### 2.9 U_CODE (Unloading) — 하역 완료 보고

AGV가 Dest 포트에서 화물 하역을 완료했을 때 보고.

**파싱 데이터**:
- `NodeId`: Data[0..3] (4자리 노드 ID)

**수정되는 DB 컬럼**: `currentNodeId`, 반송 상태 관련 컬럼

---

## 3. AMR 상태 처리 로직 (MQTT 방식)

AMR은 MQTT를 통해 JSON 형식의 상태 메시지를 발행하며, `ProcessAmrStatusActivity`에서 처리한다.

> **소스 위치**: `ACS.Elsa/Activities/MqttActivities.cs` (lines 146-299)
> **메시지 모델**: `ACS.Communication/Mqtt/Model/AmrStatusMessage.cs`

### 3.1 MQTT 토픽 구조

| 토픽 패턴 | 설명 |
|-----------|------|
| `amr/{vehicleId}/status` | 상태 보고 (주기적) |
| `amr/{vehicleId}/heartbeat` | 하트비트 |
| `amr/{vehicleId}/alarm` | 알람 보고 |
| `amr/{vehicleId}/response` | 명령 응답 |

### 3.2 AmrStatusMessage → Vehicle 매핑

AMR의 `CommType`="MQTT"이고, `CommId`=MQTT vehicleId로 Vehicle을 조회한다.

| AMR 필드 | 매핑 로직 | DB 컬럼 | 값 |
|----------|----------|---------|-----|
| RobotState | MapRobotStateToRunState() | `runState` | "Started"→RUN, "Off"/"Paused"→STOP |
| ErrorCode | 0이면 정상 | `alarmState` | 0→NOALARM, >0→ALARM |
| Battery.VoltagePercent | int 변환 | `batteryRate` | 0~100 (%) |
| NavigationStatus + ChargingState | MapToProcessingState() | `processingState` | 아래 표 참조 |
| (항상) | DateTime.UtcNow | `eventTime` | 수신 시각 |
| Pose (X, Y, Angle) | 로그만 기록 | - | DB 미저장 (향후 확장 필요) |

**ProcessingState 매핑 우선순위**:

| 조건 | processingState | 우선순위 |
|------|----------------|----------|
| Battery.ChargingState == "Charging" | CHARGE | 1 (최우선) |
| NavigationStatus == "Moving" | RUN | 2 |
| NavigationStatus == "WaitingForArrival" | IDLE | 3 |

### 3.3 AGV 코드 → AMR 필드 대응 관계

기존 AGV 코드 체계와 AMR MQTT 메시지 간의 대응:

| AGV 코드 | AGV 데이터 | AMR 대응 필드 | 현재 구현 상태 |
|----------|-----------|--------------|--------------|
| S_CODE (RunState) | Data[4] | `RobotState` | **구현됨** (Started→RUN, Off/Paused→STOP) |
| S_CODE (FullState) | Data[5] | - | **미구현** (AMR에 적재 센서 정보 없음) |
| R_CODE Type 1 (전압) | Data[2..4] | - | **미구현** (AMR은 전압 대신 VoltagePercent 사용) |
| R_CODE Type 2 (용량) | Data[3..4] | `Battery.VoltagePercent` | **구현됨** (batteryRate로 변환) |
| E_CODE (에러) | Data[1..3] | `ErrorCode` | **부분 구현** (alarmState만 변경, 알람 코드 상세 미매핑) |
| T_CODE (위치) | Data[2..5] | `Pose` (X,Y,Angle) | **미구현** (좌표→노드 매핑 로직 필요) |
| M_CODE (이상) | Data[1] | - | **미구현** (AMR 비정상 이벤트 정의 필요) |
| H_CODE (하트비트) | - | `amr/{id}/heartbeat` 토픽 | **토픽만 구독** (처리 로직 미구현) |
| C_CODE (명령응답) | Data[0..3] | `amr/{id}/response` 토픽 | **토픽만 구독** (처리 로직 미구현) |
| L_CODE (적재) | Data[0..3] | - | **미구현** |
| U_CODE (하역) | Data[0..3] | - | **미구현** |

---

## 4. 상태 전이 다이어그램

### 4.1 ProcessingState 전이

```
IDLE (대기)
  │
  ├─[S_CODE RUN / NavigationStatus=Moving]──→ RUN (운행 중)
  │                                            │
  │                                            ├─[S_CODE STOP / NavigationStatus=WaitingForArrival]──→ IDLE
  │                                            └─[충전 스테이션 도착 / ChargingState=Charging]──→ CHARGE
  │
  ├─[충전 스테이션 도착]──→ CHARGE (충전 중)
  │                          │
  │                          └─[비충전 노드 이동 / ChargingState≠Charging]──→ IDLE
  │
  └─[파킹 스테이션 도착]──→ PARK (주차)
```

### 4.2 TransferState 전이 (반송 명령 수행 흐름)

```
NOTASSIGNED ──[Job 할당]──→ ASSIGNED
                              │
                              ├──→ ASSIGNED_ENROUTE (Source로 이동 중)
                              │
                              ├──→ ASSIGNED_ACQUIRING (화물 적재 중)
                              │         │
                              │         └──→ ACQUIRE_COMPLETE (적재 완료)
                              │
                              ├──→ ASSIGNED_DEPOSITING (화물 하역 중)
                              │         │
                              │         └──→ DEPOSIT_COMPLETE (하역 완료)
                              │
                              └──→ NOTASSIGNED (Job 완료/취소)
```

---

MES(Host)와 ACS 간 통신은 **TCP/IP 기반 바이너리 프레임 XML 프로토콜**을 사용한다.

### 1.1 프레임 형식