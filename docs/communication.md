# MES-ACS 메시지 통신 사양서

## 1. 프로토콜 개요

MES(Host)와 ACS 간 통신은 **TCP/IP 기반 바이너리 프레임 XML 프로토콜**을 사용한다.

### 1.1 프레임 형식

```
┌──────────────────────┬──────────────────────────────┐
│ Length Header (4byte) │ XML Body (UTF-8, BOM 허용)   │
│ little-endian int32   │                              │
└──────────────────────┴──────────────────────────────┘
```

- **Length Header**: XML 본문의 바이트 수를 4바이트 little-endian 정수로 인코딩
- **XML Body**: UTF-8 인코딩. BOM(EF BB BF)이 포함될 수 있으며 수신 측에서 자동 처리
- **최대 메시지 크기**: 10MB

### 1.2 연결 모델

**connect-per-message** 방식: 메시지 전송 시마다 TCP 연결 → 전송 → 연결 종료.

| 방향 | 역할 | 기본 포트 | 설명 |
|------|------|-----------|------|
| Host → ACS | ACS가 서버(Listen) | 3334 | MOVECMD, ACTIONCMD, MOVECANCEL 수신 |
| ACS → Host | ACS가 클라이언트(Connect) | 3333 | JOBREPORT 전송 |

### 1.3 설정 (appsettings.json)

```json
{
  "Destination": {
    "Host": {
      "Tcp": {
        "ListenPort": 3334,
        "SendHost": "127.0.0.1",
        "SendPort": 3333,
        "ReconnectIntervalMs": 5000
      }
    }
  },
  "Acs": {
    "Process": { "Name": "ACS01" },
    "Host": {
      "DestSubject": "/HQ/MES01",
      "ReplySubject": "/HQ/ACS01"
    }
  }
}
```

---

## 2. XML 메시지 공통 구조

모든 메시지는 동일한 루트 구조를 따른다.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Msg>
  <Command>{메시지명}</Command>
  <Header>
    <DestSubject>{수신 주소}</DestSubject>
    <ReplySubject>{발신 주소}</ReplySubject>
  </Header>
  <DataLayer>
    <!-- 메시지별 필드 -->
  </DataLayer>
</Msg>
```

| 요소 | 설명 |
|------|------|
| `Command` | 메시지 타입 식별자 (MOVECMD, JOBREPORT 등) |
| `Header/DestSubject` | 수신 측 주소 (예: `/HQ/ACS01`, `/HQ/MES01`) |
| `Header/ReplySubject` | 발신 측 주소 (응답 회신 주소) |
| `DataLayer` | 메시지별 데이터 필드 |

> **참고**: JOBREPORT 응답 시 MOVECMD의 `ReplySubject`가 JOBREPORT의 `DestSubject`로, `DestSubject`가 `ReplySubject`로 스왑된다.

---

## 3. 메시지 타입 상세

### 3.1 MOVECMD (Host → ACS)

반송 작업 명령. Host가 ACS에게 자재 이동을 지시한다.

**DataLayer 필드:**

| 필드 | 타입 | 필수 | 설명 | 예시 |
|------|------|------|------|------|
| `AcsId` | string | O | ACS 시스템 ID | `ACS01` |
| `DestLoc` | string | O | 목적지 위치 | `LOC001` |
| `DestPort` | string | | 목적지 포트 | `PORT01` |
| `ActionType` | string | | 작업 타입 | `LOAD`, `UNLOAD` |
| `SourceLoc` | string | O | 출발지 위치 | `LOC000` |
| `SourcePort` | string | | 출발지 포트 | `PORT00` |
| `JobID` | string | O | 작업 ID (고유) | `JOB20260303141832001` |
| `MaterialType` | string | | 자재 타입 | `MAGAZINE` |
| `UserID` | string | | 요청자 ID | `MES01` |

**예시:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Msg>
  <Command>MOVECMD</Command>
  <Header>
    <DestSubject>/HQ/ACS01</DestSubject>
    <ReplySubject>/HQ/MES01</ReplySubject>
  </Header>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <DestLoc>LOC001</DestLoc>
    <DestPort>PORT01</DestPort>
    <ActionType>LOAD</ActionType>
    <SourceLoc>LOC000</SourceLoc>
    <SourcePort>PORT00</SourcePort>
    <JobID>JOB20260303141832001</JobID>
    <MaterialType>MAGAZINE</MaterialType>
    <UserID>MES01</UserID>
  </DataLayer>
</Msg>
```

