# ACS 프로젝트 작업 내역 및 결정 기록

## 1. CLAUDE.md 및 프로젝트 문서 구조 수립

**날짜:** 2026-03-16
**작업:** `/init` 명령으로 CLAUDE.md 생성 후 문서 구조 재편

**결정 사항:**
- CLAUDE.md는 `/Users/sean/Documents/GitHub/NAMUGA_ACS/CLAUDE.md` (레포 루트)에 배치
- CLAUDE.md에는 빌드 명령, 아키텍처 개요, 핵심 패턴만 기록 (간결하게 유지)
- 각 프로젝트별 세부 문서는 해당 프로젝트 폴더 내 `*.claude.md` 파일로 분리

**생성된 프로젝트별 문서:**
| 파일 | 내용 |
|------|------|
| `src/ACS/ACS.App/ACS.App.claude.md` | Executor 패턴, 모듈 시스템, DB, 설정 |
| `src/ACS/ACS.Core/ACS.Core.claude.md` | Core 라이브러리 구조, 의존성 규칙 |
| `src/ACS/ACS.Communication/ACS.Communication.claude.md` | 프로토콜 구현 |
| `src/ACS/ACS.Manager/ACS.Manager.claude.md` | 비즈니스 로직 매니저 |
| `src/ACS/ACS.Elsa/ACS.Elsa.claude.md` | Elsa 워크플로우 + Studio 통합 |
| `src/ACS/ACS.UI/ACS.UI.claude.md` | Avalonia MVVM 데스크탑 앱 |

---

## 2. ACS.UI 테마 변경 (Dark → Light)

**날짜:** 2026-03-16
**작업:** ACS.StartUp (Razor 프로젝트)의 디자인을 참고하여 ACS.UI를 라이트 테마로 전환

**결정 사항:**
- Avalonia FluentTheme `RequestedThemeVariant="Light"` 적용
- ACS.StartUp의 색상 팔레트를 Application.Resources로 정의
- MapCanvas 배경색을 라이트 톤(`#F5F7FA`)으로 변경
- StateToColorConverter 기본값을 `Brushes.White` → `Brushes.Gray`로 변경

**정의된 색상 팔레트 (App.axaml):**
| 키 | 색상 | 용도 |
|----|------|------|
| AcsHeaderColor | `#1a3a5c` | 타이틀 바 배경 |
| AcsTabBarColor | `#e8edf2` | 탭 바/상태 바 배경 |
| AcsActiveTabColor | `#1565c0` | 활성 탭 악센트 |
| AcsPrimaryBlueColor | `#0d47a1` | 주요 텍스트 강조 |
| AcsDashGradientStart | `#dce8f4` | 대시보드 그라데이션 시작 |
| AcsDashGradientEnd | `#c4d6ea` | 대시보드 그라데이션 끝 |
| 기타 | 14개 Color + 14개 SolidColorBrush | 텍스트, 경고, 알람, 섹션 등 |

---

## 3. ACS.UI 레이아웃 재구성 (매뉴얼 기반)

**날짜:** 2026-03-16
**작업:** ACS 사용자 매뉴얼(ACS_사용자매뉴얼_ACSGUI_170906.pdf)의 기본 Layout을 참고하여 전체 구조 재편

**이전 구조:**
- 탭 전환 시 메인 콘텐츠 영역 전체가 교체됨
- Summary View 없음

**변경 후 구조 (매뉴얼 기준):**
```
┌────────────────────────────────────────────────────┐
│ ① Title Bar (#1a3a5c) "ACS GUI"                    │
├────────────────────────────────────────────────────┤
│ ② Tab Bar: Dashboard | User | Basic Control |      │
│            Data View | History                     │
├────────────────────────────────────────────────────┤
│ ③ Ribbon Content (탭별 변경 영역)                    │
│   Dashboard → DashboardView (게이지/통계)            │
│   기타 탭 → 플레이스홀더 (향후 서브메뉴 추가)           │
├───────────┬────────────────────────────────────────┤
│ ④ Summary │ Map View (메인)                         │
│    View   │                                        │
│  (250px)  │ MapCanvas (pan/zoom)                   │
│           │                                        │
│ Site Info │                                        │
│ Vehicle   │                                        │
│ Transfer  │                                        │
│ Link Info │                                        │
├───────────┴────────────────────────────────────────┤
│ ⑤ Status Bar (연결 상태 + 마지막 업데이트)             │
└────────────────────────────────────────────────────┘
```

**핵심 설계 원칙:**
- 탭 전환 시 리본바의 서브메뉴만 변경됨
- SummaryView(좌)와 MapView(우)는 **항상 표시**
- GridSplitter로 좌/우 패널 크기 조절 가능

**변경된 파일:**

### MainWindow.axaml
- `Grid RowDefinitions="Auto,Auto,Auto,*,Auto"` — 5행 구조
- Row 0: Title Bar
- Row 1: RadioButton 기반 탭 네비게이션 (GroupName="Tabs")
- Row 2: Panel + IsVisible 바인딩으로 탭별 리본 콘텐츠 전환
- Row 3: `ColumnDefinitions="250,Auto,*"` — SummaryView | GridSplitter | MapView
- Row 4: Status Bar (연결 Ellipse + 텍스트)
- DashboardView의 IsVisible은 `$parent[Window].((vm:MainWindowViewModel)DataContext).IsTab0Selected` 패턴 사용

