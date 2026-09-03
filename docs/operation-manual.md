# ACS 운영 매뉴얼 — 기동 절차 & 에러 확인

운영/장비 PC 재부팅 후 시스템을 올리는 절차와, UI에서 AMR 에러(알람)를 확인하는 방법을 정리한 운영자용 문서.

---

## 1. 시스템 구성 요약

### 인프라 (Docker 컨테이너 2개)

| 컨테이너 | 역할 | 포트 | 계정 |
|---|---|---|---|
| `amr-rabbitmq-mqtt-broker` | RabbitMQ (프로세스 간 메시지) + MQTT (AMR 통신) | 5672 AMQP / 1883 MQTT / **15672 관리 웹** | guest / guest |
| `acs-postgres-db` | PostgreSQL 17 (`acsdb`) | 5432 | postgres / 1234 |

두 컨테이너 모두 `restart: always` — **Docker 엔진만 뜨면 자동으로 함께 기동**된다.

### ACS 프로세스 (사이트별 실행)

| 사이트 | Process:Type | 역할 | API 포트 | 기동 필수 여부 |
|---|---|---|---|---|
| TS01_P | trans | 반송/배차 로직 | 5103 | **필수** |
| ES01_P | ei | AMR MQTT 인터페이스 | 5104 | **필수** |
| CS01_P | control | 컨트롤 + **UI 백엔드**(REST/SignalR/릴리스 피드) | **5100** | **필수** |
| HS01_P | host | MES(호스트) 통신 | 5101 | MES 연동 시 |
| DS01_P | daemon | 데몬 잡 | 5105 | 사이트에 따라 |

- 실행 파일 위치(배포 PC): `src/ACS/deploy/<사이트>/<사이트>.exe` (예: `deploy/CS01_P/CS01_P.exe`)
- 로그 위치: `deploy/<사이트>/logs/<사이트>-YYYYMMDD.log`
- 설정: 공통 1부 `deploy/appsettings.common.json`(DB/브로커 주소) + 사이트별 `deploy/<사이트>/appsettings.json`(포트/타입)

### UI (ACS.UI 데스크탑)

- 운영 PC: 설치형 **AcsUi** (시작 메뉴). 최초 설치는 `http://<CS서버>:5100/releases/ui` 에서 `AcsUi-win-Setup.zip` 다운로드 → 압축 해제 → Setup 실행 (상세: `docs/deploy-ui.md`)
- CS(5100)에 접속해 동작하며, 4시간 주기 자동 업데이트 (업데이트 준비 시 상단 배너 → "지금 재시작")

---

## 2. PC 재부팅 후 기동 절차

### 순서 요약

```
① Docker Desktop 기동 → 컨테이너 2개 자동 Up 확인
② ACS 프로세스 기동 (TS01_P → ES01_P → CS01_P [→ HS01_P])
③ UI(AcsUi) 실행 → 로그인
④ 기동 확인 체크리스트
```

### ① Docker 기동

1. **Docker Desktop 실행** (권장: Settings → General → *Start Docker Desktop when you sign in* 체크 — 재부팅 시 자동 실행됨)
2. 컨테이너는 `restart: always`라 엔진이 뜨면 자동 시작된다. 확인:

```bash
docker ps
```

`amr-rabbitmq-mqtt-broker`, `acs-postgres-db` 두 개가 `Up` 상태여야 한다.

3. 컨테이너가 없거나 내려가 있으면 (최초 설치 포함) 저장소의 `docker/` 폴더에서:

```bash
docker compose up -d
```

> 최초 기동 시에만 `docker/init/01_init_acsdb.sql`로 스키마/시드가 자동 생성된다. 이미 데이터가 있는 볼륨(pgdata)에는 재실행해도 영향 없다.

4. (선택) 브로커 확인: 브라우저에서 `http://localhost:15672` (guest/guest) 접속되면 정상.

### ② ACS 프로세스 기동

Docker가 뜬 **후에** 각 사이트 exe를 실행한다:

```
src\ACS\deploy\TS01_P\TS01_P.exe
src\ACS\deploy\ES01_P\ES01_P.exe
src\ACS\deploy\CS01_P\CS01_P.exe
(MES 연동 시) src\ACS\deploy\HS01_P\HS01_P.exe
```