**C# 모델**: `ACS.Communication.Host.Models.MoveCommandData`

---

### 3.2 JOBREPORT (ACS → Host)

작업 상태 보고. ACS가 Host에게 작업 진행 상황을 보고한다.

**DataLayer 필드:**

| 필드 | 타입 | 필수 | 설명 | 예시 |
|------|------|------|------|------|
| `AcsId` | string | O | ACS 시스템 ID | `ACS01` |
| `Type` | string | O | 보고 유형 | `RECEIVE`, `ARRIVED`, `COMPLETE`, `CANCEL` |
| `AmrId` | string | | AMR(차량) ID | `AMR01` |
| `ActionType` | string | | 작업 타입 (MOVECMD에서 전달) | `LOAD`, `UNLOAD` |
| `JobID` | string | O | 작업 ID (MOVECMD의 JobID와 일치) | `JOB20260303141832001` |
| `MaterialType` | string | | 자재 타입 | `MAGAZINE` |
| `UserID` | string | | 요청자 ID | `MES01` |

**Report Type 값:**

| Type | 설명 |
|------|------|
| `RECEIVE` | 작업 수신 확인 (MOVECMD 수신 즉시 응답) |
| `ARRIVED` | 차량이 목적지에 도착 |
| `COMPLETE` | 작업 완료 |
| `CANCEL` | 작업 취소 (에러 코드 포함 가능) |

**예시:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Msg>
  <Command>JOBREPORT</Command>
  <Header>
    <DestSubject>/HQ/MES01</DestSubject>
    <ReplySubject>/HQ/ACS01</ReplySubject>
  </Header>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <Type>RECEIVE</Type>
    <AmrId>AMR01</AmrId>
    <ActionType>LOAD</ActionType>
    <JobID>JOB20260303141832001</JobID>
    <MaterialType>MAGAZINE</MaterialType>
    <UserID>MES01</UserID>
  </DataLayer>
</Msg>
```

**C# 모델**: `ACS.Communication.Host.Models.JobReportData`

---

### 3.3 ACTIONCMD (Host → ACS)

단일 위치 액션 명령. MOVECMD와 달리 출발/목적지 구분 없이 하나의 위치에서 동작을 수행한다.

**DataLayer 필드:**

| 필드 | 타입 | 필수 | 설명 | 예시 |
|------|------|------|------|------|
| `AcsId` | string | O | ACS 시스템 ID | `ACS01` |
| `TargetLoc` | string | O | 대상 위치 | `LOC001` |
| `TargetPort` | string | | 대상 포트 | `PORT01` |
| `JobID` | string | O | 작업 ID | `JOB001` |
| `MaterialType` | string | | 자재 타입 | `MAGAZINE` |
| `ActionType` | string | | 작업 타입 | `PICK` |
| `UserID` | string | | 요청자 ID | `MES01` |

**예시:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Msg>
  <Command>ACTIONCMD</Command>
  <Header>
    <DestSubject>/HQ/ACS01</DestSubject>
    <ReplySubject>/HQ/MES01</ReplySubject>
  </Header>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <TargetLoc>LOC001</TargetLoc>
    <TargetPort>PORT01</TargetPort>
    <JobID>JOB001</JobID>
    <MaterialType>MAGAZINE</MaterialType>
    <ActionType>PICK</ActionType>
    <UserID>MES01</UserID>
  </DataLayer>
</Msg>
```

**C# 모델**: `ACS.Communication.Host.Models.ActionCommandData`

---

### 3.4 MOVECANCEL (Host → ACS)

반송 취소 명령. 기존에 발행된 MOVECMD를 취소한다.

**DataLayer 필드:**