### SummaryView.axaml (신규)
- 좌측 250px 패널, ScrollViewer로 세로 스크롤
- 섹션: Summary 헤더, Site Info, Vehicle Info, Transfer Info, Link Info, Mini Map 플레이스홀더
- 각 섹션은 Border + Grid로 라벨/값 쌍 표시

### SummaryViewModel.cs (신규)
- Site Info: SiteName, ServerVersion, ClientVersion, ConnectionState
- Vehicle Info: Total, Working, Idle, Online, Offline, Charging, Down
- Transfer Info: Active, Queued, Completed, Total
- Link Info: Total, Disabled
- 메서드: `UpdateFromVehicles()`, `UpdateFromLinks()`, `UpdateFromCommands()`, `UpdateConnectionState()`

### MainWindowViewModel.cs
- `SummaryViewModel` 프로퍼티 추가
- 5개 탭 불리언 프로퍼티 (`IsTab0Selected` ~ `IsTab4Selected`)
- `StartPollingAsync()` → `LoadStaticDataAsync()` + `PollDynamicDataAsync()`
- 폴링 데이터를 MapViewModel, VehicleListViewModel, DashboardViewModel, SummaryViewModel에 분배

### DashboardView.axaml
- 리본 영역 크기에 맞게 `MaxHeight="160"` 추가
- 기존 게이지/통계 콘텐츠 유지 (System, Transfer, Vehicle, Layout, Map 섹션)

---

## 4. 해결된 기술 이슈

### Avalonia BoolConverters.ToDouble 미존재
- **문제:** DashboardView에서 알람 램프 투명도 제어에 `BoolConverters.ToDouble` 사용 시도 → Avalonia에 해당 컨버터 없음
- **해결:** 두 개의 Border를 `IsVisible="{Binding IsAlarmActive}"` / `IsVisible="{Binding !IsAlarmActive}"`로 분리하여 각각 다른 Opacity 적용

### IsVisible 바인딩이 자식 DataContext로 해석되는 문제
- **문제:** DashboardView, MapView 등에 DataContext가 설정된 경우, `IsVisible="{Binding IsTab0Selected}"`가 자식 VM에서 프로퍼티를 찾음
- **해결:** `$parent[Window].((vm:MainWindowViewModel)DataContext).IsTab0Selected` 패턴으로 부모 Window의 DataContext에 접근

### Git pathspec 오류
- **문제:** `src/ACS/` 디렉토리에서 `git add CLAUDE.md` 실행 시 파일을 찾지 못함
- **해결:** 상대 경로 `../../CLAUDE.md` 사용

---

## 5. 리본 바 탭 스타일 변경 + 탭 추가

**날짜:** 2026-03-16
**작업:** 탭 디자인을 RadioButton 밑줄 스타일에서 리본 바(Ribbon Bar) 스타일로 변경, 추가 탭 4개 생성

**변경 내용:**
- 탭 스타일: 선택 탭 = 흰색 배경 + 상단/좌우 테두리 + 하단 없음 (콘텐츠 영역과 시각적 병합), `CornerRadius="3,3,0,0"`
- 미선택 탭: 투명 배경, hover 시 `#18000000`
- 콘텐츠 영역: `BorderThickness="0,1,0,0"` 상단 테두리로 탭과 연결
- 탭 바와 콘텐츠가 하나의 `Border > Grid` 안에 통합

**탭 목록 (5개 → 9개):**
1. Dashboard (IsTab0Selected) — DashboardView 연결
2. User (IsTab1Selected) — 플레이스홀더
3. Basic Control (IsTab2Selected) — 플레이스홀더
4. Data View (IsTab3Selected) — 플레이스홀더
5. History (IsTab4Selected) — 플레이스홀더
6. Log (IsTab5Selected) — 플레이스홀더 **(신규)**
7. Application (IsTab6Selected) — 플레이스홀더 **(신규)**
8. Layout (IsTab7Selected) — 플레이스홀더 **(신규)**
9. Preference (IsTab8Selected) — 플레이스홀더 **(신규)**

**변경된 파일:**
- `MainWindow.axaml` — 탭 스타일 + 구조 전면 변경, 추가 탭 콘텐츠
- `MainWindowViewModel.cs` — `IsTab5Selected` ~ `IsTab8Selected` 프로퍼티 추가

---

## 6. Data View 탭 리본 UI 구현

**날짜:** 2026-03-16
**작업:** Data View 탭 플레이스홀더를 매뉴얼 기반 리본 UI로 교체

**리본 구성 (5개 카테고리 + 드롭다운 메뉴):**
| 카테고리 | 메뉴 항목 |
|---------|----------|
| Transfer | TrCmd View |
| Layout | Node View, Station View, Port View, Link View |
| Area | Bay View, LinkZone View, Zone View |
| Device | Vehicle View, Vehicle CrossWait View, Alarm View, Alarm Spec View |
| Assign/Route | Assign View, Route View |