- 권장 순서는 TS → ES → CS 이지만 엄격하지 않다 — 각 프로세스는 브로커/DB에 재접속을 시도한다. 단 **Docker보다 먼저 실행하면 접속 실패 로그가 쌓이므로 Docker 확인 후 실행**할 것.
- 개발 PC에서 소스로 실행할 때는 `src/ACS/run-all.sh` (기본: TS01_P, ES01_P, CS01_P 빌드+실행).
- 프로세스별 창(콘솔)이 뜨며, Task Manager에서 `TS01_P.exe` 등 사이트명으로 식별된다.

### ③ UI 실행

- 시작 메뉴에서 **AcsUi** 실행 → 로그인 (권한: Admin / Operator / Viewer)
- CS(5100)가 떠 있어야 로그인/데이터 조회가 된다. 백엔드 재시작 시 UI가 401을 감지하면 재로그인을 유도한다.

### ④ 기동 확인 체크리스트

| # | 확인 항목 | 정상 기준 |
|---|---|---|
| 1 | `docker ps` | 컨테이너 2개 `Up` |
| 2 | 각 프로세스 콘솔/로그 (`deploy/<사이트>/logs/`) | 접속 실패·예외 반복 없음 |
| 3 | UI 로그인 | MainWindow 진입 |
| 4 | UI 우측 **Summary** 패널 | 서버 연결 상태 정상, 차량 수 표시 |
| 5 | 맵 화면 | AMR 위치·배터리 표시, 차량 색상이 회색(끊김)이 아님 |
| 6 | Data View → Vehicle | `connectionState = CONNECT` |
| 7 | (MES 연동 시) HS 로그 | MES 접속/HEARTBEAT 정상 |

> AMR 연결 판정: AMR이 MQTT로 아무 메시지든 보내면 heartbeat로 간주, **30초 무수신 시 DISCONNECT** 처리(10초 주기 체크). 재접속되면 자동으로 CONNECT 복귀.

---

## 3. UI에서 에러(알람) 확인 방법

AMR이 status 메시지에 `error.code ≠ 0`을 보내면 ACS가 해당 차량을 **ALARM 상태**로 전환하고 UI에 실시간 반영한다. 해소(`error.code = 0`)되면 자동으로 NOALARM으로 돌아간다.

### 3.1 맵 (메인 화면) — 1차 확인

알람이 발생한 차량은 맵에서 즉시 눈에 띈다:

- **빨간 테두리 링** (상시) + **깜빡이는 빨간 글로우** (0.5초 주기) + 차량 우상단 **`!` 배지**
- 차량에 **마우스를 올리면** 상태 팝업이 뜨고, 알람 중이면 다음 행이 빨간색으로 표시된다:
  - `Alarm` — ALARM / NOALARM
  - `Alarm Code` — AMR이 보낸 에러 코드 (아래 3.5 코드표 참조)
  - `Alarm Reason` — AMR이 보낸 에러 메시지(사유)
  - `Alarm Time` — 발생 시각

> **유의**: Alarm Code/Reason은 알람 발생 "순간"의 실시간 이벤트로 전달된다. **알람이 이미 떠 있는 상태에서 UI를 새로 켜면** Alarm 행은 ALARM으로 보이지만 코드/사유는 `-`로 나온다. 이때는 ES01_P/TS01_P 로그에서 `RAIL-VEHICLEALARM` 항목을 확인할 것.

### 3.2 맵 차량 색상 (알람 외 상태)

| 색 | 의미 |
|---|---|
| 파랑 | IDLE (대기) |
| 초록 | RUN (작업 중) |
| 노랑 | CHARGE / MANUAL |
| 빨강 | DOWN |
| 회색 | **DISCONNECT (통신 끊김)** — 알람과 별개. heartbeat 30초 무수신 |

배터리 바(차량 아래)는 70%↑ 초록 / 30~70% 노랑 / 30%↓ 빨강.

### 3.3 Dashboard 탭

