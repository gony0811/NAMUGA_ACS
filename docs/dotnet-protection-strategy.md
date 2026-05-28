# ACS 배포물(.NET) 디컴파일/변조 방어 전략

> 작성일: 2026-05-28
> 상태: 미적용 — 후속 작업으로 단계별 도입 예정
> 적용 우선순위: ACS.App(서버) · ACS.UI(데스크탑) 동등

---

## Context

ACS는 .NET 8 기반 솔루션이며 ACS.App(서버 콘솔, Exe + dll 다수)과 ACS.UI(Avalonia 데스크탑, WinExe + dll 다수)를 고객 현장에 배포한다. 현재 빌드는 **표준 IL DLL** 그대로이며 보호장치는 0이다:

- 모든 .csproj가 `PublishSingleFile / PublishTrimmed / SelfContained / PublishReadyToRun` 설정 없음
- Obfuscar / ConfuserEx / Dotfuscator / .NET Reactor 흔적 없음
- `src/ACS/ACS.App/appsettings.json` 등에 **DB 비밀번호 평문**(`Password=1234`), Host 자격증명 평문 노출
- `deploy.ps1`, `publish-deploy.ps1`이 robocopy로 그대로 복사 — 서명/난독화 단계 없음

→ ILSpy/dnSpy/dotPeek로 거의 원본 그대로 복원 가능. 핵심 비즈니스 로직(Path 탐색, 통신 핸들러, 알람 처리, 라이센스 로직 등) 노출 및 dll 변조가 자유롭다.

**막고 싶은 것**: (1) 비즈니스 로직 역공학, (2) 변조/패치, (3) 시크릿 평문 노출, (4) 불법 복제/라이센스 우회.

본 문서는 **비용 0 → 저비용 → 상용 도구** 순으로 단계화한 권장 적용안이다. 각 단계는 독립 적용 가능하므로 PoC 결과를 보고 다음 단계로 진행한다.

---

## 핵심 전제

- **완벽한 보호는 존재하지 않는다.** 충분한 시간·전문성을 가진 공격자는 어떤 .NET 보호도 깬다. 목표는 *공격 비용 vs 보호 가치*의 균형이며 "캐주얼한 디컴파일 + 일반적 변조"를 막는 게 현실적 마지노선.
- ACS는 **Reflection/DI heavy 의존성**(Autofac, Quartz, Elsa, EF Core, Avalonia XAML 바인딩)이 깊다 → 공격적인 난독화/AOT는 호환성이 깨질 위험이 큼. **각 단계마다 회귀 테스트 필수.**
- Native AOT(.NET 8 지원)는 Elsa·Avalonia XAML 동적 로딩과 충돌 가능성이 매우 높아 **현실적 후보에서 제외**.

---

## Phase A — 비용 0, 즉시 적용 (필수 baseline)

### A1. 시크릿을 코드/설정 파일 밖으로 분리

**문제**: 현재 `appsettings.json`이 그대로 배포물에 동봉되어 DB/Host 자격증명 노출.

**조치**:
- 운영 배포 시 `appsettings.json`에서 비밀 필드 제거하고 **환경변수**로 주입 — `ConnectionStrings__Default`, `Acs__Host__Password` 등. .NET Configuration이 이중 언더스코어를 자동 매핑.
- 또는 OS DPAPI로 암호화된 별도 secrets 파일(`Microsoft.AspNetCore.DataProtection` 사용, key는 머신 또는 사용자 단위로 보관 — Windows DPAPI는 별도 NuGet 불필요).
- `appsettings.Production.json`은 배포 머신에서만 관리하고 **git 추적에서 제외**.
- 운영 부서에 "appsettings 그대로 두지 말 것" 정책 문서화.

**대상 파일**: `src/ACS/ACS.App/appsettings*.json`, `publish/base/appsettings*.json`, `publish/<SITE>/appsettings*.json`, `src/ACS/publish-deploy.ps1` (secrets 제외하고 publish하도록 수정).