**생성 파일:**
- `ViewModels/DataViewViewModel.cs` — SelectedMenu/SelectedCategory 상태 + SelectMenuCommand
- `Views/DataViewRibbonView.axaml` + `.cs` — 카테고리 버튼 (테이블 아이콘 + 드롭다운 MenuFlyout) + 선택 배지

**수정 파일:**
- `MainWindowViewModel.cs` — DataViewViewModel 프로퍼티 추가
- `MainWindow.axaml` — Data View 플레이스홀더 → DataViewRibbonView 교체

---

## 7. Application 탭 리본 UI 구현

**날짜:** 2026-03-16
**작업:** Application 탭 플레이스홀더를 매뉴얼 기반 리본 UI로 교체

**리본 구성:**
- **Application** 카테고리 버튼
- **NIO** 카테고리 버튼
- "Application Management" 섹션 라벨

**생성 파일:**
- `ViewModels/ApplicationViewModel.cs` — DataViewViewModel과 동일 패턴
- `Views/ApplicationRibbonView.axaml` + `.cs` — 2개 카테고리 버튼 + 섹션 라벨

**수정 파일:**
- `MainWindowViewModel.cs` — ApplicationViewModel 프로퍼티 추가
- `MainWindow.axaml` — Application 플레이스홀더 → ApplicationRibbonView 교체

---

## 8. Application Management + NIO View 구현

**날짜:** 2026-03-16
**작업:** Application 탭에서 Application/NIO 선택 시 메인 영역(MapView 자리)에 전용 뷰 표시 + 모달리스 팝업 지원

**구현 내용:**

### Application Management 화면
- 3컬럼 구성: Primary TreeView | Secondary TreeView | Properties DataGrid
- TreeView 노드에 상태 색상 Ellipse (Green/Red/Gray/Yellow — 매뉴얼 표 7.1.1.2 기반)
- Properties: 선택한 프로세스의 상세 정보를 Property/Value DataGrid로 표시
- Toolbar: Delete, Refresh, Auto Refresh, Popup 버튼

### NIO View 화면
- DataGrid: ID, NAME, INTERFACECLASSNAME, WORKFLOWMANAGERNAME, APPLICATIONNAME, PORT, REMOTEIP, MACHINENAME, STATE, DESCRIPTION, CREATETIME
- Toolbar: Table Option, Add, Refresh, Popup 버튼

### 뷰 전환 구조
- MainWindow Row 3의 MapView 영역을 Panel로 감싸서 MapView / AppManagementView / NioView 전환
- `ActiveMainView` 프로퍼티로 전환 관리 ("Map", "AppManagement", "Nio")
- Application 탭이 아닌 다른 탭 선택 시 자동으로 MapView 복귀
- ApplicationViewModel.OnViewChangeRequested 콜백으로 MainWindowViewModel에 뷰 전환 요청

### 모달리스 팝업
- AppManagementWindow, NioWindow — 독립 Window에 같은 View/ViewModel 내장
- MainWindowViewModel.OpenPopupRequested 이벤트 → MainWindow.cs에서 Window.Show() 호출
- Popup 버튼은 각 View와 ApplicationRibbonView에 배치

**생성 파일:**
| 파일 | 설명 |
|------|------|
| `Models/ProcessNodeModel.cs` | 트리뷰 노드 모델 (Name, Type, State, Children, Properties) |
| `Models/NioItemModel.cs` | NIO DataGrid 모델 |
| `Converters/ProcessStateToColorConverter.cs` | 프로세스/NIO 상태 → 색상 변환 |
| `ViewModels/AppManagementViewModel.cs` | Primary/Secondary 트리 + Properties |
| `ViewModels/NioViewModel.cs` | NIO DataGrid + CRUD 명령 |
| `Views/AppManagementView.axaml` + `.cs` | TreeView + DataGrid UI |
| `Views/NioView.axaml` + `.cs` | DataGrid UI |
| `Views/AppManagementWindow.axaml` + `.cs` | 모달리스 팝업 |
| `Views/NioWindow.axaml` + `.cs` | 모달리스 팝업 |

**수정 파일:**
| 파일 | 변경 내용 |
|------|----------|
| `MainWindowViewModel.cs` | AppManagementVM, NioVM 추가, ActiveMainView 전환, OpenPopup 이벤트 |
| `ApplicationViewModel.cs` | OnViewChangeRequested 콜백 추가 |
| `ApplicationRibbonView.axaml` + `.cs` | Popup 버튼 + Click 핸들러 |
| `MainWindow.axaml` | Row 3 Panel 전환 구조 |
| `MainWindow.axaml.cs` | OpenPopupRequested 이벤트 핸들러 |

---

## 9. Dock.Avalonia 기반 도킹 레이아웃 마이그레이션

**날짜:** 2026-03-16
**작업:** Panel 기반 뷰 전환(MapView/AppManagementView/NioView)을 Dock.Avalonia 도킹 프레임워크로 교체

**설계:**
- Summary → 좌측 ToolDock 패널 (고정/숨김/너비 조절 가능)
- Map/AppManagement/Nio → 중앙 DocumentDock 탭 영역 (탭 전환, 분리(float) 가능)
- ProportionalDock(Horizontal)로 좌(22%)/우(*) 분할