- 차량 집계: Online / Offline / Idle / Working / Charging / **Down·Warning(알람성 상태) 카운트**
- Warning 카운트가 0이 아니면 맵/Vehicle 목록에서 해당 차량을 찾아 확인한다.

### 3.4 Data View · History · Log 탭

- **Data View → Vehicle**: 전 차량 목록 — `alarmState`, `connectionState`, `processingState` 등 컬럼으로 일괄 확인. EXCHANGE 사용 시 행 선택 상세에서 슬롯(slot1~4) 점유도 확인 가능.
- **History**: 차량 상태 변화 이력(AlarmState 포함) 조회 — 언제 알람이 걸리고 풀렸는지 추적.
- **Log**: 서버 로그 조회. 파일로 직접 볼 때는 `deploy/<사이트>/logs/<사이트>-YYYYMMDD.log` (알람 경로는 ES01_P·TS01_P, UI 전달은 CS01_P).

### 3.5 AMR 에러 코드표 (요약)

상세 조건은 `docs/vehicle_alarm.md` 참조. 전부 Critical 등급.

| 코드 | 이름 | 주요 조건 |
|---|---|---|
| ERR-100 | Cobot Not Ready | Modbus 끊김 / Cobot Disable / 메인 프로그램 정지 / **수동(Manual) 모드** |
| ERR-101 | AMR Not Ready | Modbus 끊김 |
| ERR-102 | Camera Not Ready | 카메라 연결 끊김 |
| ERR-103 | Cobot Collision Error | 협동로봇 충돌 감지 — AMR 주변 장애물 확인 |
| ERR-104 | AMR Map Matching Error | 맵 매칭율 30% 미만 — 주변 환경/랜드마크 확인 |
| ERR-105 | Magazine Unloaded Manually | 반송 중 포트 센서 On→Off (수동으로 매거진 제거됨) |

### 3.6 알람과 통신 끊김의 구분

| 구분 | 알람 (ALARM) | 통신 끊김 (DISCONNECT) |
|---|---|---|
| 원인 | AMR이 스스로 보고한 에러 (`error.code ≠ 0`) | MQTT 메시지 30초 무수신 |
| 맵 표시 | 빨간 링 + 글로우 + `!` 배지 | 차량 회색 |
| 해소 | AMR이 error.code=0 보고 시 자동 | 메시지 재수신 시 자동 (최대 약 40초 지연) |
| 배차 영향 | 상태에 따름 | DISCONNECT 차량에는 이동 명령 미발행 |

---

## 4. 자주 발생하는 문제

| 증상 | 원인 / 조치 |
|---|---|
| UI 로그인 실패, `http://<CS>:5100` 404 | 다른 프로그램(예: HD.Acs.App)이 5100 포트 선점 — 해당 프로그램 종료 후 CS01_P 재시작, 또는 CS 서버 IP로 직접 접속 |
| 프로세스 기동 직후 브로커/DB 접속 오류 반복 | Docker가 아직 안 뜸 — `docker ps` 확인 후 프로세스 재시작 |
| AMR에 moveCmd 보낼 때마다 REJECTED(resultCode=11) | AMR이 이전 명령 sequence를 점유 중 — 해당 jobId로 `cancelCmd` 발행하여 해제 (RabbitMQ 관리 웹에서 `amr.<ID>.command`로 발행 가능) |
| TS 기동 시 42P01(테이블 없음) 오류 | DB 덤프 복원 후 `NA_H_*` 이력 테이블 10개 누락 — `docs/memory.md` 45번 절차로 수동 생성 |
| 차량이 계속 회색(DISCONNECT) | AMR 전원/Wi-Fi/MQTT 브로커(1883) 연결 확인. RabbitMQ 관리 웹(15672)에서 MQTT 클라이언트 접속 여부 확인 |
| UI가 옛 버전 | 4시간 주기 자동 업데이트 대기 또는 UI 재시작. 릴리스 피드: `http://<CS>:5100/releases/ui` |

---

*작성: 2026-09-03. 관련 문서: `docs/deploy-ui.md`(UI 배포), `docs/vehicle_alarm.md`(에러 코드 상세), `docs/mqtt_interface.md`(AMR 통신 규약), `src/ACS/config-templates/README.md`(설정 구조).*