| 필드 | 타입 | 필수 | 설명 | 예시 |
|------|------|------|------|------|
| `AcsId` | string | O | ACS 시스템 ID | `ACS01` |
| `DestLoc` | string | | 목적지 위치 | `LOC001` |
| `SourceLoc` | string | | 출발지 위치 | `LOC000` |
| `JobId` | string | O | 취소할 작업 ID | `JOB20260303141832001` |
| `MaterialType` | string | | 자재 타입 | `MAGAZINE` |
| `UserId` | string | | 요청자 ID | `MES01` |

> **주의**: MOVECANCEL은 `JobId`, `UserId` (소문자 d)를 사용하고, 다른 메시지는 `JobID`, `UserID` (대문자 D)를 사용한다.

**예시:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Msg>
  <Command>MOVECANCEL</Command>
  <Header>
    <DestSubject>/HQ/ACS01</DestSubject>
    <ReplySubject>/HQ/MES01</ReplySubject>
  </Header>
  <DataLayer>
    <AcsId>ACS01</AcsId>
    <DestLoc>LOC001</DestLoc>
    <SourceLoc>LOC000</SourceLoc>
    <JobId>JOB20260303141832001</JobId>
    <MaterialType>MAGAZINE</MaterialType>
    <UserId>MES01</UserId>
  </DataLayer>
</Msg>
```

**C# 모델**: `ACS.Communication.Host.Models.MoveCancelData`

---

## 4. 메시지 흐름

### 4.1 MOVECMD → JOBREPORT 시퀀스

```
Host(MES)                              ACS
   │                                    │
   │──── TCP Connect (→ port 3334) ────→│
   │──── [4B len] + MOVECMD XML ───────→│
   │←─── TCP Close ─────────────────────│
   │                                    │
   │  (ACS가 MOVECMD 수신, 워크플로우 실행)
   │                                    │
   │←─── TCP Connect (← port 3333) ────│
   │←─── [4B len] + JOBREPORT XML ─────│  Type=RECEIVE
   │──── TCP Close ────────────────────→│
   │                                    │
   │  (차량 배차 → 이동 → 도착)
   │                                    │
   │←─── JOBREPORT (ARRIVED) ──────────│
   │←─── JOBREPORT (COMPLETE) ─────────│
```

### 4.2 내부 처리 흐름

```
HostTcpGateway (Listen:3334)
    ↓ MessageReceived 이벤트
HostBridgeService
    ↓ Command 판별 → 워크플로우 실행
Elsa Workflow (HostMoveCmdWorkflow)
    ├─ SendJobReportActivity → JOBREPORT(RECEIVE) 전송
    └─ CreateTransportCommandActivity → DB에 TransportCommand 생성