**패키지 변경:**
| 패키지 | 버전 |
|--------|------|
| Dock.Avalonia | 11.3.11.22 |
| Dock.Avalonia.Themes.Fluent | 11.3.11.22 |
| Dock.Model.Mvvm | 11.3.11.22 |

**생성 파일:**
| 파일 | 설명 |
|------|------|
| `ViewModels/Docking/MapDocumentViewModel.cs` | Map Document (Dock.Model.Mvvm.Controls.Document 상속) |
| `ViewModels/Docking/AppManagementDocumentViewModel.cs` | AppManagement Document |
| `ViewModels/Docking/NioDocumentViewModel.cs` | NIO Document |
| `ViewModels/Docking/SummaryToolViewModel.cs` | Summary Tool (Dock.Model.Mvvm.Controls.Tool 상속) |
| `ViewModels/Docking/AcsDockFactory.cs` | Factory — 레이아웃 생성, Document 활성화, HostWindow 설정 |

**수정 파일:**
| 파일 | 변경 내용 |
|------|----------|
| `ACS.UI.csproj` | Dock 패키지 추가, Avalonia 11.3.11 유지 |
| `App.axaml` | DockFluentTheme StyleInclude + DataTemplate 등록 (MapDocumentVM→MapView 등) |
| `MainWindowViewModel.cs` | IRootDock Layout + AcsDockFactory 프로퍼티, ActiveMainView/Popup 관련 코드 제거, ActivateDockDocument() 메서드 |
| `MainWindow.axaml` | Grid.Row="3" Panel → `<dock:DockControl Layout="{Binding Layout}" />` 교체 |
| `MainWindow.axaml.cs` | OpenPopupRequested 이벤트 핸들러 제거 |
| `ApplicationRibbonView.axaml` + `.cs` | Popup 버튼 및 핸들러 제거 |
| `AppManagementView.axaml` + `.cs` | Popup 버튼 및 핸들러 제거 |
| `NioView.axaml` + `.cs` | Popup 버튼 및 핸들러 제거 |

**삭제 파일:**
| 파일 | 사유 |
|------|------|
| `Views/AppManagementWindow.axaml` + `.cs` | Dock float window로 대체 |
| `Views/NioWindow.axaml` + `.cs` | Dock float window로 대체 |

**핵심 설계 결정:**
- Composition 패턴: Document/Tool VM이 기존 비즈니스 VM을 프로퍼티로 보유 (상속 X)
- DataTemplate으로 VM→View 매핑: `DataType="docking:MapDocumentViewModel"` → `<views:MapView DataContext="{Binding MapViewModel}" />`
- DockFactory.ActivateDocument()로 프로그래밍 방식 탭 전환
- 모달리스 팝업은 Dock의 자체 float 기능으로 대체 (탭 드래그로 분리)

---

## 10. Dock 레이아웃 재구성: Summary 고정 + Document 온디맨드

**날짜:** 2026-03-16
**작업:** 사용자 피드백 기반 도킹 구조 근본 재설계

**변경 사항:**
- SummaryView를 Dock 시스템에서 제거 → MainWindow Grid 좌측 고정 배치 (250px + GridSplitter)
- AcsDockFactory 전면 단순화: Summary/ToolDock/ProportionalDock 제거, RootDock > DocumentDock 직결
- 초기 탭: Map만 표시 (AppManagement/Nio는 리본 클릭 시 온디맨드 추가)
- AppManagement/Nio: CanClose=true (탭 X로 닫기 가능, 재표시는 리본 클릭)
- Map: CanClose=false (항상 표시)
- SummaryToolViewModel.cs 삭제, App.axaml에서 해당 DataTemplate 제거
- OnDockableClosing/OnWindowClosing/ContainsCoreDockable 오버라이드 모두 제거

**수정 파일:**
| 파일 | 변경 |
|------|------|
| `Views/MainWindow.axaml` | Grid.Row="3" → SummaryView(고정) + GridSplitter + DockControl |
| `ViewModels/Docking/AcsDockFactory.cs` | 전면 재작성 — 단순 Factory |
| `ViewModels/MainWindowViewModel.cs` | DockFactory 생성자에서 Summary 파라미터 제거 |
| `App.axaml` | SummaryToolViewModel DataTemplate 삭제 |
| `ViewModels/Docking/SummaryToolViewModel.cs` | 파일 삭제 |

---

## 11. Elsa Workflows 3 통합 및 워크플로우 마이그레이션

**날짜:** 2026-03-16 ~ 17
**작업:** Elsa 3.5 워크플로우 엔진을 ACS에 통합하고, Spring.NET XML 워크플로우를 Elsa 코드 기반 워크플로우로 마이그레이션

**구현 내용:**
- `ACS.Elsa` 프로젝트: ElsaModule(Autofac↔Elsa 브릿지), AutofacContainerAccessor, ElsaMigrationConfig
- `ACS.Elsa.Studio` + `ACS.Elsa.Studio.Client`: Blazor WASM 워크플로우 디자이너 웹 UI
- ControlStartHeartBeatWorkflow, HostMoveCmdWorkflow (코드 기반)
- JSON 기반 워크플로우 로딩 (ControlStartHeartBeat.json)

