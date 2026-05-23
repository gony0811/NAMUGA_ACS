
# ACS.App

메인 콘솔 애플리케이션. 시스템의 진입점이자 DI 컨테이너 구성을 담당한다.

## 실행

```bash
dotnet run --project ACS.App/ACS.App.csproj
```

## Executor 패턴

`Executor.cs`가 중앙 오케스트레이터 역할을 한다:
1. `appsettings.json`에서 설정 로드 (ConfigurationBuilder)
2. Autofac 컨테이너 빌드 — 프로세스/사이트별 모듈 등록
3. DB 스키마 초기화 (`EnsureCreated()`)
4. Quartz 스케줄러 시작
5. BackgroundService(IHostedService) 시작

## 모듈 시스템 (`Modules/`)

프로세스 타입(`Acs:Process:Type`)에 따라 다른 Autofac 모듈 등록:
- `trans` → TransModule
- `ei` → EiModule
- `daemon` → DaemonModule
- `control` → ControlModule (UI 백엔드(REST API + SignalR) 호스팅 겸함)
- `host` → HostModule
- `query`, `report` → TransModule

> `ui` 프로세스 타입은 폐지됨. UI 백엔드는 `control` 프로세스가 겸한다(아래 "실행 호스트" 참조).
> 이전 UiModule이 등록하던 CacheManager·실시간 구독자(PoseTelemetrySubscriber, HostCommSubscriber)는 ControlModule로 이전됨.

## 실행 호스트 (`Program.cs`)

프로세스 타입에 따라 두 가지 호스트로 분기:
- `control` → 웹 호스트(`RunWebHost`): ASP.NET Core(Kestrel) + SignalR + Autofac. REST/SignalR 백엔드를 호스팅하며 동시에 control 본연의 서버 관리(start/kill/heartbeat) 기능을 수행한다.
- 그 외(host/trans/ei/daemon/query/report) → 콘솔 호스트(`RunConsoleHost`).

사이트(`Acs:Site:Name`)에 따라 추가 모듈:
- `NAMUGA` → NamugaSiteModule
- `V1` → V1SiteModule
- `V2` → V2SiteModule
- `SSM1D1F` → Ssm1d1fSiteModule

## Database

PostgreSQL via EF Core. 키 파일:
- `Database/AcsDbContext.cs` — EF Core DbContext
- `Database/EfCorePersistentDao.cs` — 영속성 DAO 구현

두 개 DB 사용:
- `acsdb` — 메인 애플리케이션 데이터
- `acsdb_elsa` — Elsa 워크플로우 엔진

## 설정 (`appsettings.json`)

주요 설정 섹션:
- `Acs:Process` — 프로세스 ID, 타입, HardwareType, Msb
- `Acs:Api` — HTTP API 리스닝 (기본 포트 5100)
- `Acs:Site` — 사이트 이름
- `ConnectionStrings` — PostgreSQL 연결
- `Serilog` — 로깅 설정
- 메시지 XPath/NodeName 매핑 (Transfer, Carrier, Vehicle, Port, Zone, Alarm 등)

## HTTP API

`control` 프로세스 타입으로 실행 시 포트 5100(`Acs:Api:ListenPort`)에서 REST API + SignalR 제공:
- REST: GET /api/vehicles, /api/nodes, /api/links, /api/commands 등 — `Web/Controllers/AcsRestControllers.cs`
- SignalR: `/hubs/vehicle`(POSE 텔레메트리), `/hubs/hostcomm`(Host TCP 통신 로그) — `Web/Hubs/`, `Web/Realtime/`
