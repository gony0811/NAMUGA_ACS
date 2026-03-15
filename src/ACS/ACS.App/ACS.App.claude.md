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
- `control` → ControlModule
- `host` → HostModule
- `ui` → UiModule (읽기 전용 매니저 + HttpCommServer)
- `query`, `report` → TransModule

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

`ui` 프로세스 타입으로 실행 시 포트 5100에서 REST API 제공:
- GET /api/vehicles, /api/nodes, /api/links, /api/commands
- 핸들러: `ACS.Communication/Http/Handlers/ApiRequestHandler.cs`