---

## 12. Control Server HeartBeat 스케줄링 수정

**날짜:** 2026-03-17
**작업:** CS01_P의 HeartBeat 스케줄이 동작하지 않는 문제를 추적하여 다수의 근본 원인을 수정

**수정 내역 (발견 순서):**

### 12.1 ControlModule.cs — Init() 미호출 + Type.GetType null 덮어쓰기
- **문제:** `OnActivated`에서 `mgr.Init()` 미호출 → HeartBeatJobType 등이 null
- **문제:** `Type.GetType("..., ACS.Control")` → 존재하지 않는 어셈블리 → null로 정상값 덮어쓰기
- **수정:** `mgr.Init()` 호출 추가, 6개 `SetProtected` Job Type 설정 제거

### 12.2 HeartBeatJob.cs / SimpleHeartBeatJob.cs — DateTime.Now → UtcNow
- **문제:** PostgreSQL `timestamp with time zone`에 `DateTime.Now` (Kind=Local) 전달 시 예외
- **수정:** `DateTime.UtcNow` 사용, `EfCorePersistentDao.SetPropertyValue()`에 자동 UTC 변환 추가

### 12.3 HeartBeatJob.cs — Configuration JobDataMap 누락
- **문제:** `context.MergedJobDataMap.Get("Configuration")` → null (NullReferenceException)
- **수정:** `CreateHeartBeatJobDetail()`에서 `jobData.Put("Configuration", this.Configuration)` 추가

### 12.4 GenericRabbitMQSender.cs — ISynchronousMessageAgent 미등록
- **문제:** Sender가 `IMessageAgent`로만 등록, `ISynchronousMessageAgent`로 미등록
- **수정:** `.As<ISynchronousMessageAgent>()` 추가

### 12.5 GenericRabbitMQSender.cs — 빈 응답 XML 파싱 에러
- **문제:** `string.ReferenceEquals(replyMessage, null)` → 빈 문자열 통과 → XmlException
- **수정:** `string.IsNullOrEmpty(replyMessage)` 사용

### 12.6 MsbRabbitMQModule.cs — 중첩 JSON config 평탄화 (핵심 수정)
- **문제:** `IConfiguration.GetSection("Destination").GetChildren()` → 1차 자식만 반환 (Server→null, Host→null)
- **결과:** 모든 `dest["server.ts.xxx"]` 조회가 null → RabbitMQ 리스너/센더 destination 전부 null → 큐 미생성
- **수정:** `FlattenSection()` 재귀 메서드 추가, `server.domain` → `server.domainvalue` 하위 호환 매핑
```csharp
private void FlattenSection(IConfigurationSection section, string prefix, NameValueCollection dest)
{
    foreach (var child in section.GetChildren())
    {
        string key = string.IsNullOrEmpty(prefix) ? child.Key.ToLowerInvariant() : prefix + "." + child.Key.ToLowerInvariant();
        if (child.Value != null) dest[key] = child.Value;
        FlattenSection(child, key, dest);
    }
}
```

### 12.7 MsbRabbitMQModule.cs — CastOption 상수 불일치
- **문제:** `"RPC_SERVER"` / `"RPC_CLIENT"` 설정했으나 실제 상수는 `"RPCSERVER"` / `"RPCCLIENT"`
- **결과:** switch case 매치 안 됨 → 큐 미생성 (ApplicationControlAgentListener), RPC 미동작 (HeartBeatRpcSender)
- **수정:** `"RPCSERVER"`, `"RPCCLIENT"` 사용

### 12.8 ControlServerManagerImplement.cs — GetDestinationName 앞부분 `/` 누락
- **문제:** `DestinationNamePrefix + "/" + appName` → `"VM/DEMO/CONTROL/AGENT/HS01_P"` (앞에 `/` 없음)
- **결과:** RabbitMQ routingKey와 큐 이름(`/VM/DEMO/...`) 불일치 → 메시지 전달 안 됨
- **수정:** RabbitMQ/Highway101일 때 앞부분 `/` 추가 로직 적용

### 12.9 ApplicationInitializer.cs — UI 프로세스 StartMsb 누락
- **문제:** `TYPE_UI` 분기에 `StartMsb(executor)` 호출 없음
- **결과:** UI01_P의 ApplicationControlAgentListener.Start() 미호출 → 큐 미생성 → HeartBeat 응답 불가
- **수정:** `SetApplicationContextToApplicationControlManager()` + `StartMsb(executor)` 추가

### 12.10 AbstractRabbitMQListener.cs — OnRequest sender 캐스팅 오류
- **문제:** `IModel session = sender as IModel` → sender는 `EventingBasicConsumer`이므로 null 반환
- **결과:** NullReferenceException → 메시지 소비/응답/ACK 전부 실패 → 큐에 메시지 적체
- **수정:** `IModel session = ((EventingBasicConsumer)sender).Model`

### 최종 결과
- 3개 프로세스(CS01_P, HS01_P, UI01_P) 모두 `active` 상태 유지
- HeartBeat RPC: CS01_P → HS01_P/UI01_P 20초 간격 정상 동작
- RabbitMQ 큐 9개 정상 생성 (이전 1개 → 9개)
- DB checkTime 갱신 확인