```

---

## 5. TransportCommand 필드 매핑

MOVECMD 수신 시 `CreateTransportCommandActivity`가 DB `TransportCommand` 레코드를 생성한다.

| MOVECMD 필드 | TransportCommandEx 필드 | 변환 |
|-------------|------------------------|------|
| `JobID` | `Id` | 그대로 |
| `SourceLoc`:`SourcePort` | `Source` | `{SourceLoc}:{SourcePort}` 결합 |
| `DestLoc`:`DestPort` | `Dest` | `{DestLoc}:{DestPort}` 결합 |
| `ActionType` | `JobType` | 그대로 |
| `MaterialType` | `Description` | 그대로 |
| `AcsId` | `EqpId` | 그대로 |

---

## 6. 에러 코드 (JOBREPORT CANCEL)

JOBREPORT의 Type이 `CANCEL`인 경우 사용되는 에러 코드:

| 코드 | 이름 | 설명 |
|------|------|------|
| 0 | HOSTCANCEL | Host 요청에 의한 취소 |
| -1 | CARRIERLOADED | 캐리어가 이미 로드됨 |
| 1 | CARRIERREMOVED | 캐리어가 제거됨 |
| -2 | DESTOCCUPIED | 목적지가 점유됨 |
| 4 | SOURCEEMPTY | 출발지가 비어있음 |
| 5 | SOURCEPIOPORTCHECKERROR | 출발지 PIO 포트 체크 에러 |
| 6 | SOURCEPIOCONERROR | 출발지 PIO 연결 에러 |
| 7 | SOURCEPIOREQERROR | 출발지 PIO 요청 에러 |
| 8 | SOURCEPIORUNERROR | 출발지 PIO 실행 에러 |
| 10 | VEHICLEEMPTY | 차량이 비어있음 |
| -11 | VEHICLEOCCUPIED | 차량이 점유됨 |
| 40 | DESTPIOPORTCHECKERROR | 목적지 PIO 포트 체크 에러 |
| 41 | DESTPIOCONERROR | 목적지 PIO 연결 에러 |
| 42 | DESTPIOREQERROR | 목적지 PIO 요청 에러 |
| 43 | DESTPIORUNERROR | 목적지 PIO 실행 에러 |
| 50 | UNDEFINED | 정의되지 않은 에러 |
| 99 | UICANCEL | UI에서 사용자가 취소 |

---

## 7. 구현 파일 참조

| 파일 | 역할 |
|------|------|
| `ACS.Communication/Host/HostMessageProtocol.cs` | 바이너리 프레이밍, 메시지 읽기/쓰기 |
| `ACS.Communication/Host/HostTcpGateway.cs` | TCP 서버/클라이언트 구현 |
| `ACS.Communication/Host/HostXmlSerializer.cs` | XML 직렬화/역직렬화 |
| `ACS.Communication/Host/Models/*.cs` | 메시지 데이터 모델 |
| `ACS.Core/Host/IHostTcpGateway.cs` | 게이트웨이 인터페이스 |
| `ACS.Core/Host/IHostMessageService.cs` | 메시지 빌드 서비스 인터페이스 |
| `ACS.App/Host/HostMessageService.cs` | 메시지 빌드 구현 |
| `ACS.App/Host/HostBridgeService.cs` | Host ↔ Elsa 워크플로우 브릿지 |
| `ACS.Elsa/Workflows/HostMoveCmdWorkflow.cs` | MOVECMD 워크플로우 |
| `ACS.Elsa/Activities/HostActivities.cs` | Host 관련 워크플로우 액티비티 |
| `ACS.Host.Test/` | MES 시뮬레이터 (테스트용) |

---
---

# RabbitMQ 내부 통신 (프로세스 간 메시지 버스)

ACS 시스템 내부의 프로세스 간 통신은 RabbitMQ를 메시지 버스(MSB)로 사용한다.
각 프로세스는 `MsbRabbitMQModule`에서 리스너(수신)와 센더(송신)를 Autofac DI에 등록한다.

## 1. 프로세스 개요

| 프로세스 | 타입 | 설정 예시 | 역할 |
|----------|------|-----------|------|
| TS01_P | `trans` | `ACS.App/appsettings.json` | 반송 서버 — 중앙 메시지 허브, 워크플로우 실행 |
| HS01_P | `host` | `deploy/HS01_P/appsettings.json` | Host 서버 — Trans와 동일 MSB 구성 (MES 연동 전용) |
| UI01_P | `ui` | `deploy/UI01_P/appsettings.json` | UI 서버 — Trans와 동일 MSB 구성 |
| CS01_P | `control` | `deploy/CS01_P/appsettings.json` | 제어 서버 — HeartBeat, 애플리케이션 상태 감시 |
| DS01_P | `daemon` | (런타임 구성) | 데몬 — 주기 스케줄 잡 실행 |
| ES01_P | `ei` | (런타임 구성) | 장비 인터페이스 — 설비 통신 |

> `trans`, `query`, `report`, `host`, `ui` 타입은 모두 `RegisterTransMsb()`를 공유한다.

## 2. Queue 네이밍 규칙

```
${server.domain} / {역할} / {세부}
```

- `${server.domain}` → `appsettings.json`의 `Destination:DomainValue` (기본: `VM/DEMO`)
- `@{application}` → `Acs:Process:Name` (예: `TS01_P`, `CS01_P`)

**Placeholder 해석 예시:**

| 설정값 | 해석 결과 |
|--------|-----------|
| `${server.domain}/HOST/LISTENER` | `VM/DEMO/HOST/LISTENER` |
| `${server.domain}/CONTROL/AGENT/@{application}` | `VM/DEMO/CONTROL/AGENT/TS01_P` |
| `${server.domain}/UI/SENDER.*` | `VM/DEMO/UI/SENDER.*` |

## 3. 통신 패턴 (Cast Option)

| 패턴 | RabbitMQ 구현 | 용도 |
|------|-------------|------|
| **UNICAST** | Direct queue (`BasicPublish(exchange:"", routingKey:queue)`) | 1:1 메시지 전달 |
| **MULTICAST** | Fanout exchange (`BasicPublish(exchange:name, routingKey:"")`) | 1:N 브로드캐스트 (UI 갱신 등) |
| **RPCSERVER** | Durable queue + ReplyTo 헤더 | 요청-응답 서버측 |
| **RPCCLIENT** | Auto-reply queue + CorrelationId | 요청-응답 클라이언트측 (HeartBeat) |

## 4. Queue별 송수신 관계

### 4.1 전체 Queue 목록

| # | Queue / Exchange 이름 | 타입 | 설명 |
|---|----------------------|------|------|
| 1 | `VM/DEMO/HOST/LISTENER` | Queue (UNICAST) | Host(MES) → Trans 메시지 수신 |
| 2 | `VM/DEMO/HOST/SENDER` | Queue (UNICAST) | Trans → Host(MES) 메시지 발신 |
| 3 | `VM/DEMO/ES/LISTENER` | Queue (UNICAST) | ES → Trans 메시지 수신 |
| 4 | `VM/DEMO/ES/{appName}` | Queue (UNICAST) | Trans → 특정 ES 프로세스 메시지 발신 |
| 5 | `VM/DEMO/UI/LISTENER` | Queue (UNICAST) | UI → Trans 메시지 수신 |
| 6 | `VM/DEMO/UI/SENDER` | Exchange (MULTICAST) | Trans/Control → 전체 UI 브로드캐스트 |
| 7 | `VM/DEMO/UI/SENDER.*` | Exchange (MULTICAST) | Daemon → 전체 UI 브로드캐스트 |
| 8 | `VM/DEMO/DAEMON/LISTENER` | Queue (UNICAST) | Daemon → Trans 스케줄 메시지 수신 |
| 9 | `VM/DEMO/CONTROL/AGENT/{appName}` | Queue (RPCSERVER) | Control → 각 프로세스 HeartBeat/제어 |
| 10 | `VM/DEMO/CONTROL/AGENT/LISTENER` | Queue (UNICAST) | Control → Trans 메시지 발신 |
| 11 | `/BN1/BNE3/ACS/FACTORY1/ACSmgr_2F` | Queue (UNICAST) | MES Host 수신 (사이트별 커스텀 주소) |
| 12 | `/BN1/BNE3/MOS/CLIENT/MHSmgr` | Queue (UNICAST) | MES Host 발신 (사이트별 커스텀 주소) |

### 4.2 프로세스별 등록 상세

#### Trans 프로세스 (TS01_P) — `RegisterTransMsb()`

**리스너 (수신):**

| 등록명 | 리스너명 | Queue | 패턴 | 송신자 |
|--------|---------|-------|------|--------|
| `HostListener` | HOSTLISTENER | `/BN1/BNE3/ACS/FACTORY1/ACSmgr_2F` | UNICAST | Host(MES) |
| `EsListener` | ESLISTENER | `VM/DEMO/ES/LISTENER` | UNICAST | ES 프로세스 |
| `UiListener` | UILISTENER | `VM/DEMO/UI/LISTENER` | UNICAST | UI 프로세스 |
| `DaemonListener` | DAEMONLISTENER | `VM/DEMO/DAEMON/LISTENER` | UNICAST | Daemon 프로세스 |
| `ControlListener` | DAEMONLISTENER | `VM/DEMO/CONTROL/AGENT/LISTENER` | UNICAST | Control 프로세스 |
| `ApplicationControlAgentListener` | — | `VM/DEMO/CONTROL/AGENT/TS01_P` | RPCSERVER | Control HeartBeat |

**센더 (송신):**

| 등록명 | 센더명 | 목적지 | 패턴 | 수신자 |
|--------|-------|--------|------|--------|
| `HostAgentSender` | HOSTSENDER | `/BN1/BNE3/MOS/CLIENT/MHSmgr` | UNICAST | Host(MES) |
| `EsAgentSender` | ESSENDER | `VM/DEMO/ES/{appName}` | UNICAST | 특정 ES 프로세스 |
| `UiAgentSender` | UISENDER | `VM/DEMO/UI/SENDER` | MULTICAST | 전체 UI 클라이언트 |

> Trans에서 `IMessageAgent` 단일 resolve 시 마지막 등록인 **UiAgentSender**가 반환됨.
> 워크플로우 내에서 Named resolve로 특정 센더를 지정하여 사용.

---

#### Daemon 프로세스 (DS01_P) — `RegisterDaemonMsb()`

**리스너 (수신):**

| 등록명 | Queue | 패턴 | 송신자 |
|--------|-------|------|--------|
| `ApplicationControlAgentListener` | `VM/DEMO/CONTROL/AGENT/DS01_P` | RPCSERVER | Control HeartBeat |

**센더 (송신):**

| 등록명 | 목적지 | 패턴 | 수신자 | 비고 |
|--------|--------|------|--------|------|
| `UiAgentSender` | `VM/DEMO/UI/SENDER.*` | MULTICAST | 전체 UI | 먼저 등록 |
| `TsAgentSender` | `VM/DEMO/DAEMON/LISTENER` | UNICAST | Trans(DaemonListener) | **마지막 등록 → IMessageAgent 기본값** |

> Awake 스케줄 잡 6개(`AwakeQueueTransportJob` 등)가 `IMessageAgent`로 메시지를 발신.
> 마지막 등록인 `TsAgentSender`가 resolve되어 `VM/DEMO/DAEMON/LISTENER` → Trans로 전달됨.

**Awake 잡 메시지 목록:**

| 잡 | 메시지명 | 주기 | 목적지 (TsAgentSender) |
|----|---------|------|----------------------|
| AwakeQueueTransportJob | `SCHEDULE-QUEUEJOB` | 20초 | `VM/DEMO/DAEMON/LISTENER` |
| AwakeChargeTransportJob | `SCHEDULE-CHARGEJOB` | 20초 | `VM/DEMO/DAEMON/LISTENER` |
| AwakeCallVehicleStopWaitJob | `SCHEDULE-CALLIDLEVEHICLE` | 5초 | `VM/DEMO/DAEMON/LISTENER` |
| AwakeCheckVehiclesJob | `SCHEDULE-CHECKVEHICLES` | 10초 | `VM/DEMO/DAEMON/LISTENER` |
| AwakeCheckCrossNodeJob | `SCHEDULE-CHECKCROSSNODE` | 5초 | `VM/DEMO/DAEMON/LISTENER` |
| AwakeCheckServerTimeJob | `SCHEDULE-CHECKSERVERTIME` | 5분 | `VM/DEMO/DAEMON/LISTENER` |

---

#### Control 프로세스 (CS01_P) — `RegisterControlMsb()`

**리스너 (수신):**

| 등록명 | 리스너명 | Queue | 패턴 | 송신자 |
|--------|---------|-------|------|--------|
| `CsListener` | CsListener | `VM/DEMO/CONTROL/AGENT/CS01_P` | UNICAST | 워크플로우 응답 |

**센더 (송신):**

| 등록명 | 센더명 | 목적지 | 패턴 | 수신자 |
|--------|-------|--------|------|--------|
| `CsSenderToServer` | CsSender | `VM/DEMO/CONTROL/AGENT/LISTENER` | UNICAST | Trans(ControlListener) |
| `CsSenderToUi` | CsSender | `VM/DEMO/UI/SENDER` | MULTICAST | 전체 UI |
| `HeartBeatRpcSender` | HeartBeatRpcSender | `VM/DEMO/CONTROL/AGENT` (prefix) | RPCCLIENT | 각 프로세스 (동적 목적지) |

> HeartBeat RPC: `VM/DEMO/CONTROL/AGENT` prefix에 대상 프로세스명을 붙여 동적 목적지 생성.
> 예: `VM/DEMO/CONTROL/AGENT/TS01_P`, `VM/DEMO/CONTROL/AGENT/DS01_P` 등.

---

#### EI 프로세스 (ES01_P) — `RegisterEiMsb()`

**리스너 (수신):**

| 등록명 | 리스너명 | Queue | 패턴 | 송신자 |
|--------|---------|-------|------|--------|
| `TransAgentListener` | TsAgentListener | `VM/DEMO/ES/ES01_P` | UNICAST | Trans(EsAgentSender) |
| `ApplicationControlAgentListener` | — | `VM/DEMO/CONTROL/AGENT/ES01_P` | RPCSERVER | Control HeartBeat |

**센더 (송신):**

| 등록명 | 센더명 | 목적지 | 패턴 | 수신자 |
|--------|-------|--------|------|--------|
| `TransAgentSender` | tsAgentSender | `VM/DEMO/ES/LISTENER` | UNICAST | Trans(EsListener) |

## 5. 메시지 흐름 다이어그램

### 5.1 전체 프로세스 간 통신 흐름

```
                          ┌─────────────────────────────────────────────┐
                          │              RabbitMQ Broker                │
                          └─────────────────────────────────────────────┘

  ┌──────────┐                                                    ┌──────────┐
  │ Host(MES)│                                                    │ Control  │
  │          │                                                    │ (CS01_P) │
  └────┬─────┘                                                    └────┬─────┘
       │                                                               │
       │ /BN1/.../ACSmgr_2F        VM/DEMO/CONTROL/AGENT/LISTENER      │
       │ ────────────────────→  ┌──────────┐  ←────────────────────────│
       │                        │  Trans   │                           │
       │ /BN1/.../MHSmgr        │ (TS01_P) │  VM/DEMO/CONTROL/AGENT/* │
       │ ←────────────────────  │          │  ←── HeartBeat RPC ──────│
       │                        └──┬───┬───┘                           │
       │                           │   │                               │
       │          ┌────────────────┘   └─────────────────┐             │
       │          │                                      │             │
       │   VM/DEMO/ES/{app}                    VM/DEMO/UI/SENDER       │
       │          │                          (MULTICAST exchange)       │
       │          ▼                                      │             │
       │   ┌──────────┐                                  ▼             │
       │   │   EI     │                           ┌──────────┐        │
       │   │ (ES01_P) │                           │  UI 전체  │        │
       │   └────┬─────┘                           │(UI01_P 등)│        │
       │        │                                 └──────────┘        │
       │        │ VM/DEMO/ES/LISTENER                     ▲            │
       │        └───────────────→ Trans                   │            │
       │                                                  │            │
       │                        ┌──────────┐              │            │
       │                        │  Daemon  │  VM/DEMO/UI/SENDER.*     │
       │                        │ (DS01_P) │──────────────┘            │
       │                        └────┬─────┘                           │
       │                             │                                 │
       │                             │ VM/DEMO/DAEMON/LISTENER         │
       │                             └───────────────→ Trans           │
       │                                                               │
```

### 5.2 Daemon Awake 잡 메시지 흐름

```
Daemon (DS01_P)                   RabbitMQ                    Trans (TS01_P)
     │                               │                            │
     │  TsAgentSender                │                            │
     │──SCHEDULE-QUEUEJOB──────────→│  VM/DEMO/DAEMON/LISTENER  │
     │                               │──────────────────────────→│ DaemonListener
     │  (20초 주기)                   │                            │ → 워크플로우 실행
     │                               │                            │
     │  UiAgentSender                │                            │
     │──(UI 갱신 메시지)────────────→│  VM/DEMO/UI/SENDER.*      │
     │                               │──────────────────────────→│ UI 전체 (MULTICAST)
```

### 5.3 Control HeartBeat RPC 흐름

```
Control (CS01_P)                 RabbitMQ                   대상 프로세스
     │                               │                            │
     │  HeartBeatRpcSender           │                            │
     │  (RPCCLIENT)                  │                            │
     │──CONTROL-HEARTBEAT──────────→│ VM/DEMO/CONTROL/AGENT/TS01_P│
     │                               │──────────────────────────→│ ApplicationControl
     │                               │                            │   AgentListener
     │                               │  (ReplyTo + CorrelationId) │   (RPCSERVER)
     │←─────── HeartBeat 응답 ──────│←───────────────────────────│
     │                               │                            │
     │  (timeout 5초 내 응답 없으면 HANG 판정)                      │
```

## 6. appsettings.json Destination 설정 구조

```json
{
  "Destination": {
    "DomainValue": "VM/DEMO",
    "Server": {
      "Domain": {
        "ConnectUrl": "localhost",
        "Username": "guest",
        "Password": "guest",
        "StationMode": "INTER"
      },
      "Ts": {
        "Host":   { "Listener": { "Destination": "..." }, "Sender": { "Destination": "..." } },
        "Es":     { "Listener": { "Destination": "..." }, "Sender": { "Destination": "..." } },
        "Ui":     { "Listener": { "Destination": "..." }, "Sender": { "Destination": "..." } },
        "Daemon": { "Listener": { "Destination": "..." } },
        "Control":{ "Listener": { "Destination": "..." } }
      },
      "Daemon":  { "Control": {...}, "Ts": { "Sender": {...} }, "Ui": { "Sender": {...} } },
      "Control": { "Control": {...}, "Ts": { "Sender": {...} }, "Ui": { "Sender": {...} }, "All": {...} },
      "Es":      { "Control": {...}, "Ts": { "Listener": {...}, "Sender": {...} } }
    }
  }
}
```

**설정 키 → Queue 매핑 (플레이스홀더 치환 후):**

| 설정 키 (소문자 평탄화) | 해석 결과 |
|------------------------|-----------|
| `server.ts.host.listener.destination` | `/BN1/BNE3/ACS/FACTORY1/ACSmgr_2F` |
| `server.ts.host.sender.destination` | `/BN1/BNE3/MOS/CLIENT/MHSmgr` |
| `server.ts.es.listener.destination` | `VM/DEMO/ES/LISTENER` |
| `server.ts.es.sender.destination` | `VM/DEMO/ES/SENDER` |
| `server.ts.ui.listener.destination` | `VM/DEMO/UI/LISTENER` |
| `server.ts.ui.sender.destination` | `VM/DEMO/UI/SENDER` |
| `server.ts.daemon.listener.destination` | `VM/DEMO/DAEMON/LISTENER` |
| `server.ts.control.listener.destination` | `VM/DEMO/CONTROL/AGENT/TS01_P` |
| `server.control.ts.sender.destination` | `VM/DEMO/CONTROL/AGENT/LISTENER` |
| `server.daemon.ts.sender.destination` | `VM/DEMO/DAEMON/LISTENER` |
| `server.daemon.ui.sender.destination` | `VM/DEMO/UI/SENDER.*` |
| `server.daemon.control.listener.destination` | `VM/DEMO/CONTROL/AGENT/DS01_P` |
| `server.control.control.listener.destination` | `VM/DEMO/CONTROL/AGENT/CS01_P` |
| `server.control.ui.sender.destination` | `VM/DEMO/UI/SENDER` |
| `server.es.ts.listener.destination` | `VM/DEMO/ES/ES01_P` |
| `server.es.ts.sender.destination` | `VM/DEMO/ES/LISTENER` |
| `server.es.control.listener.destination` | `VM/DEMO/CONTROL/AGENT/ES01_P` |

## 7. 구현 파일 참조

| 파일 | 역할 |
|------|------|
| `ACS.App/Modules/MsbRabbitMQModule.cs` | 프로세스 타입별 RabbitMQ 리스너/센더 DI 등록 |
| `ACS.Communication/Msb/RabbitMQ/GenericRabbitMQSender.cs` | `IMessageAgent` 구현 — UNICAST/MULTICAST/RPC 송신 |
| `ACS.Communication/Msb/RabbitMQ/GenericWorkflowRabbitMQListener.cs` | 워크플로우 메시지 수신 리스너 |
| `ACS.Communication/Msb/RabbitMQ/ApplicationControlAgentRabbitMQListener.cs` | Control HeartBeat RPC 서버 리스너 |
| `ACS.Communication/Msb/RabbitMQ/AbstractRabbitMQListener.cs` | 리스너 베이스 클래스 (연결 관리, QoS) |
| `ACS.Communication/Msb/RabbitMQ/AbstractRabbitMQ.cs` | RabbitMQ 연결 팩토리 베이스 클래스 |
| `ACS.App/Scheduling/Awake/*.cs` | Daemon Awake 스케줄 잡 (6개) |