### A2. PDB(디버그 심볼) 제거 + Release 빌드 강화

**문제**: Debug PDB가 동봉되면 디컴파일러가 변수명·라인까지 복원.

**조치**: 솔루션 루트 `Directory.Build.props` 신규 생성 —
```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <Optimize>true</Optimize>
  <DefineConstants>$(DefineConstants);RELEASE</DefineConstants>
</PropertyGroup>
```
- `publish-deploy.ps1`이 이미 Release 빌드라면 자동 적용. 검증: publish 출력 폴더에 .pdb 파일이 없어야 함.

### A3. PublishSingleFile + 압축

**효과**: dll 묶음을 하나의 exe로 패킹 → 캐주얼 공격자가 개별 dll을 꺼내기 한 단계 어려워짐(여전히 추출 가능하지만 진입장벽 ↑). 압축으로 정적 분석 도구가 한번 더 unpack을 거쳐야 함.

**조치** (각 진입점 프로젝트 — `ACS.App.csproj`, `ACS.UI.csproj`):
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>false</SelfContained>  <!-- 현재 --no-self-contained 유지 -->
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```
- **호환성 PoC 필수**: Avalonia XAML 동적 로딩, Elsa 워크플로우 어셈블리 스캔, Autofac assembly 모듈 등록이 single-file에서 깨지지 않는지 확인.
- 깨지면 `<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>` 로 동작은 보장하되 압축 효과 감소.

---

## Phase B — 무료 난독화 (1~2일, 비용 0)

### B1. Obfuscar 도입 — 식별자 rename

**효과**: 클래스/메서드/필드/변수 이름을 a, b, c로 치환. ILSpy로 봤을 때 의미 추론 난도가 급상승. 무료 OSS, MSBuild에 NuGet 통합 가능.

**적용 범위**:
- **포함**: ACS.Core, ACS.Manager, ACS.Service, ACS.Communication, ACS.Elsa(액티비티 클래스 일부) — 비즈니스 IP 본체
- **제외 규칙(필수)**:
  - EF Core 엔티티 (`VehicleEx`, `TransportCommandEx` 등 — DB 매핑이 이름 기반) → 클래스/속성명 유지
  - Autofac 모듈/등록 인터페이스 (`IResourceManagerEx` 등) → public API 표면은 유지
  - Avalonia ViewModel public 프로퍼티 (XAML이 이름으로 바인딩) → ACS.UI는 `[ObservableProperty]` 자동 생성 이름이 바인딩 대상이므로 매우 신중
  - Quartz Job (`HeartBeatJob` 등 reflection 인스턴스화) → 유지
  - Elsa Activity (`ActivityDescriptor`가 타입명 사용) → 유지
  - SignalR Hub method (`VehicleUpdate` 등 클라이언트가 이름으로 호출) → 유지
  - REST 컨트롤러 클래스/액션 (라우팅이 이름 기반인 경우) → 라우트 어트리뷰트가 잡아주므로 대체로 OK이나 PoC 필요
- **문자열 암호화**: Obfuscar는 약함 → Phase D에서 다룸

**산출물**:
- `obfuscar.xml` 설정 파일을 `src/ACS/` 루트에 신규 추가
- `publish-deploy.ps1`에 obfuscar 호출 단계 삽입 (Release 빌드 후 → 난독화 → 그 후 robocopy)
- 회귀 테스트: 양 프로세스(서버/UI) 기동 후 핵심 시나리오(차량 1대 telemetry 수신, 워크플로우 1건 트리거, UI Vehicle View Reset 동작) 통과 확인

### B2. PDB·메타데이터 정리(난독화 후)

- Obfuscar 결과물에 남은 attribute(특히 `[Obfuscation]`, `[CompilerGenerated]` 외 흔적)는 다시 한번 PDB 제거 확인.

---

## Phase C — 안티변조 + 라이센스 (저비용~중간)

### C1. Authenticode 코드 사이닝 (변조 방지의 표준 수단)

**효과**: 모든 exe·dll에 디지털 서명 → 단 1바이트라도 변조되면 서명 깨짐 → 자가 검증 코드로 거부 가능. 또한 SmartScreen/AV 경고 감소 부수효과.

**비용**: 코드 사이닝 인증서 연 약 20~80만원 (Sectigo, DigiCert, GlobalSign). EV 인증서는 비싸지만 SmartScreen 즉시 신뢰.

**조치**:
- `signtool.exe` (Windows SDK 동봉) 로 publish 산출물 일괄 서명
- `publish-deploy.ps1` 끝단에 서명 단계 추가 — `*.exe`, `ACS.*.dll` 대상
- 런타임에 entry assembly의 `IsAuthenticodeSigned` 체크 (X509Certificate.CreateFromSignedFile 사용) — 변조 또는 서명 제거 시 fail-fast
- PowerShell 배포 스크립트(`*.ps1`)도 같은 인증서로 서명 가능 → 변조 방지 일관성

### C2. 라이센스 파일 (RSA 서명 기반, 무료)

**효과**: 머신 식별자 + 만료일 + 사이트명을 RSA로 서명한 라이센스 파일을 발급, 시작 시 검증. 불법 복제 1차 방어.

**도구 후보**: `Standard.Licensing` (Portable.Licensing의 활성 fork, MIT, 무료) — 라이센스 생성 CLI + 런타임 검증 API 제공. 또는 자체 구현(RSA-SHA256 + 머신 ID = `Win32_BIOS.SerialNumber` 등).

**구현 위치**: `ACS.App/Executor.cs`의 Autofac 컨테이너 빌드 직전 — 라이센스 무효 시 `Environment.Exit(LICENSE_INVALID_CODE)`. Obfuscar(B1) 적용 후라야 우회가 어려워지므로 **반드시 Phase B 이후에 도입**.

**한계**: 난독화 없으면 `if` 분기 한 줄 패치로 우회 가능 → C1 코드 사이닝 + C2 라이센스 + B1 난독화 3종 세트로 묶어야 의미 있음.

---

## Phase D — 강한 난독화 (상용/PoC, 선택)

Phase B의 Obfuscar는 rename 위주라 control-flow는 그대로 보이고 문자열도 평문이다. 더 강한 보호가 필요하면:

### D1. ConfuserEx 2 fork (무료, 메인테넌스 약함)
- Control flow obfuscation, string encryption, anti-debug, anti-dump, anti-tamper
- Avalonia/Elsa/EF Core 호환성 위험이 매우 큼 — 모듈별 protection preset을 보수적으로 골라야 함
- PoC 시간이 길어질 수 있음(수일~수주)

### D2. .NET Reactor (상용, 단일 개발자 라이센스 약 30만원/년 수준)
- 가상화 모드(IL → 자사 VM 명령어로 치환), code virtualization, hardware-locked licensing 내장
- 가장 강력한 옵션 중 하나이나 가상화는 성능 저하 + 호환성 위험 동반 — 핵심 알고리즘 클래스 몇 개에만 선택적 적용 권장
- 14일 평가판 있음 → PoC 가능

### D3. 권장
- **먼저 Phase A+B+C로 baseline 확보 후 운영하다가 실제 침해 시도 흔적이 발견되면 D 도입.** 처음부터 D에 들어가면 호환성 디버깅에 시간 소진.

---

## 권장 적용 순서 요약 (체크리스트)

| 단계 | 작업 | 비용 | 소요 | 우선순위 |
|------|-----|------|------|----------|
| A1   | Secret 환경변수/DPAPI 분리 | 0 | 0.5일 | 즉시 |
| A2   | PDB 제거, Release 옵션 | 0 | 0.5일 | 즉시 |
| A3   | PublishSingleFile + 압축 | 0 | 1일(호환 PoC 포함) | 즉시 |
| B1   | Obfuscar 적용 + 예외 규칙 정비 | 0 | 1~2일 | 1주 내 |
| C1   | 코드 사이닝 인증서 + signtool 통합 | 연 20~80만원 | 1일 | 2주 내 |
| C2   | 라이센스 파일 검증 (Standard.Licensing) | 0 | 1~2일 | C1 이후 |
| D    | ConfuserEx 또는 .NET Reactor PoC | 0 ~ 30만원/년 | 1~3주 | 침해 흔적 또는 IP 가치 재평가 후 |

---

## 변경 대상 파일 (Phase A~C에 한정)

- 신규: `src/ACS/Directory.Build.props` — Release 옵션 일괄 적용 (A2)
- 신규: `src/ACS/obfuscar.xml` — Obfuscar 설정 (B1)
- 신규: `src/ACS/sign.ps1` 또는 `publish-deploy.ps1`에 통합 — 서명 단계 (C1)
- 신규: `src/ACS/ACS.Core/Licensing/LicenseValidator.cs` — 라이센스 검증 (C2)
- 수정: `src/ACS/ACS.App/ACS.App.csproj`, `src/ACS/ACS.UI/ACS.UI.csproj` — PublishSingleFile (A3)
- 수정: `src/ACS/ACS.App/appsettings*.json`, `publish/base/appsettings*.json`, `publish/<SITE>/appsettings*.json` — 시크릿 제거 (A1)
- 수정: `src/ACS/publish-deploy.ps1`, `src/ACS/deploy.ps1` — 난독화·서명 단계 삽입, appsettings 시크릿 동봉 금지 (A1, B1, C1)
- 수정: `src/ACS/ACS.App/Executor.cs` — 라이센스 검증 호출 지점 (C2)
- 신규(권장): `docs/deployment-secrets.md` — 현장 운영자용 시크릿 주입 절차

## 검증 방법

각 Phase 적용 후 반드시 다음 회귀 시나리오를 통과시킨 다음 진행:

1. **빌드**: `dotnet build src/ACS/ACS.sln -c Release` 통과, PDB 미생성 확인
2. **Publish**: `publish-deploy.ps1` 실행 후 산출물에 .pdb 없음, appsettings에 평문 비밀번호 없음 확인
3. **서버 기동**: ACS.App publish 산출물을 별도 폴더에서 실행 → DB 연결 (환경변수 주입), MQTT/RabbitMQ 연결, Elsa 워크플로우 1건 트리거
4. **UI 기동**: ACS.UI publish 산출물 실행 → 서버 SignalR 연결, Vehicle View 1Hz 갱신, 직전 작업의 "초기화" 버튼 정상 동작
5. **역공학 체감 테스트**: ILSpy로 publish 산출물을 열어 (a) Phase A 후 — DB password가 안 보이는지, (b) Phase B 후 — 클래스/메서드명이 rename되었고 EF/DI 예외 규칙은 유지되는지 시각 확인
6. **변조 테스트(Phase C 이후)**: publish 산출물에서 ACS.Core.dll의 1바이트를 hex editor로 변조 후 기동 시 서명 검증 실패로 거부되는지 확인
7. **라이센스 우회 테스트(Phase C2 이후)**: 만료된 라이센스 파일, 다른 머신 라이센스 파일, 라이센스 파일 삭제 — 3가지 모두에서 기동 거부 확인

---

## 후속 작업 진입점

- **즉시 시작 가능**: Phase A1 (시크릿 분리) — 외부 의존성 0, 가장 큰 위험(평문 DB 비밀번호) 해결
- **다음 스프린트**: Phase A2/A3 → B1 순으로 진행
- **분기점**: B1 회귀 테스트 결과에 따라 C/D 진입 여부 재평가

각 Phase는 작업 시작 시 별도 plan을 만들어 진행한다.