**수정 파일 목록:**
| 파일 | 수정 |
|------|------|
| `ACS.App/Modules/ControlModule.cs` | Init() 호출, Job Type SetProtected 제거, DestinationNamePrefix 설정 |
| `ACS.App/Modules/MsbRabbitMQModule.cs` | FlattenSection, domain 호환 매핑, CastOption 수정 |
| `ACS.App/Control/Implement/ControlServerManagerImplement.cs` | GetDestinationName `/` 추가, Configuration JobDataMap |
| `ACS.App/Control/Scheduling/HeartBeatJob.cs` | DateTime.UtcNow, Configuration 사용 |
| `ACS.App/Control/Scheduling/SimpleHeartBeatJob.cs` | DateTime.UtcNow |
| `ACS.App/Database/EfCorePersistentDao.cs` | SetPropertyValue UTC 자동 변환 |
| `ACS.App/ApplicationInitializer.cs` | TYPE_UI에 StartMsb 추가 |
| `ACS.Communication/Msb/RabbitMQ/AbstractRabbitMQListener.cs` | OnRequest sender 캐스팅 수정 |
| `ACS.Communication/Msb/RabbitMQ/GenericRabbitMQSender.cs` | IsNullOrEmpty, ISynchronousMessageAgent |

---

## 13. run-all.sh 수정

**날짜:** 2026-03-17
**작업:** `< /dev/null` stdin redirect 추가 (nohup 백그라운드 프로세스 안정성)

**알려진 이슈:** `PROCESSES` 배열에 CS01_P 누락 — 사용자가 수정 거부하여 수동 시작 필요

---

## 14. ChargeJob 완료 시 TC 정리 훅 추가

**날짜:** 2026-05-21
**작업:** `RailVehicleUpdateWorkflow.cs` 의 BatteryRate≥30 (CHARGE→IDLE) 블록을 확장하여 CHARGEMOVE TC 정리 누락 버그 수정.

**문제:**
- ChargeJob (JOBTYPE=CHARGEMOVE) 의 충전 완료 처리 시 `NA_T_TRANSPORTCMD` row 가 삭제되지 않고, `NA_R_VEHICLE.transportCommandId` 도 비워지지 않음.
- 결과: 1회 충전 후 그 차량은 영원히 후속 Job 못 받음 (`FindSuitableVehicleActivity` 의 `GetTransportCommandByVehicleId != null` 체크에 걸림).
- 레거시 `TransferServiceEx.DeleteChargeTransportCommandsByVehicle` (ACS.Service:491-513) 가 동일 로직 구현해 둔 채로 호출처 0건이었음.

**변경:** `src/ACS/ACS.Elsa/Workflows/Trans/RailVehicleUpdateWorkflow.cs`
- using 추가: `ACS.Core.History`, `ACS.Core.Transfer`, `ACS.Core.Transfer.Model`
- BatteryRate≥30 분기 안에서 ProcessingState 전이 **전에** 다음 4단계 수행:
  1. `IHistoryManagerEx.CreateTransportCommandHistory(tc, "", STATE_CHARGE_COMPLETED)` — `NA_H_TRANSPORTCMDHISTORY` 이관
  2. `ITransferManagerEx.DeleteTransportCommand(tc)` — `NA_T_TRANSPORTCMD` 삭제
  3. `IResourceManagerEx.UpdateVehicleTransportCommandId/UpdateVehicleAcsDestNodeId/UpdateVehicle(Path)` — Vehicle 측 FK + 잔여 필드 클리어
  4. 기존 `ProcessingState CHARGE → IDLE` 전이

**Why:** 사용자가 ChargeJob 동작 확인 중 발견. 충전 완료 후 차량이 후속 Job을 못 받는 잠재 deadlock 위험 제거.

**검증:** `dotnet build ACS.Elsa.csproj` 성공 (0 오류, 기존 NU1603 경고만). 실제 시나리오 테스트는 AMR Simulator + DB 직접 조회로 (1) `NA_T_TRANSPORTCMD` CHARGEMOVE 사라짐, (2) `NA_H_TRANSPORTCMDHISTORY` 에 cause=`CHARGECOMPLETED` 이력 1행, (3) `NA_R_VEHICLE` TransportCommandId/Path/AcsDestNodeId 빈 문자열, ProcessingState=IDLE 확인 필요.

---

## 15. RecoverStuckVehiclesActivity 에 CHARGEMOVE 재전송 분기 추가

**날짜:** 2026-05-21
**작업:** `RecoverStuckVehiclesActivity` (`src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs`) 의 vehicle 루프에 ChargeJob 전용 stuck 복구 분기 추가.

**문제:** ChargeJob 은 `DispatchChargeJobActivity` 가 ProcessingState/TransferState 를 안 바꾸고 IDLE/NOTASSIGNED 그대로 두어, 기존 `RecoverStuckVehiclesActivity` 의 PROCESSINGSTATE_RUN 첫 필터에서 탈락. 충전소 이동 중 AMR 이 RunState=STOP 으로 멈춰도 CARRIERTRANSFER 재전송이 안 됐음.

**변경:**
- 루프 진입 직후 공통 필터(STOP, NOALARM, TC 비어있지 않음)를 먼저 적용.
- ProcessingState=IDLE 분기에서 TC.JobType=CHARGEMOVE 면 destNode 미도착(StationId 불일치) 조건 만족 시 `CarrierTransferJsonBuilder.Build(..., JOBTYPE_CHARGEMOVE, useSource:false, ...)` + `SendCarrierTransferJson` 으로 재전송.
- 기존 일반 Job (ProcessingState=RUN) 분기는 CHARGEMOVE 분기 뒤로 이동, 로직 그대로 유지.

**Why:** 사용자 질문 "ChargeJob 도 RunState=STOP 시 CARRIERTRANSFER 재전송하는가" 확인 중 누락 발견. Stuck ChargeJob 자동 복구 추가로 수동 개입 없이 충전 디스패치 회복 가능.

**검증:** `dotnet build ACS.Elsa.csproj` 성공 (0 오류). 실제 시나리오는 AMR Simulator 로 (1) battery<30 → ChargeJob assign, (2) RunState=STOP 보고 (destNode 미도착), (3) 10초 내 로그에 `CHARGEMOVE 재전송` Info 확인, (4) 충전 노드 도착 후엔 재전송 발화 안 되는지 회귀 체크 필요.

---

## 16. EI → AMR moveCmd 사양 일치화 (portType + amrSlot 추가)

**날짜:** 2026-05-21
**작업:** `docs/ACS-AMR_mqtt_movecmd.md` 사양에 맞춰 EI 가 AMR 로 발행하는 `moveCmd` JSON 에 `portType` / `amrSlot` 필드 추가.

**문제:**
- AMR 측은 `portType` (FACILITY=설비포트 / MATERIAL=자재포트) 으로 도착 후 시퀀스를 분기 (`AMR/Service/MoveSequenceRunner.cs:577-587`) 하는데, 기존 ACS 송신 페이로드에는 `portType` 필드 자체가 없었음.
- ACS 내부 `RailCarrierTransferMessage.Data.PortType` 은 이미 `"EQP"`/`"MAT"` 로 채워서 들어오지만, `HandleCarrierTransferActivity` 가 JSON 파싱에서 그 키를 무시하고 버림.
- 사양은 `amrSlot` (int 1~4, 기본 1) 도 요구 — Cobot DI 매핑(`amrSlotOffset = amrSlot - 1`)에 사용됨.

**변경:**
- `src/ACS/ACS.Communication/Mqtt/Model/AmrCommandMessage.cs`
  - 필드 추가: `PortType` (string), `AmrSlot` (int, 기본값 1)
  - `AmrPortTypeMapper` static 클래스 추가 — 도메인 `"EQP"` → AMR `"FACILITY"`, 그 외(`MAT`/`LP`/`OP`/`BP`/`null`) → `"MATERIAL"` (사양 default 와 일치)
- `src/ACS/ACS.Communication/Mqtt/MqttInterfaceManager.cs`
  - `SendDestination(...)` 시그니처에 `string portType = null, int amrSlot = 1` 추가
  - `AmrCommandMessage` 생성 시 `AmrPortTypeMapper.ToAmr(portType)` 으로 변환하여 설정
  - `SendCommand` 의 INFO 로그에 `portType` / `amrSlot` 추가
- `src/ACS/ACS.Elsa/Activities/MqttActivities.cs` (`HandleCarrierTransferActivity`)
  - JSON 파싱에서 `portType` 키 추출
  - `SendDestination(..., commandId, portType)` 로 전달 (amrSlot 은 default 1)
  - 성공/실패 로그에 `portType` 추가

**Why:** 사용자 검증 중 "moveCmd 페이로드에 설비/자재 구분자가 없다" 지적. AMR이 portType 없으면 자재포트로 간주(사양 default)하여 설비포트 도착 시 ActionCmd 대기 단계를 스킵하므로 실제 운영에서 reach goal 시퀀스 오류 가능.

**결정 사항 (디폴트 채택):**
- LP/OP/BP 매핑: 일단 모두 MATERIAL 로 묶음. 추후 LP 도 FACILITY 분류가 필요해지면 `AmrPortTypeMapper.ToAmr` 만 수정하면 됨.
- amrSlot 출처: 현재 도메인 매핑 없어서 사양 default `1` 고정. Vehicle 다중 슬롯 운용 시 RAIL-CARRIERTRANSFER 에 `amrSlot` 필드 신설 + `SendDestination` 마지막 인자로 전달하는 방식으로 확장 예정.
- `SendAction` (actionCmd) 은 사양에 portType 명시가 없어 미변경.

**검증:** `dotnet build ACS.sln` 성공 (0 오류, 기존 경고 71개만). 실제 검증은 MQTT 클라이언트로 `amr/{commId}/command` 구독 → CARRIERTRANSFER 트리거 시 JSON 에 7개 필드(cmdId/command/nodeId/port/jobType/portType/amrSlot) 모두 포함, EQP 포트 → `"FACILITY"`, 그 외 → `"MATERIAL"` 출력 확인 필요.

**※ 16-1 후속 결정 (동일일):** 사용자 요청으로 매핑(FACILITY/MATERIAL)을 폐기하고 **LocationEx.Type 값을 그대로** AMR 에 통과시키는 방식으로 변경. 자세한 내용은 §16-1 참조.

---

## 16-1. EI → AMR moveCmd portType 을 LocationEx.Type 그대로 통과로 전환

**날짜:** 2026-05-21
**작업:** §16 의 EQP↔FACILITY 매핑을 폐기하고 ACS LocationEx.Type 값을 그대로 AMR `portType` 에 실어 보내도록 전환.

**문제/배경:**
- §16 에서 사양 문서(`docs/ACS-AMR_mqtt_movecmd.md`) 의 `FACILITY/MATERIAL` 표기를 따라 `AmrPortTypeMapper` 로 매핑 처리했음.
- 사용자 의도는 "도메인에 있는 LOCATION type 정보(EQP/BUFFER/CHARGE 등) 를 그대로 보내라" — 임의 변환을 한 곳에 두지 말고 송신·수신 양쪽이 같은 어휘를 쓰자는 것.

**변경:**
- `src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs` — `CarrierTransferJsonBuilder.Build()` 에서 `Port.PortType` (EQP/MAT/LP/OP/BP) 대신 `LocationEx.Type` (EQP/BUFFER/INPUT/OUTPUT/CHARGE/VBUFFER) 사용. 기존 location 조회를 그대로 활용 (nodeId 추출과 같은 호출에서 같이 가져옴) — `GetUnitByName` + `Port.PortType` 블록은 제거.
- `src/ACS/ACS.Communication/Mqtt/Model/AmrCommandMessage.cs` — `AmrPortTypeMapper` static 클래스 삭제, `PortType` 필드 주석을 LocationEx.Type 값으로 갱신.
- `src/ACS/ACS.Communication/Mqtt/MqttInterfaceManager.cs` — `SendDestination(...)` 의 `PortType = AmrPortTypeMapper.ToAmr(portType)` 을 `PortType = portType ?? ""` 로 변경 (매핑 호출 제거).
- `src/ACS/ACS.Communication/Mqtt/Model/RailCarrierTransferMessage.cs` — `PortType` 필드 주석을 LocationEx.Type 값으로 갱신.
- `docs/ACS-AMR_mqtt_movecmd.md` — 요청 메시지 예시 값, 필드 표, "PortType에 따른 시퀀스 차이", "Cobot Digital Input 매핑" 표를 `EQP` (설비포트) / `BUFFER`·`INPUT`·`OUTPUT`·`VBUFFER` (자재포트) / `CHARGE` (충전소) 카테고리 기준으로 재기술.

**Why:** AMR 인터페이스 어휘를 ACS 도메인 어휘와 일치시키기 위함. 임의 매핑이 EI 한 곳에 끼면 진실 표가 2개가 되어 LP/OP/BP 같은 모호한 케이스 분류 정책에 결정 부담이 생긴다. 한쪽(LocationEx) 에 정의를 두고 그대로 통과시키면 향후 카테고리 추가/변경 시 사양 문서·도메인 두 곳만 손대면 된다.

**검증:** `dotnet build ACS.sln` 성공 (0 오류, 기존 경고 71개만). 실제 검증은 MQTT 클라이언트로 `amr/{commId}/command` 구독 → 설비포트 도착 시 `"portType":"EQP"`, 버퍼 도착 시 `"portType":"BUFFER"`, 충전소 도착 시 `"portType":"CHARGE"` 직렬화 확인 필요.

**후속 작업 (별도 레포):** AMR 프로그램의 `AMR/Service/MoveSequenceRunner.cs:577-587` 분기를 새 값(`EQP`/`BUFFER`/`INPUT`/`OUTPUT`/`CHARGE`/`VBUFFER`) 기준으로 동기화해야 운영 정합성이 유지됨.

---

## 17. 현재 상태 및 미완료 항목

**빌드 상태:** 성공 (경고만, 오류 0)

**미완료/향후 작업:**
- [ ] VehicleListView가 현재 어떤 탭/뷰에도 연결되지 않음 — SummaryView 하단 통합 또는 별도 패널로 배치 필요
- [x] Data View 탭 리본 서브메뉴 구현 (5개 카테고리 + 드롭다운)
- [x] Application 탭 리본 서브메뉴 구현 (Application + NIO)
- [x] Application Management 화면 구현 (TreeView + Properties)
- [x] NIO View 화면 구현 (DataGrid)
- [x] Dock.Avalonia 도킹 레이아웃 마이그레이션 완료
- [x] Summary 고정 + Document 온디맨드 재구성 완료
- [x] Elsa Workflows 3 통합 완료
- [x] Control Server HeartBeat 스케줄링 정상화 (10개 버그 수정)
- [ ] run-all.sh PROCESSES 배열에 CS01_P 추가
- [ ] JsonBackedWorkflow 파라미터 없는 생성자 경고 수정
- [ ] 리본바 탭별 서브메뉴 구현 (User, Basic Control, History, Log, Layout, Preference — 현재 플레이스홀더)
- [ ] Mini Map 구현 (SummaryView 하단 — 현재 플레이스홀더)
- [ ] 변경사항 커밋
