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

## 17. ChargeJob TC 정리 누락 — Step 순서/메모리 동기화 재수정

**날짜:** 2026-05-21
**작업:** §14 의 후속. `RailVehicleUpdateWorkflow.cs` Step 7/8 순서를 뒤집고 메모리 동기화를 보강하여, BatteryRate≥30 상태로 N1001 에 도착하는 메시지에서도 TC 가 즉시 정리되도록 한다.

**문제:**
- §14 의 훅이 들어간 후에도 "ChargeJob 정상 완료 (배터리 30%↑) 후 `NA_T_TRANSPORTCMD` 의 CHARGEMOVE row 잔존" 재현.
- 원인 1 — **블록 순서**: 기존 코드는 Step 7(CHARGE→IDLE + TC 정리) 가 Step 8(NodeChanged→CHARGE 진입) 보다 먼저였음. AMR 이 N1001 도착 메시지에 이미 BatteryRate≥30 인 경우, 같은 메시지에서 Step 7 은 `vehicle.ProcessingState=IDLE` 로 보고 건너뛰고 Step 8 만 CHARGE 로 세팅 → 정리 누락.
- 원인 2 — **인메모리 비동기화**: `ResourceManagerExImplement.UpdateVehicle(...)` 은 `PersistentDao.UpdateByAttribute` 직접 호출로 DB 만 갱신, 인메모리 `VehicleEx` 객체를 mutate 하지 않음 (`ResourceManagerExImplement.cs:344-347`). Step 8 에서 DB 를 CHARGE 로 바꿔도 같은 활동의 후속 조건 평가에는 반영 안 됨.
- 원인 3 — **case-sensitive 비교**: Step 7 의 `vehicle.ProcessingState == VehicleEx.PROCESSINGSTATE_CHARGE` 는 case-sensitive `String ==` 인 반면 Step 8 은 `OrdinalIgnoreCase` 사용. 대소문자 혼입 시 미발화 위험.

**변경:** `src/ACS/ACS.Elsa/Workflows/Trans/RailVehicleUpdateWorkflow.cs`
1. Step 8(NodeChanged→CHARGE) 을 Step 7(CHARGE→IDLE + TC 정리) **앞**으로 이동.
2. Step 8 에서 ProcessingState 를 DB 에 쓴 직후 `vehicle.ProcessingState = VehicleEx.PROCESSINGSTATE_CHARGE;` 로 인메모리 동기화 (Step 7 평가가 같은 메시지에서 정확히 발화하도록).
3. Step 7 비교를 `OrdinalIgnoreCase` 로 통일.
4. Step 7 의 IDLE 전이 직후도 동일하게 `vehicle.ProcessingState = ...IDLE` 동기화.
5. 진단 로그 보강: 분기 진입 시 (ProcessingState/BatteryRate/threshold) 출력, TC 미존재/JobType 불일치 케이스 명시 로그, `DeleteTransportCommand` 가 0행 반환 시 `Error` 로 승격.

**Why:** §14 fix 가 동작하지 않는 시나리오가 실측으로 재현됨. 단일 진실원천은 DB 지만, 같은 활동 안에서 두 단계가 ProcessingState 를 가지고 분기하므로 인메모리 일관성을 유지해야 함. 디자인은 그대로(삭제 시점=BatteryRate≥30) 유지하고 순서/동기화만 교정.

**검증:** `dotnet build ACS.Elsa.csproj` 성공 (0 오류, 기존 경고만). 실측 검증 포인트:
- (a) `NA_T_TRANSPORTCMD` CHARGEMOVE 사라짐
- (b) `NA_H_TRANSPORTCMDHISTORY` 에 cause=`CHARGECOMPLETED` 이력 1행
- (c) `NA_R_VEHICLE` TransportCommandId/Path/AcsDestNodeId 빈 문자열, ProcessingState=IDLE
- (d) 로그에서 `Vehicle ProcessingState → CHARGE (충전 노드 도착)` 다음 줄에 `ChargeJob 완료: TC 삭제 tc=..., deleted=1` 가 같은 vehicleId 로 연속해서 찍힘
- (e) 회귀 — BatteryRate<30 도착 시에는 TC 잔존 + ProcessingState=CHARGE 만 설정

---

## 18. 현재 상태 및 미완료 항목

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

---

## 19. Rider 디버그 배포 실행 구성 추가

**날짜:** 2026-05-23
**작업:** `publish-deploy.ps1` 을 Debug 빌드로 실행해 `src/ACS/deploy/<SITE>/` 에 디버깅 가능한 실행파일을 생성하는 Rider Run/Debug 구성 추가.

**변경:** `src/ACS/.run/Publish_Deploy__Debug_.run.xml` (신규)
- 타입 `ShConfigurationType` (Shell Script), 이름 `Publish Deploy (Debug)`.
- `SCRIPT_PATH=$PROJECT_DIR$/publish-deploy.ps1`, `SCRIPT_OPTIONS=-Configuration Debug`, 인터프리터 `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`, 터미널 실행.
- `$PROJECT_DIR$` = `src/ACS` (.idea 위치 기준). git 추적되는 공유 구성으로 팀과 공유됨.

**Why:** 기존 workspace.xml 의 동일 스크립트 항목은 `default="true"` 템플릿이라 Run 드롭다운에 노출되지 않고 공유도 안 됨. 스크립트 기본값(Release) 대신 Debug 로 publish 하면 PDB 포함·최적화 없는 산출물이 배포되어 사이트 exe 에 디버거 attach 가능.

**수정 (2026-05-23):** 인터프리터를 파일명 `powershell.exe` 만 지정하면 Rider 가 PATH 해석에 실패해 "오류: 인터프리터를 찾을 수 없습니다" 발생. `INTERPRETER_PATH` 를 절대 경로 `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe` 로 변경하여 해결.

---

## 20. RecoverStuckVehiclesActivity 에 DISCONNECT 가드 추가

**날짜:** 2026-05-23
**작업:** `RecoverStuckVehiclesActivity` (`src/ACS/ACS.Elsa/Activities/ScheduleActivities.cs`) 의 RAIL-CARRIERTRANSFER 재전송 루프에 ConnectionState 가드 추가.

**문제:** `AwakeCheckVehiclesJob` 이 10초마다 도는 `SCHEDULE-CHECKVEHICLES` 워크플로우에서, Step 2 `DisconnectVehiclesActivity` 는 stale vehicle 의 `ConnectionState` 만 DISCONNECT 로 바꾸고 ProcessingState/RunState/AlarmState 는 그대로 둠. 이어지는 Step 3 `RecoverStuckVehiclesActivity` 는 `ConnectionState` 를 전혀 검사하지 않아, 반송 중(RUN+STOP+NOALARM+TC 보유) 연결이 끊긴 vehicle 에 같은 사이클에서 곧바로 재전송하고 이후 10초마다 반복함. 송신 메서드 `SendCarrierTransferJson` (`MessageManagerExImplement.cs:1701`) 에도 connection 가드 없음. (ALARM 케이스는 `:896` 에서 이미 차단됨.)

**변경:**
- ALARM 가드(`:896`) 바로 다음에 `if (!VehicleEx.CONNECTIONSTATE_CONNECT.Equals(vehicle.ConnectionState, OrdinalIgnoreCase)) continue;` 한 줄 추가. 루프 상단이라 CHARGEMOVE(IDLE) 분기와 일반 RUN 분기를 모두 커버.
- 동일 파일 XML doc "발동 조건" 목록에 `ConnectionState == CONNECT` 추가.
- `ScheduleCheckvehiclesWorkflow.cs` 주석에 "ALARM/DISCONNECT 제외" 명시.

**Why:** 사용자 질문 "vehicle 이 disconnect/alarm 일 때도 계속 재전송하는가" 확인 중, ALARM 만 가드되고 DISCONNECT 는 누락된 것을 발견. 연결 끊긴 vehicle 에 명령을 계속 푸시하는 것은 의도된 동작이 아니므로 ALARM 과 동일하게 차단. 관련: [[15. CHARGEMOVE 재전송 분기]].

**검증:** `dotnet build ACS.Elsa.csproj` 성공 (0 오류, 기존 경고 7). 실제 시나리오: 반송 중 vehicle 의 EventTime 60초+ 미갱신 → DISCONNECT 유발 후 `ELSA_ACTIVITY` 로그에 해당 vehicleId `RAIL-CARRIERTRANSFER 재전송` 미발화 확인, CONNECT 복귀 후 정상 재전송·ALARM 회귀 체크 필요.

---

## 21. UI 백엔드를 control 프로세스로 통합 (UiModule 폐지)

**날짜:** 2026-05-23
**작업:** 기존 `ui` 프로세스(UI01_P)가 담당하던 UI 백엔드(REST API + SignalR) 호스팅을 `control` 프로세스(CS01_P)로 이전하고, `ui` 프로세스 타입을 폐지.

**배경/동기:** UI에서 사용자가 trans/ei/daemon 등 서버를 kill/reload하려면 그 기능을 실제로 가진 것은 control 프로세스의 `IControlServerManager`(start/kill/heartbeat/reload). UI 백엔드와 control이 별도 프로세스라 RabbitMQ를 한 번 우회해야 했음. UI 백엔드를 control에 통합하면 in-process로 직접 접근 가능.

**통합이 깔끔했던 이유 (탐색 결과):**
- `PoseTelemetrySubscriber`/`HostCommSubscriber`(`ACS.App/Web/Realtime/`)는 `IConfiguration`에서 직접 RabbitMQ fanout에 자체 커넥션을 연다. `IHubContext`/`IConfiguration`/`ILogger`에만 의존 → 그대로 control로 이전 가능.
- REST 컨트롤러(`Web/Controllers/AcsRestControllers.cs`)는 `IResourceManagerEx`/`ITransferManagerEx`만 주입받음 — ControlModule이 이미 둘 다 등록(`ResourceManagerExImplement`, `TransferManagerExsImplement` as `ITransferManagerEx`).
- `ACS.App`은 이미 웹 SDK 사용(`WebApplication.CreateBuilder`) → csproj 변경 불필요.

**변경:**
- `Modules/ControlModule.cs`: `ICacheManagerEx`(CacheManagerExImplement) 등록 추가, `PoseTelemetrySubscriber`/`HostCommSubscriber`를 `As<IHostedService>`로 등록 추가.
- `Program.cs`: `Main` 분기 `ui` → `control`; `RunUiHost` → `RunWebHost`로 일반화(내부 로직 동일). 콘솔 호스트는 host/trans/ei/daemon/query/report용으로 유지.
- `Executor.cs` `RegisterProcessModule`: `case "ui"` 제거.
- `Modules/UiModule.cs` 삭제.
- `deploy/CS01_P/appsettings.json`: `Acs:Api:ListenPort` 5102 → 5100 (데스크탑 클라이언트 `ACS.UI/appsettings.json`이 `127.0.0.1:5100`을 보므로 클라이언트 무변경).
- `deploy/UI01_P/appsettings.json` 삭제(프로세스 폐지). UI01_P 배포 폴더의 빌드 산출물은 gitignore 대상.
- 문서: `ACS.App.claude.md` 모듈/HTTP API/실행 호스트 섹션 갱신.

**Why:** [[18. 현재 상태 및 미완료 항목]] — control이 서버 관리 주체이므로 UI 백엔드도 control이 겸하는 것이 자연스러움. RabbitMQ 분기(`MsbRabbitMQModule`의 control: CsListener/ApplicationControlAgentListener/CsSenderToServer/CsSenderToUi/HeartBeatRpcSender)는 무변경 — 구독자가 자체 커넥션을 쓰므로 영향 없음.

**미완료(범위 외):** UI가 서버를 직접 kill/reload하는 신규 REST/SignalR 엔드포인트(예: `/api/servers`)는 미구현. 이제 control 프로세스 안에서 `IControlServerManager`에 직접 접근 가능하므로 다음 단계로 추가 예정(RabbitMQ 우회 UiCommandJob 대신 in-process 직접 호출로 단순화 가능).

**검증:** `dotnet build ACS.App.csproj` 성공 (0 오류, 기존 경고 17). 런타임 검증 필요: control 기동 시 Kestrel 5100 수신, `/api/vehicles`·`/api/commands` 응답, `/hubs/vehicle`·`/hubs/hostcomm` 연결, ACS.UI 데스크탑 클라이언트 표시, heartbeat 잡 및 TS/ES/DS start/kill 회귀(웹 호스트 전환 후 `ControlServerManager` 활성화 여부가 핵심 회귀 포인트).

**후속(publish/deploy 정리):** ui 프로세스 폐지에 맞춰 산출물·스크립트의 UI01_P/`ui` 흔적 제거.
- `git rm -r src/ACS/publish/UI01_P/` (git 추적 publish 산출물 454개 제거 — `publish/` 트리는 전체 git 추적). 물리 `deploy/UI01_P/`·`publish/UI01_P/` 폴더도 삭제(`publish-deploy.ps1`이 `deploy/*/`를 자동 열거하므로 잔여 폴더 제거 필요).
- `run-all.sh`: 활성 목록 `UI01_P` → `CS01_P` 대체(control이 5100에서 UI 백엔드 겸함), 전체 목록·COLORS 주석·실행 안내 echo에서 UI01_P 제거.
- `deploy.ps1`: `$Targets`에서 `'ui'` 제거(이 스크립트엔 control 타겟 없음).
- `publish-deploy.ps1`/`.run/Publish_Deploy__Debug_.run.xml`은 사이트 하드코딩 없어 무변경. `.idea` workspace.xml은 git 미추적·참조 없음.

---

## 22. ControlModule Scripts 프로세스 기동 정상화

**날짜:** 2026-05-23
**작업:** control 프로세스가 다른 프로세스(TS/ES/DS/HS)를 START/재기동하는 경로의 결함 수정.

**배경/동기:** `ControlModule`이 설정한 `Scripts` Hashtable은 `Control()`(CONTROL-START 메시지) 또는 `HeartBeatJob`(다운 감지 자동 재기동) → `ControlServerManagerImplement.Start()` → `GetStartScript()` → `SystemUtility.PerformCommand()`로 사용됨. 사용자 질문("Scripts 실행이 구현돼 있나") 확인 중 3개 결함 발견.

**문제:**
1. `SystemUtility.PerformCommand`가 `WaitForExit()` 없이 `process.ExitCode`를 즉시 읽음 → 미종료 프로세스 접근 시 `InvalidOperationException` → `PerformCommandException`으로 래핑 → `Start()`가 false 반환하고 `ScheduleHeartBeat()`를 건너뜀. 즉 서버가 떠도 control이 실패로 간주하고 감시를 안 검(START의 fire-and-forget 의미가 깨짐).
2. `Scripts["HS-START"]`가 `TS01_P.exe`를 가리키는 복붙 오류 + `GetStartScript()`에 `"host"` 분기 누락 → DB에서 active(PRIMARY)인 host(HS01_P)를 HeartBeatJob이 재기동 불가.
3. `ExecuteCoreDump`가 미설정 `Scripts["COREDUMP"]`(null)를 그대로 `PerformCommand`에 전달. (MS/RS/QS = emulator/report/query는 배포·DB에 없어 START 빈문자열 안전처리로 충분 — 추가 작업 불요.)

**변경:**
- `ACS.App/appsettings.json`: `Acs:Control:Scripts` 섹션 신설(TS/ES/DS/HS-START 경로). 하드코딩 → 설정 이동으로 환경별 경로 조정 가능.
- `ACS.App/Modules/ControlModule.cs`: 하드코딩 Hashtable → `mgr.Configuration.GetSection("Acs:Control:Scripts").GetChildren()`로 로드.
- `ACS.App/Control/Implement/ControlServerManagerImplement.cs`: `SCRIPT_HS_START`/`SCRIPT_HS_KILL` 상수 추가, `GetStartScript()`에 `"host"` 분기 추가, `Start()`를 재작성(`SystemUtility.GetProcessId`로 이미-실행 가드 후 `SystemUtility.StartProcess`로 detached 기동 → ScheduleHeartBeat; OS 분기 죽은코드 제거), `ExecuteCoreDump()` null 가드 추가.
- `ACS.Core/Utility/SystemUtility.cs`: `StartProcess(filePath)` 신규(UseShellExecute=true, fire-and-forget). `PerformCommand` 수정 — null/빈인자 가드, `cmd.exe`+CreateNoWindow, 출력 ReadToEnd 후 WaitForExit→ExitCode, 캡처 출력 반환, ExitCode!=0이면 3-인자 생성자(exitValue 포함)로 예외 던져 Start/Kill의 exit-code 3/4 분기 동작.

**Why:** START는 장기 실행 서버 기동이므로 종료를 기다리면 안 됨(detached). 단명령(taskkill/coredump/getprocessid/systemcheck)은 종료·종료코드·출력이 필요하므로 PerformCommand는 그쪽 전용으로 정상화하고 START는 별도 메서드로 분리. host 재기동은 HS-START가 이미 의도에 있었으므로 분기 추가. Kill/GetProcessId는 `UseSystemKill`/`UseSystemGetProcessId`=true라 system 경로 유지(스크립트 미사용)로 영향 없음. 관련: [[21. UI 백엔드를 control 프로세스로 통합]].

**검증:** `dotnet build ACS.sln` 성공(0 오류, 기존 경고 98). 런타임 검증 필요: control 기동 후 Scripts 4종 로드, CONTROL-START 또는 HeartBeatJob 다운 감지 시 TS/ES/DS/HS 실제 기동 + ScheduleHeartBeat 등록, HS01_P 종료 후 host 재기동, COREDUMP 미설정 시 예외 없이 false 처리.

---

## 23. control 웹호스트 기동 DI 오류 수정 (ElsaModule이 기본 IServiceProvider 덮어씀)

**날짜:** 2026-05-24
**작업:** CS01_P(control) 웹 호스트 기동 시 `WebApplicationBuilder.Build()`에서 발생하던 Autofac DI 크래시 수정.

**증상:** `Autofac ... activating KestrelServerImpl -> λ:System.Diagnostics.DiagnosticSource ---> No service for type 'System.Diagnostics.DiagnosticListener' has been registered.` (`Program.cs:195`).

**근본 원인:** `ACS.Elsa.ElsaModule`이 Elsa 3를 자체 `ServiceCollection`→`BuildServiceProvider()`로 만든 **격리** `IServiceProvider`를 `.As<IServiceProvider>()`로 Autofac **기본** `IServiceProvider`로 등록(+scope factory도 `.As<IServiceScopeFactory>()`로 기본 등록). 웹 호스트에서는 `AutofacServiceProviderFactory.CreateBuilder`가 `Populate()`로 호스트 프레임워크 서비스(DiagnosticListener)와 기본 IServiceProvider(AutofacServiceProvider)를 먼저 등록하는데, 그 뒤 ElsaModule이 기본 IServiceProvider를 Elsa 격리 provider로 덮어씀(Autofac last-wins). 이후 Kestrel resolve 시 `DiagnosticSource` 팩토리 `sp => sp.GetRequiredService<DiagnosticListener>()`의 `sp`가 Elsa 격리 provider로 잡혀 DiagnosticListener를 못 찾음. 콘솔 프로세스(host/trans/ei/daemon)는 `Executor.Start()`의 plain Autofac 컨테이너 + Kestrel 없음이라 덮어쓰기가 무해해 지금까지 정상이었음(ElsaModule은 5개 모듈 전부 로드).

**안전성:** `IServiceProvider`/`IServiceScopeFactory`를 주입/resolve하는 곳은 ACS 소스 전체에서 `ElsaWorkflowManagerBridge`(`_scopeFactory.CreateScope()`로 Elsa scoped `IWorkflowRunner` 해석) 단 하나뿐 → Elsa provider/scope factory를 named로만 등록하고 bridge에 named scope factory를 명시 주입하면 양쪽 안전.

**변경 (ElsaModule.cs 한 파일):**
- 격리 provider 등록에서 `.As<IServiceProvider>()` 제거, `.Named<IServiceProvider>("ElsaServiceProvider")`만 유지.
- scope factory를 `.As<IServiceScopeFactory>()` → `.Named<IServiceScopeFactory>("ElsaScopeFactory")`.
- bridge 등록에 `ResolvedParameter`로 생성자 `IServiceScopeFactory scopeFactory`에 `"ElsaScopeFactory"` 주입. bridge 생성자 시그니처는 무변경.
- `using Autofac.Core;` 추가(`ResolvedParameter`).

**Why:** Elsa가 자체 격리 provider를 쓰는 구조([[21. UI 백엔드를 control 프로세스로 통합]]로 control이 웹 호스트가 되면서 드러남)에서, 격리 provider를 컨테이너 기본 서비스로 노출하면 호스트 프레임워크(Kestrel/MVC/SignalR)의 DI가 깨진다. named 전용 등록으로 호스트 기본 DI와 Elsa DI를 분리. Elsa→Autofac 접근은 기존대로 `AutofacContainerAccessor`(Elsa ServiceCollection에 등록, Executor가 Container 설정) 사용 — 무변경.

**검증:** `dotnet build ACS.sln` 성공(0 오류). 런타임 검증 필요: CS01_P 기동 시 Build() 통과 + "Web backend started ...:5100", 콘솔 프로세스에서 Elsa 워크플로우(bridge ElsaCommands 라우팅) 정상 실행. **주의:** 실행 exe는 `D:\ACS\deploy\CS01_P\`에서 로드되므로 빌드 산출물(특히 ACS.Elsa.dll) 재배포 후 재실행할 것.

---

## 24. control heartbeat 재기동 무한루프 수정 (정상 active 프로세스가 종료→재실행 반복)

**날짜:** 2026-05-24
**작업:** control(CS01_P)이 워커(TS/ES/DS/HS)를 heartbeat로 감시하던 중, 상태가 active인 정상 프로세스인데도 "Kill→Start"를 무한 반복하던 문제 수정. 독립적인 4개 결함이 겹쳐 있었고 각각 단독으로도 루프 유발.

**근본 원인 4가지:**
1. **재시도 타임아웃 단위 반전** (`HeartBeatJob.cs:67`): 초기 체크(:47)는 ms 모드에서 5000ms인데, 재시도는 삼항 분기가 반대라 `HeartBeatRetryTimeout/1000 = 10000/1000 = 10ms`. RabbitMQ 왕복이 10ms 안에 불가 → 모든 재시도 즉시 실패 → ProcessHang case 2(Kill+Start) 직행, 재시도 회복 불가.
2. **공유 RPC 클라이언트 동시성 결함**: `GenericRabbitMQSender`(RPC_CLIENT, 싱글톤 `ISynchronousMessageAgent`)가 단일 채널(IModel)·고정 correlationId·단일 respQueue를 공유. `AbstractJob`에 `[DisallowConcurrentExecution]` 없음 + Quartz `threadCount=5`(`SchedulingModule.cs:34`) → 동일 StartDelay(10s)+Interval(20s)로 스케줄된 4개 heartbeat 잡이 거의 동시 발화하여 thread-unsafe한 채널을 동시 publish → 응답 혼선·유실 → 정상 프로세스도 타임아웃.
3. **host 타입 ControlAgent 리스너 미등록** (`MsbRabbitMQModule.RegisterHostMsb`): trans/ei/daemon과 달리 `RegisterControlAgentListener` 호출 없음 → HS01_P가 CONTROL-HEARTBEAT에 응답 불가 → 매 주기 Kill+Start.
4. **Start() 신규 기동 시 상태 active 미갱신** (`ControlServerManagerImplement.cs:748-751`): 이미-실행 분기(:743)만 active 설정 → Kill(→inactive)→Start 후 DB가 inactive로 남아 Reschedule 제외/UI 표시 불일치.

> 큐 이름은 정상 일치(`ChannelDestination.Init`이 양쪽 `/VM/DEMO/CONTROL/AGENT/{app}`로 정규화), 워커 리스너는 RPCSERVER라 `OnRequest.finally`에서 응답 echo → 단일·직렬이면 정상. 즉 동시성/타임아웃이 핵심.

**변경:**
- `ACS.App/Control/Scheduling/HeartBeatJob.cs`: 재시도 타임아웃 삼항을 초기 체크와 동일 규칙으로 교정(ms 모드 10000ms).
- `ACS.Communication/Msb/RabbitMQ/GenericRabbitMQSender.cs`: `_rpcLock` 추가, `Request(string,string,long,...)`를 lock으로 직렬화하고 publish 전 respQueue의 stale 응답을 drain(`while(TryTake(out _,0)){}`). control의 유일한 RPC_CLIENT가 HeartBeatRpcSender라 영향 범위는 heartbeat 한정.
- `ACS.App/Modules/MsbRabbitMQModule.cs`: `Load` host 케이스가 server 브로커 자격증명도 `RegisterHostMsb`에 전달하도록 시그니처 확장. `RegisterHostMsb`에서 `(server.domainvalue)/CONTROL/AGENT/@{application}` destination으로 `RegisterControlAgentListener`를 server 브로커로 등록(HostModule이 `IApplicationControlManager` 등록 → OnlyIf 가드 통과).
- `ACS.App/Control/Implement/ControlServerManagerImplement.cs`: `Start()` 신규 기동 분기에서 `StartProcess` 직후 `UpdateApplicationState(name,"active")` 추가.

**Why:** 사용자 요구는 "재기동이 제대로 동작"이지 비활성화가 아니므로 `HeartBeatFailWhenProcessHang/Down=2` 정책은 유지. 동시성은 사용자 결정에 따라 RPC 멀티플렉싱 재설계 대신 lock 직렬화 채택(20s 주기/4프로세스라 타이밍 여유 충분, 변경 최소·저위험). 기동 유예는 재스케줄 시 적용되는 StartDelay(10s)+타임아웃 정상화로 충분하여 별도 grace 미추가. 관련: [[22. control Scripts 실행 구현]](Start/host 분기·UseSystemKill 맥락 공유).

**검증:** `dotnet build ACS.sln` 성공(0 오류, 기존 경고 72). 런타임 검증 필요: control+워커 기동 후 `HEARTBEATFAIL_PROCESSHANG/DOWN` 로그가 반복되지 않고 4개 워커가 active 안정 유지, 특히 HS01_P가 더는 주기적으로 죽지 않을 것, `RPC timeout` 로그 소멸. 워커 강제 종료 시 1주기 내 재기동되어 active 복귀(재기동 시나리오 정상). **주의:** 실행 exe는 `D:\ACS\deploy\*`에서 로드되므로 빌드 산출물 재배포 후 재실행할 것.

---

## 25. heartbeat 루프 진짜 원인 = 워커 리스너 기동 지연 (진단 로그로 확정) + 기동 유예 수정

**날짜:** 2026-05-24
**작업:** [[24. control heartbeat 재기동 무한루프 수정]]의 4개 수정 후에도 4개 워커 전부 100% RPC 타임아웃이 지속되어, 양쪽에 `[HB-DIAG]` 진단 로그를 심어 단절 지점을 확정하고 진짜 원인을 수정.

**진단 결과(런타임 로그):** RPC 응답 메커니즘 자체는 **정상**이었다. 워커의 control-agent 리스너가 뜬 뒤에는 모든 heartbeat가 ~40ms에 왕복 성공(`reply recv match=True`). 문제는 **타이밍**:
- 워커 프로세스 시작(14:26:19) 후 `RPC_SERVER listening queue=/VM/DEMO/CONTROL/AGENT/TS01_P` 로그가 **~29초 뒤(14:26:48)** 에야 출력. 워커 부팅이 Elsa 워크플로우 등록(~20s) + DB 마이그레이션 + 다른 리스너들 → control-agent 리스너는 `StartMsb` 순회의 거의 마지막에 기동되기 때문.
- 이 ~30초 부팅 창 동안 control의 heartbeat는 타임아웃(프로세스는 존재하나 리스너 미소비) → HeartBeatJob의 hang 경로 → 재시도 실패 → `Kill+Start`로 **부팅 중인 워커를 죽임** → 재시작 → 또 30초 부팅 → 또 죽임 → 앱들이 돌아가며 재기동되는 루프.
- 리스너가 무사히 뜨면 그 이후로는 완전히 안정(로그상 14:26:48 이후 타임아웃 없음). 즉 **부팅 창에서의 오판(hang)** 이 루프의 직접 원인.

**부수 관측:** 워커 control-agent 큐(`durable=false, exclusive=false, autoDelete=false`)가 워커 재시작·control 재시작을 넘어 **잔존**하며, 리스너 미소비 동안 heartbeat가 쌓였다가 리스너 기동 시 한꺼번에 drain됨(이전 control 인스턴스의 corrId/replyTo로 온 stale 메시지 포함 → 죽은 reply 큐로 응답되어 버려짐). 기동 유예 적용 시 부팅 창에 heartbeat를 발행하지 않으므로 누적이 최소화되어 무해. (향후 개선: 큐를 autoDelete/exclusive로 두거나 메시지 TTL 부여.)

**변경:**
- `ACS.App/Control/Implement/ControlServerManagerImplement.cs`: `HeartBeatStartupGrace`(기본 60000ms) 추가. `ScheduleHeartBeat(app, startDelay)` 및 `CreateHeartBeatTrigger(app, jobDetail, startDelayValue)` 오버로드 추가(기존 무인자는 `HeartBeatStartDelay`로 위임). `Start()`의 양쪽 분기(이미-실행/신규-기동)가 첫 heartbeat를 `HeartBeatStartupGrace`만큼 지연 스케줄 → 부팅 중 hang 오판 방지.
- `ACS.App/Modules/ControlModule.cs`: `mgr.HeartBeatStartupGrace`를 `Acs:Control:HeartBeatStartupGraceMs`(없으면 60000)로 설정 — 환경별 부팅 시간에 맞춰 appsettings로 조정 가능.
- 진단 로그: `[HB-DIAG]` 추가. 기동 1회성(`RPC_CLIENT replyQueue=`, `RPC_SERVER listening queue=`)은 Info, 매 주기(request/reply/OnRequest)는 Debug로, 응답 실패(`reply skipped: ReplyTo null`, `reply publish failed`)는 Error로. 워커 OnRequest finally에 ReplyTo null-guard 추가(누락 시 publish 건너뛰고 BasicAck는 수행).

**Why:** 재시도 타임아웃·동시성·host 리스너·상태 갱신(#24)은 모두 유효한 개선이지만 **루프의 진짜 원인은 아니었다**. 진짜 원인은 "워커가 리스너를 띄우기 전에 control이 hang으로 오판해 죽이는 경쟁(race)". 사용자가 1차 때 "기존 StartDelay(10s) 유지"를 택했으나, 진단으로 실제 부팅 시간이 ~30s임이 측정되어 전용 기동 유예(60s)가 필요해졌다. Start() 경로에 유예를 적용하면 control이 워커를 재기동하는 주 경로가 모두 커버되어 루프가 끊긴다.

**검증:** `dotnet build ACS.sln` 성공(0 오류). **런타임 검증 완료(2026-05-24 14:41 로그)**: control 기동 후 워커 부팅 ~33s(TS01_P 14:41:59 시작 → `RPC_SERVER listening` 14:42:32) 동안 초기 `RPC timeout`이 몇 번 떴으나 **`HEARTBEATFAIL_*`/Kill→Start 루프 전혀 없이**, Start()의 60s 유예 경과 후 첫 heartbeat(14:42:53)부터 `reply recv match=True`로 전환. 이후 4개 워커(TS/ES/DS/HS) 모두 ~20s 주기로 안정적으로 응답하며 재기동 없이 정상 운영(transfer/schedule 워크플로우 동작 확인). 부팅이 60s를 넘는 환경이면 `Acs:Control:HeartBeatStartupGraceMs` 상향. (운영 시 control Serilog MinimumLevel을 Information으로 두면 매 주기 `[HB-DIAG]` Debug 로그는 자동 침묵, 기동 1회성 Info만 노출.)

---

## 26. heartbeat 설정 UI 편집 기능 (Application 화면 + REST + NA_X_OPTION 영구 저장)

**날짜:** 2026-05-24
**작업:** [[25. heartbeat 루프 진짜 원인]]의 heartbeat 옵션들을 ControlModule 하드코딩이 아니라 UI에서 조회/변경/영구 저장하도록 구현. 결정: DB 영구 저장(NA_X_OPTION) + Application 관리 화면 배치 + ProcessDown/Hang 3단계 드롭다운.

**노출 설정(9개, live 객체 `ControlServerManagerImplement`):** UseHeartBeat(on/off), HeartBeatInterval, HeartBeatStartDelay, HeartBeatStartupGrace, HeartBeatTimeout, HeartBeatRetryTimeout(ms), HeartBeatRetryCount(회), HeartBeatFailWhenProcessDown/Hang(0=없음/1=상태표시만/2=재시작).

**런타임 적용 규칙:** Timeout/RetryCount/RetryTimeout/StartupGrace/Fail* 는 HeartBeatJob이 매 주기 live로 읽어 즉시 반영. Interval/StartDelay는 Quartz 트리거에 baked-in이라 변경 시 `ScheduleHeartBeats()`로 전체 트리거 재생성해야 적용(주의: `RescheduleHeartBeats()`는 누락분만 추가하므로 기존 트리거 주기를 못 바꿈). UseHeartBeat off→`UnscheduleHeartBeats()`, off→on→`ScheduleHeartBeats()`.

**변경:**
- `ACS.Core/Control/IControlServerManager.cs`: `HeartBeatStartupGrace` + `LoadHeartBeatOptions()`/`SaveHeartBeatOptions()` 선언.
- `ACS.Core/Application/IApplicationManager.cs` + `ACS.App/ApplicationManagerImplement.cs`: `GetOption(id)` public화, `SaveOption(option)`(=`PersistentDao.SaveOrUpdate`, upsert) 추가.
- `ACS.App/Control/Implement/ControlServerManagerImplement.cs`: OPT_HB_* 상수(8001~8009), `LoadHeartBeatOptions()`(행 없으면 현재값 시드, 있으면 적용)·`SaveHeartBeatOptions()`(live값 9개 upsert)·Load/Save 헬퍼.
- `ACS.App/ApplicationInitializer.cs`: control 분기 `ScheduleHeartBeat()`에서 `ScheduleHeartBeats()` 직전 `LoadHeartBeatOptions()` 호출.
- `ACS.App/Web/Controllers/AcsRestControllers.cs`: `HeartbeatSettingsController`(`api/heartbeat-settings`) GET(live값)/PUT(검증→live 적용→(un)schedule/재스케줄→`SaveHeartBeatOptions()`).
- DTO `HeartbeatSettingsDto`: `ACS.Communication/Http/Models/` + `ACS.UI/Models/` 동일 사본.
- `ACS.UI/Services/{IAcsApiService,AcsApiService}.cs`: `GetHeartbeatSettingsAsync`/`UpdateHeartbeatSettingsAsync`.
- `ACS.UI/ViewModels/AppManagementViewModel.cs`: Hb* ObservableProperty 9개(3단계=int SelectedIndex), `LoadHeartbeatSettingsAsync`(ctor 1회 — auto-refresh와 분리해 편집 중 덮어쓰기 방지)·`SaveHeartbeatSettingsAsync` 커맨드.
- `ACS.UI/Views/AppManagementView.axaml`: Properties 컬럼을 `Auto,*,Auto,Auto`로 분할, 하단 "Heartbeat 설정" 섹션(ToggleSwitch/NumericUpDown/3단계 ComboBox/저장·불러오기 버튼).

**Why:** 운영/튜닝 중 재빌드·재배포 없이 heartbeat 동작 조정. 영구 저장은 기존 NA_X_OPTION 인프라 재사용(Id 8001~8009, trans 1xxx~7xxx과 대역 분리). 기존 패턴(ApplicationsController·AcsApiService·BayEditWindow 입력 그리드) mirror.

**검증:** `dotnet build ACS.sln` 성공(0 오류). 런타임 검증 필요(`D:\ACS\deploy\CS01_P`에 ACS.App/ACS.Communication/ACS.Core dll, UI 실행 위치에 UI 산출물 재배포 후): Application 화면 "Heartbeat 설정"에 현재값 로드 → 변경·저장 시 즉시 반영 + DB 8001~8009 생성/갱신 → control 재시작 후 유지. NumericUpDown(decimal?)↔long/int 바인딩 빌드 0 오류.

## 27. DB 로깅(NA_L_LOGMESSAGE) 활성화 — 휴면 상태였던 기존 경로를 비동기 큐로 기동

**날짜:** 2026-05-24
**작업:** WARN 이상 ACS 도메인 로그를 `NA_L_LOGMESSAGE`에 적재. 조사 결과 DB 로깅 인프라가 이미 구현되어 있었으나 휴면 상태였고(새 Serilog sink 불필요), 이를 "켜는" 방식으로 처리. 결정: ACS 도메인 로그만 / WARN 이상 / 비동기 백그라운드 큐.

**휴면 원인(3가지):** ① `Logger.GetLogger()`(static 팩토리)가 `logManager`를 연결 안 함 → `AbstractManager/Service`의 logger 전부 `logManager==null`(통신 클래스 `AbstractRabbitMQ/Highway101`만 수동 연결). ② `LogManagerImpl.UseAdoDotNetAppender` 기본 false → DB 저장 스킵. ③ `SkipLoggingMessages` null(NRE 위험), `LogLevel` 미설정.

**중요 사실:** `AcsDbContext.cs:18` `using ACS.Core.Logging.Model;` 때문에 `DbSet<LogMessage>`/`Entity<LogMessage>`(775행)는 **`ACS.Core.Logging.Model.LogMessage`**를 매핑(=Logger/LogManagerImpl이 쓰는 타입). `Database.Model.Logging.LogMessage`는 미사용 중복 클래스.

**변경:**
- `ACS.Core/Logging/Logger.cs`: `static ILogManager DefaultLogManager` 추가. `logManager`를 getter-폴백 프로퍼티로 변경(`_logManager ?? DefaultLogManager`) — DefaultLogManager 주입 전 생성된 logger도 주입 후 DB 경로 사용.
- `ACS.Core/Logging/Implement/LogManagerImpl.cs`: `Channel<LogMessage>` 비동기 큐(Bounded, `FullMode=DropWrite`). `CreateLogMessage(...)` 2개를 동기 `PersistentDao.Save` → `Enqueue()`로 전환. `Start()`(소비자 Task, BatchSize 드레인→`SaveAll`)/`Flush()`(Writer.Complete+대기) 추가. `PrepareForPersist()`로 text 분할 로직 이동(+기존 `Substring(start,end)` 길이 버그 수정→`Min(fieldSize, size-start)`). `CreateLargeLogMessageInstance` `NotImplementedException` 수정. `SkipLoggingMessages` null 가드.
- `ACS.Core/Logging/ILogManager.cs`: `Start()`/`Flush()` 선언 추가.
- `ACS.Core/Base/Interface/IPersistentDao.cs` + `ACS.App/Database/EfCorePersistentDao.cs`: `SaveAll(ICollection)` 추가(단일 DbContext AddRange+SaveChanges 1회, Save와 동일 retry).
- `ACS.App/Modules/CoreModule.cs`: `LogManagerImpl` 등록을 `.PropertiesAutowired()`→명시적 `Register(c=>...)`로 교체. `Acs:Logging:Database`(Enabled/Level/QueueCapacity/BatchSize) 주입, `UseAdoDotNetAppender=enabled`, `LogLevel`(기본 WARN), `SkipLoggingMessages=new ArrayList()`, `UseShortClassNameAtOperationName=true`.
- `ACS.App/appsettings.json`: `Acs:Logging:Database` 섹션 추가(Enabled=true/Level=WARN/QueueCapacity=10000/BatchSize=200).
- `ACS.App/Executor.cs`: `OnContainerBuilt`의 **DB 스키마 초기화 직후**(연결 문자열 캐싱 보장)에 `Logger.DefaultLogManager` 주입 + `logManager.Start()`. `Stop()`에서 hosted service 종료 후 `Flush()`.

**Why:** ① DB 저장은 `LogManager.LogLevelInt`로 제어되어 파일/콘솔 Serilog 설정과 독립(파일/콘솔=MinimumLevel Debug 유지, DB만 WARN+). ② `Debug`는 `Logger.Debug`가 `SaveMessageToDatabase`를 호출하지 않아 어떤 경우에도 DB 미저장. ③ Start를 DB 스키마 init 이후로 둔 이유: `EfCorePersistentDao.NewDb()`는 파라미터 없는 `AcsDbContext()`→static `_cachedConnectionString` 사용, 이 캐시는 config 포함 생성(=EnsureCreated 시) 시점에 채워지므로 그 이전 소비자 쓰기는 localhost 폴백 위험. 그 전 startup 로그는 DefaultLogManager=null이라 DB 미기록(파일/콘솔엔 남음)으로 안전 스킵. ④ 모든 프로세스가 공통 `CoreModule`+`OnContainerBuilt`를 거쳐 일괄 적용.

**검증:** `dotnet build ACS.sln` 성공(0 오류). 런타임 검증 필요: 프로세스 기동 후 WARN/ERROR 유발 → `SELECT time,"logLevel","operationName",text,"carrierName","machineName" FROM public."NA_L_LOGMESSAGE" ORDER BY time DESC LIMIT 20;`로 Warning/Error/Fatal 행만·도메인 컬럼 채워짐 확인. Info/Debug는 DB 미존재(파일엔 존재)·다량 WARN 버스트 시 호출 스레드 정체 없음·종료 시 잔여 flush 확인.

## 28. DB 로깅 런타임 크래시 수정 — ChangeCommunicationMessageName null-key + 조용한 실패 제거

**날짜:** 2026-05-25
**작업:** [[27. DB 로깅 활성화]] 후 DB에 로그가 전혀 안 들어가던 문제. 사용자 제공 스택 트레이스로 원인 확정 후 수정.

**근본 원인:** `LogManagerImpl.ChangeCommunicationMessageName`가 `UseFriendlyCommunicationMessageNames.Contains(communicationMessageName)` 호출 → 이 컬렉션은 `Dictionary`(IDictionary)이고 일반 로그는 `CommunicationMessageName==null` → `Dictionary.Contains(null)`이 `ArgumentNullException` throw. `CreateLogMessage`의 `catch{Console.WriteLine}`에만 잡혀 **거의 모든 로그가 enqueue 전에 조용히 버려짐**. 원래 있던 잠재 버그가 #27의 경로 활성화로 드러남(과거엔 휴면이라 호출 안 됨). 참고: `SkipLoggingMessages`는 `ArrayList`라 `Contains(null)`이 예외 없음 → 그래서 그 가드만으론 못 막았음.

**변경(`LogManagerImpl.cs` + `Executor.cs`):**
- `ChangeCommunicationMessageName`: `string.IsNullOrEmpty(name) || dict==null`이면 즉시 return (블로커 수정).
- 오류 가시성: 모든 `catch`의 `Console.WriteLine` → `LogInternalError()`(=`Serilog.Log.ForContext(...).Error`). **무한루프 방지 위해 ACS `Logger` 래퍼 절대 사용 금지**(Serilog 파이프라인은 LogManager 미호출이라 안전).
- varchar 초과 방지: `PrepareForPersist` 진입 시 `NormalizeLengths()` — operationName→128, logLevel→20, 그 외 문자열 컬럼→64로 truncate. (한 행만 초과해도 `SaveAll`의 SaveChanges가 배치 전체 롤백되던 다음 단계 silent 실패 차단. operationName은 `UseShortClassNameAtOperationName=true`라 클래스FQN.메서드라 길어질 수 있음)
- 테이블 안전망: `Executor.MigrateLogMessageTable()` 추가(`CREATE TABLE IF NOT EXISTS NA_L_LOGMESSAGE/NA_L_LARGELOGMESSAGE`, 기존 `MigrateXxxTable` 패턴). `OnContainerBuilt` DB init try에서 `MigrateMqttTable` 다음 호출. `EnsureCreated()`는 기존 DB엔 테이블 안 만들어서 구 DB 대비.

**교훈:** 휴면 코드 경로를 활성화할 때 그 경로의 모든 헬퍼에 잠복한 null-guard 결함이 한꺼번에 드러남. catch에서 `Console.WriteLine`로 삼키면 서비스 환경에서 진단 불가 → 로깅 인프라 자신의 실패는 반드시 별도 sink(Serilog 파일)로 가시화.

**검증:** `dotnet build ACS.sln` 성공(0 오류). 런타임 재검증 필요: ACS.Core.dll(1·2·3) + ACS.App.dll(4) 재배포·재시작 후 WARN/ERROR 유발 → `NA_L_LOGMESSAGE` 적재 확인. 남은 문제 시 이제 logs 파일에 실제 원인 출력됨.

## 29. DB 로그 컨텍스트 자동 보강 (messageName/carrier/command/machine/unit/transactionId)

**날짜:** 2026-05-25
**작업:** [[27]]~[[28]]로 DB 로깅은 되지만 약 65% 로그가 `logger.Warn("text")` 평문이라 컨텍스트 컬럼이 비어 있던 문제. 모든 호출부 수정 대신 **ambient(AsyncLocal) 컨텍스트 + 빈 필드 자동 보강** 방식.

**핵심 설계 근거:**
- 메시지→워크플로우 진입 단일 choke point = `GenericWorkflowRabbitMQListener.ExecuteWorkflow(...)`/`OnJsonMessage(...)`.
- Elsa 실행(`ElsaWorkflowManagerBridge.RunElsaWorkflow`)이 `RunAsync(...).GetAwaiter().GetResult()`로 **동기 실행**(Task.Run/SuppressFlow 없음) → 진입 직전 설정한 AsyncLocal이 활동·서비스 깊은 곳까지 ExecutionContext로 전파됨.
- **AsyncLocal은 읽는 스레드에서만 유효** → 보강은 반드시 로그 생성 스레드(`CreateLogMessage`)에서. 백그라운드 큐 소비자 스레드(`PrepareForPersist`/`ConsumeAsync`)에서 읽으면 무효(이게 가장 헷갈리는 함정).

**변경:**
- 신규 `ACS.Core/Logging/LogContext.cs`: `LogContextData` POCO + `AsyncLocal` 기반 `LogContext.Push(data)`(IDisposable, 이전값 복원 중첩 안전)/`Current`. (기존 `Compat/CallContext`는 문자열 키라 부적합 → 전용 타입.)
- `LogManagerImpl.cs`: `EnrichFromAmbientContext(m)` — `LogContext.Current`로 7개 필드 중 **빈 것만** 채움(명시적 컨텍스트 오버로드 값 우선 보존). `CreateLogMessage(LogMessage,...)`/`CreateLogMessage(LogEvent)` 양쪽 `Enqueue` 직전(=producer 스레드)에서 호출, `ChangeCommunicationMessageName`보다 앞.
- `GenericWorkflowRabbitMQListener.cs`: `BuildLogContext(tid, name, obj)` — tid/name/commMsgName 기본 + `obj`가 `AbstractMessage`면 machine/unit, `BaseMessage`/`TransferMessageEx`면 carrier/command(모두 `ACS.Core.Message.Model`, 둘 다 `AbstractMessage` 상속). `ExecuteWorkflow`(XML/typed)·`OnJsonMessage`의 `workflowManager.Execute(...)`를 `using LogContext.Push(...)`로 감쌈.

**범위 외:** Host TCP·Quartz 스케줄 등 비-RabbitMQ 경로는 컨텍스트 없으면 기존처럼 비움(best-effort). 동일 패턴으로 확장 가능.

**검증:** `dotnet build ACS.sln` 성공(0 오류). 런타임: ACS.Core.dll + ACS.Communication.dll + ACS.App.dll 재배포·재시작 후 메시지 처리 중 WARN/ERROR 유발 → `NA_L_LOGMESSAGE`의 messageName/transactionId/carrier/command/machine/unit 채워짐 확인. 명시적 컨텍스트 로그는 값 유지(보강이 안 덮어씀), 비메시지 로그는 빈 컨텍스트로도 정상 적재.

## 30. DB 저장 레벨 INFO 하향 + 통신 메시지(MES↔host, 앱 간 MSB) 전체 DB 로깅

**날짜:** 2026-05-25
**작업:** (a) DB 저장 임계값을 WARN→INFO로 하향. (b) 애플리케이션 간 송수신 메시지와 MES↔host 메시지를 모두 NA_L_LOGMESSAGE에 적재. 결정: heartbeat/telemetry 제외, 전체 본문 저장.

**(a) 레벨 하향:** `CoreModule.cs`의 `Level` 기본값 `"WARN"`→`"INFO"` + `appsettings.json` `Acs:Logging:Database:Level` `"INFO"`. 근거: `LogManagerImpl.LogLevelInt`(WARN=30000)가 INFO(20000)를 제외했음. 주의: `Logger.Debug`는 `SaveMessageToDatabase` 미호출이라 Debug는 레벨 무관 영구 제외. **배포 주의**: deployed appsettings에 섹션 없으면 코드 기본값 적용(DLL만 재배포로 OK), `Level:"WARN"` 명시돼 있으면 그 파일도 수정 필요(deployed config가 코드 기본값을 덮음).

**(b) 통신 메시지 로깅 — 현황:** 수신은 이미 INFO로 적재 중이었으나(`AbstractRabbitMQListener`의 `logger.Info("received message...")`, MES→host는 `HostBridgeService.cs:59` 전체 본문), **송신은 `GenericRabbitMQSender.Send/Request`의 로깅이 전부 주석 처리**되어 누락. 현재 MSB=rabbitmq라 Tibrv/Highway101은 범위 밖.

**변경:**
- `AbstractRabbitMQ.cs`(공유 베이스): telemetry 집합 `_telemetryMessageNames` + `IsTelemetryJsonMessage`(substring) + `IsTelemetryName`(정확일치)를 **베이스로 이동**(송수신 공용), `LogCommMessage(direction, payload, peer, commMsgName)` 추가 — telemetry는 `logger.Debug`(DB 미적재), 그 외는 `logger.Well(text,"",msgName,"","","","",msgName)`(INFO 적재, MessageName/CommunicationMessageName 설정, 빈 컨텍스트는 [[29]]의 ambient LogContext가 보강). try/catch로 통신 보호.
- `GenericRabbitMQSender.cs`: 3개 `Send(...)`의 주석 로깅 → `LogCommMessage("SENT→",...)`. RPC `string Request(430)`에 `RPC-REQ→`/`RPC-REP←` 로깅(XmlDocument Request 계열은 모두 여기로 위임).
- `AbstractRabbitMQListener.cs`: 중복 telemetry 정의 제거(베이스 사용). 수신 INFO 로그(XML 3곳·AbstractMessage 2곳)를 `LogCommMessage("RECV←",...)`로 교체(AbstractMessage 분기는 이름만→전체 본문). **OnRequest의 JSON 수신 로그(`RPC received JSON...`)도 교체** — heartbeat RPC가 INFO로 새던 것을 telemetry 강등으로 차단.
- `HostMessageService.cs` `SendToHost`: 이름+크기만 → `[SENT→MES]` + 전체 XML 본문.

**효과:** 한 메시지가 송신측엔 `[SENT→]`/`[RPC-REQ→]`, 수신측엔 `[RECV←]`/`[RPC-REP←]`로 각 프로세스에 적재(processName 구분). telemetry(CONTROL-HEARTBEAT/RAIL-VEHICLEHEARTBEAT/SCHEDULE-CHECKVEHICLES 등)는 송수신 양쪽 Debug 강등으로 DB 제외. 대용량 본문은 text(4000) 초과 시 LargeLogMessage 분할.

**검증:** `dotnet build ACS.sln` 성공(0 오류). 런타임: ACS.Communication.dll + ACS.App.dll 재배포·재시작 후 `SELECT "processName","communicationMessageName",left(text,80) FROM "NA_L_LOGMESSAGE" WHERE text LIKE '[SENT%' OR text LIKE '[RECV%' OR text LIKE '[RPC%' OR text LIKE '[SENT→MES]%' ORDER BY time DESC;` → 송수신 적재 확인, heartbeat/telemetry 부재 확인.

## 31. ACS.UI 로그 조회 화면 (NA_L_LOGMESSAGE / NA_L_LARGELOGMESSAGE)

**날짜:** 2026-05-25
**작업:** ACS.UI에 로그 조회 화면 구현 — 시간 범위 + 필터([[30]]에서 적재된 로그 대상) 조회. 시간은 **클라이언트 로컬 입력 → UTC 변환 전송 → 응답 UTC → 로컬 표시**. 배치: 팝업 창(DataView/HostComm 패턴), 필터: 시간+Level+Keyword(Text)+Process+MessageName+TransactionId, 대용량 메시지 상세 보기 포함.

**아키텍처:** ACS.UI는 DB 직접 접근 안 함 → HTTP API(`AcsApiService`, :5100, control 프로세스 Kestrel) 경유. 신규 `LogsController`는 로그 전용 Manager가 없어 **`AcsDbContext`를 직접 주입**(Autofac `AsSelf().InstancePerLifetimeScope()`)해 `IQueryable` LINQ로 시간범위+필터+정렬+limit 구성(DAO `IPersistentDao`엔 범위 조회 메서드 없음).

**시간/UTC 핵심:** `Program.cs:23` `Npgsql.EnableLegacyTimestampBehavior=true` + `time` 컬럼 `timestamptz`(`Executor.cs:654,664`) + DAO `NormalizeDateTimeProperties`가 저장 직전 UTC 정규화. 변환은 **클라이언트 컴퓨터 기준**: UI에서 로컬 From/To→`ToUniversalTime()`→ISO "o" 전송, 표시는 `LogRow.LocalTime = dto.Time?.ToLocalTime()`. 컨트롤러는 `from/to`를 `DateTimeOffset.Parse(...).UtcDateTime`로 파싱, 반환 Time은 Kind에 무관하게 UTC 정규화(`ToUtc()`, legacy read Kind 방어).
- **부수 수정:** `LogManagerImpl.cs:105` `logEvent.Timestamp.DateTime`(Kind=Unspecified=로컬 벽시계 → DAO가 변환 없이 UTC 라벨링 = 오기록) → **`.UtcDateTime`**. Serilog 경로 신규 로그만 정합, 기존 적재분은 소급 안 됨(알려진 한계).

**주의 — 엔티티 네임스페이스 중복:** `LogMessage`/`LargeLogMessage`/`PartitionedEntity`가 `ACS.Core.Database.Model.Logging`(+`...Base`)와 `ACS.Core.Logging.Model`(+`ACS.Core.Base`) **두 곳**에 존재. `AcsDbContext`의 DbSet은 후자(`ACS.Core.Logging.Model`)를 매핑하므로 컨트롤러 LINQ도 반드시 후자 using. (전자로 작성 시 CS0266.)

**신규 파일:** `ACS.Communication/Http/Models/LogMessageDto.cs`(공유), `ACS.UI/Models/LogMessageDto.cs`(미러)+`LogQueryFilter.cs`, `ACS.UI/ViewModels/LogViewModel.cs`(+`LogRow`), `ACS.UI/Views/LogView.axaml`(+`.cs`), `ACS.UI/Converters/LogLevelToColorConverter.cs`.
**수정:** `AcsRestControllers.cs`(`LogsController`: `GET /api/logs` 필터+범위, `GET /api/logs/{id}/text` = LargeLogMessage Sequence순 재조합), `IAcsApiService`/`AcsApiService`(`GetLogsAsync`/`GetLogTextAsync`+`BuildLogQuery` 로컬→UTC), `MainWindowViewModel`(LogViewModel+OpenPopupView "Log"+`OpenLogCommand`+오픈 시 1회 조회), `MainWindow.axaml`(Log 탭 플레이스홀더 → Log Viewer 버튼).

**검증:** `dotnet build ACS.sln` 성공(0 오류, XAML 컴파일 포함). 런타임 재검증 필요(PostgreSQL+control 프로세스+GUI): `GET /api/logs?limit=20`의 time이 UTC(Z) 직렬화 확인 → UI Log Viewer에서 시간/레벨/키워드/프로세스/메시지명/TxId 필터 + TIME 컬럼 로컬 표시 + 행 선택 시 상세 패널 전체 메시지(4000자 초과 재조합) + Auto-Refresh(5초) 확인.

---

## 32. SignalR 차량 POSE 텔레메트리 수신 사양서 작성

**날짜:** 2026-05-25
**작업:** 차량 POSE(X,Y,Angle) 실시간 수신 경로(SignalR)의 사양을 `docs/signalr_pose_status.md`로 정리(문서만 작성, 코드 변경 없음).

**정리된 흐름:** Trans 프로세스가 `RAIL-VEHICLEUPDATE`를 RabbitMQ fanout(`/VM/DEMO/UI/SENDER`)로 발행 → ACS.App `PoseTelemetrySubscriber`(BackgroundService, `RabbitMQ.Client` 직접 사용, 워크플로우 우회)가 구독 → `PoseX/PoseY` 존재 시 camelCase 페이로드로 `IHubContext<VehicleHub>.Clients.All.SendAsync("PoseUpdate", …)` 브로드캐스트(`/hubs/vehicle`) → ACS.UI `VehicleHubClient.On<PoseUpdateDto>("PoseUpdate")` → `PoseUpdated` 이벤트 → `App.axaml.cs`가 `Dispatcher.UIThread.Post` 마샬링 → `MapViewModel.ApplyPoseUpdate`(VehicleId 우선, CommId fallback, OrdinalIgnoreCase 매칭) → `DataChanged` 맵 리렌더.

**핵심 사실:** 서버→클라 단방향, 이벤트명 `"PoseUpdate"` 단일. `VehicleHub`은 빈 Hub(발행은 `IHubContext`로). `WithAutomaticReconnect`. `MapViewModel.UpdateVehicles`가 REST 갱신 시 기존 실시간 POSE 머지(깜빡임 방지). `App.axaml.cs:44-45`에 `[TEMP DEBUG]` Console 로그 잔존(정리 대상). 동일 패턴 부 채널 `HostCommHub`(`/hubs/hostcomm`, `"Log"`/`"Connection"`)도 존재.

---

## 33. SignalR 차량 텔레메트리 확장 — POSE만 → POSE + 상태(배터리/노드/런상태/연결)

**날짜:** 2026-05-25
**작업:** CS→ACS.UI SignalR 푸시를 POSE 전용에서 RAIL-VEHICLEUPDATE의 상태 필드까지 포함하도록 확장. 송신(TS) 측은 무변경 — TS는 이미 원본 JSON 전체를 fanout으로 forward 중이라 상태 정보가 CS까지 다 도착해 있었고, CS의 `PoseTelemetrySubscriber`가 POSE만 추출하던 것이 병목이었다.

**확정 범위(사용자 선택):** ①RAIL-VEHICLEUPDATE 필드만(알람 제외 — 알람은 별도 메시지 RAIL-VEHICLEALARM이고 UI fanout으로 forward 안 됨), ②맵(MapView)만(차량 목록 그리드 제외 — `VehicleDto`가 INotifyPropertyChanged 미구현이라 그리드 in-place 갱신 불가).

**변경:**
- `PoseTelemetrySubscriber.cs`: 이벤트명 `"PoseUpdate"`→`"VehicleUpdate"`, `PoseX/PoseY==null` 조기 드롭 제거(`Data==null`만 가드), 페이로드에 `runState/batteryRate/batteryVoltage/currentNodeId/vehicleDestNodeId/connectionState` 추가 + POSE는 nullable로 전송. (클래스명·DI 등록은 유지)
- UI: `PoseUpdateDto`→`VehicleUpdateDto`(POSE nullable + 상태 필드), `VehicleHubClient` `PoseUpdated`→`VehicleUpdated`/`.On("VehicleUpdate")`, `App.axaml.cs`는 `ApplyVehicleUpdate(dto)` 호출(임시 `[TEMP DEBUG]` 로그 제거), `MapViewModel.ApplyPoseUpdate`→`ApplyVehicleUpdate`(상태는 항상 머지·문자열은 빈값이면 미덮어씀·`CurrentNodeId`는 노드변경 시에만 채워지므로 빈값 클리어 방지, POSE는 `HasValue`일 때만 갱신).

**한계(범위 밖):** 알람/`State`/`ProcessingState`/`TransferState`는 메시지에 없어 여전히 REST 새로고침 시에만 갱신. 차량 목록 그리드도 REST 유지.

**검증:** `dotnet build ACS.sln` 0 오류(경고 172, 기존). 런타임 재검증 필요: control 로그 `VehicleUpdate broadcast ...` + ACS.UI 맵에서 수동 새로고침 없이 배터리 바/호버 팝업(RunState/Battery/Node/Connection) 1Hz 갱신, DISCONNECT 시 차량 회색 전환, POSE 없는 상태 메시지에 위치 유지(0,0 안 튐).

**문서:** `docs/signalr_pose_status.md` 갱신(제목·이벤트명·페이로드·한계 반영).

---

## 34. 로그 자동 삭제(7일 보존) — 파일 로그 잡 복구 + DB 시간 파티셔닝 전환

**날짜:** 2026-05-25 ~ 2026-05-26
**작업:** "7일마다 로그 삭제가 돌고 있냐"는 확인에서 출발. 파일/DB 로그 모두 정리가 사실상 미작동이라 7일 보존 도입. DB는 처음 단일 DELETE 잡으로 갔다가 "대량 한 방 DELETE면 DB 느려짐" 우려로 **시간 파티셔닝 재설계**로 선회.

**진단(작업 전):**
- 파일 로그(`logs/{processId}-.log`): `AwakeDeleteLogJob` 존재하나 ①등록 주석, ②daemon 전용 블록, ③설정 키 오타(`Acs:Database:LogDeleteDays` vs `Acs:LogDeleteDays`)로 `int.Parse(null)` 예외 → 미작동. Serilog `retainedFileCountLimit`도 없음.
- DB 로그(`NA_L_LOGMESSAGE`/`NA_L_LARGELOGMESSAGE`): #27 이후 적재만, 정리 전무. `time` 인덱스도 없어 DELETE 시 풀스캔. → 단일 대량 DELETE는 긴 락/WAL/bloat 위험.

**변경 — 파일 로그(완료, 유지):**
- `AwakeDeleteLogJob.cs`: 설정 키 `Acs:LogDeleteDays`로 수정, `int.Parse`→`int.TryParse`+기본 7 가드(`delLog()`에서 1회 계산→`check(DirectoryInfo,int)`).
- `SchedulingModule.cs`: 파일 잡을 daemon 블록 밖으로 빼 **전 프로세스** 등록(각자 `logs/` 정리).

**변경 — DB 로그(시간 파티셔닝):**
- 두 로그 테이블을 `time` 일별 RANGE 파티션으로 전환. 보존 만료분은 `DROP TABLE`로 즉시 제거(스캔/bloat 없음). DB는 **PostgreSQL 17**(`docker-compose.yml:19`)이라 선언적 파티셔닝 완전 지원.
- **PK 없음 결정**: PG는 파티션 키(`time`)를 PK에 포함 요구하나 `time`은 nullable 공유 베이스(`PartitionedEntity`, 다수 `NA_H_*`가 의존)라 비-nullable화 위험 → 로그 테이블만 PK 제거(`id`는 Guid라 사실상 유일). **EF는 런타임에 DB PK를 검증 안 하므로 `AcsDbContext` 무변경**(`HasKey(x=>x.Id)` 유지, INSERT/뷰어 정상). 대신 `("time")`(large는 `logMessageId`도) 인덱스 추가.
- `docker/init/01_init_acsdb.sql`: 두 테이블 `PARTITION BY RANGE ("time")` + `_pdefault` + 인덱스, 두 `NA_L_*` PK 제약 제거.
- `Executor.cs` `MigrateLogMessageTable` 재작성 → `ConvertLogTableToPartitioned`: 기동 시 `pg_advisory_xact_lock`(전 프로세스 동시 실행 직렬화) + `pg_class.relkind` 가드. `'p'`면 일별 파티션 보장만, 아니면 부모+DEFAULT+인덱스 생성하고 `'r'`이면 rename→생성→`min(time)`~오늘+3일 일별 파티션→명시 컬럼 INSERT 복사→old DROP. 오늘..+3 파티션은 매 기동 보장(per-day `BEGIN/EXCEPTION`로 DEFAULT 겹침 시에도 계속). 경계는 `SET LOCAL timezone='UTC'`로 UTC.
- 신규 `AwakeLogPartitionMaintenanceJob.cs`(기존 `AwakeDeleteDbLogJob.cs` 대체·삭제): 매일 03:00, 오늘~+3일 `CREATE TABLE IF NOT EXISTS ... PARTITION OF`(오늘 파티션은 전날 미리 생성돼 자정 직후 DEFAULT 누수 방지), `(days+1)~(days+40)`일 전 `DROP TABLE IF EXISTS`. `IPersistentDao.ExecuteUpdate`, 문장별 try/catch. control 단독 등록.

**핵심 사실:** `SchedulingModule`은 `Executor.cs:164`에서 전 프로세스 등록. IHostedService는 콘솔 호스트(`OnContainerBuilt(true)`)+control 웹호스트(Generic Host) 모두 기동. 보존 일수 단일 키 `Acs:LogDeleteDays`(=7) 파일/DB 공용. 두 로그 테이블 간 FK 없음. `WorkflowLog`/`SaveToDatabase`는 EF 관례 매핑(PascalCase). 파티션명 `"<부모>_pYYYYMMDD"`(UTC), DEFAULT는 절대 DROP 안 함.

**검증:** `dotnet build ACS.App.csproj` 0 오류. 런타임 재검증 필요: ①파일 — 8일 전 더미 `*.log` 생성 후 삭제·7일 내 보존. ②기존 DB 변환 — 구 비파티션 테이블+데이터로 기동 시 1회 변환(`relkind='p'`, 행수 일치, `_old` 삭제), 재기동 no-op, 동시 2프로세스 락 경합. ③삽입→정확 `_pYYYYMMDD` 파티션(null/이상치는 `_pdefault`). ④뷰어 `GET /api/logs?from=…` 정상 + `EXPLAIN` 프루닝. ⑤보존 — 만료 파티션만 `DROP`(즉시), `_pdefault`/최근 보존. ⑥신규 설치(docker) — `\d+`에 `Partition key: RANGE("time")`, PK 없음, `time` 인덱스.

---

## 35. 서버 이전용 마스터 데이터 이관 도구 보완 (스키마 전용 init + NA_C_NIO)

**날짜:** 2026-05-26
**작업:** "ACS 서버 이전 시 현재 DB 기준정보(NA_R_VEHICLE/NODE/LINK/존 등)를 모두 이전할 실행 가능한 SQL을 만들어달라. init.sql은 이전 데이터인 것 같다"는 요청. 원본=Docker 컨테이너, 대상=동일 docker-compose, 방식=기존 스크립트 활용·보완으로 확정.

**진단(핵심 사실):**
- `docker/init/01_init_acsdb.sql`은 과거 전체 pg_dump(스키마 + 모든 테이블 `COPY` 데이터). 안의 데이터는 **DEMO 사이트**(노드 N001~N012, 차량 AMR001 1대, 192.168.1.x 데모 location). 컨테이너 최초 기동 시 자동 적재되므로 신규 서버 init으로 부적합 = 사용자가 말한 "이전 데이터".
- 이미 `docker/scripts/backup-master.ps1`/`restore-master.ps1`(커밋 `20d662b`) 존재. `pg_dump --data-only --column-inserts --disable-triggers`로 마스터 테이블만 추출 → `psql -1 -v ON_ERROR_STOP=1`로 단일 트랜잭션 복원. 시퀀스는 dump의 `setval()`로 동기화.
- 데이터는 **반드시 라이브 DB에서 추출**(손작성 정적 SQL 불가). 로컬엔 `pg_dump`/`psql` 없고 Docker만 → 컨테이너 `docker exec`로 수행.

**변경:**
- `backup-master.ps1`/`restore-master.ps1`: 마스터 목록에 **`NA_C_NIO` 추가**(인터페이스 정의 = 사이트 설정. `remoteIp`/`machineName`은 이관 후 수정 필요). MQTT `brokerIp`와 동일 성격.
- 신규 `backup-schema.ps1`: 라이브 DB `pg_dump --schema-only --no-owner --no-privileges` → `acs-schema-<ts>.sql`. 신규 서버의 `docker/init/01_init_acsdb.sql`로 사용 → 빈 현재 스키마로 기동(라이브 스키마라 02/03 마이그레이션 이미 반영 = 별도 마이그 불필요). `backup-master.ps1`와 동일 docker cp/exec + DryRun 패턴.
- `README.md`: 시나리오 A를 "구→신 서버 클린 이관"으로 재작성(backup-schema → backup-master → 신규 서버 schema-only init → restore -Truncate), 스크립트 표/주의문 추가.

---

## 36. TS→CS forward에 차량 권위 상태 스냅샷 추가 (ProcessingState 등 실시간화)

**날짜:** 2026-05-27
**작업:** "TS가 CS로 포워딩할 때 ProcessingState/CurrentNodeId 등을 조회해 함께 보낼 수 없냐"는 요청. #33이 "TS는 원본 JSON 전체를 보내므로 상태가 다 도착해 있다"고 했으나, 그건 **EI 원본에 있는 필드만** 해당. `ProcessingState`/`State`/`TransferState`와 작업 완료 시 클리어되는 `AcsDestNodeId`/`TransportCommandId`/`Path`는 **EI 원본에 없고 Trans가 워크플로우에서 계산**하는 값이라 UI엔 REST 새로고침 때만 반영됐다. 또 `currentNodeId`는 EI가 노드 변경 시에만 채워 상태-only 메시지엔 비어 있었다.

**핵심 통찰:** `RailVehicleUpdateActivity`는 이미 `vehicle`(VehicleEx)을 메모리에 들고 활동 내내 DB 갱신과 함께 권위값을 sync(`vehicle.RunState/CurrentNodeId/ProcessingState/...`)해 둔다. **forward 직전(Step 10)에 그 권위값으로 메시지를 채워 보내면** 추가 DB 조회 없이 실시간 전달 가능.

**확정 범위(사용자 선택):** ①전체 상태 스냅샷(AlarmState만 제외 — RAIL-VEHICLEALARM 전용 경로), ②값 출처 = 메모리 vehicle 객체(재조회 없음).

**변경:**
- `RailVehicleUpdateMessage.cs`(`RailVehicleUpdateData`): 신규 필드 `processingState/state/transferState/acsDestNodeId/transportCommandId/path` 추가(camelCase). `currentNodeId/runState/...`는 기존.
- `RailVehicleUpdateWorkflow.cs` Step 10: `ForwardToUi(accessor, json)` → forward 직전 `data`의 상태 필드를 `vehicle` 권위값으로 덮어쓴 뒤 `JsonSerializer.Serialize(updateMessage)` 전달. **POSE/배터리는 EI 원본 유지.** `currentNodeId`는 노드변경 여부 무관 항상 `vehicle.CurrentNodeId`.
- `PoseTelemetrySubscriber.cs`: SignalR payload에 신규 6필드 추가. 진단 로그에 `proc/tc` 추가.
- UI `VehicleUpdateDto.cs`: 신규 6필드 + 주석 갱신. `VehicleDto`는 이미 보유 → 무변경.
- `MapViewModel.ApplyVehicleUpdate`: merge 2갈래 — `ProcessingState/State/TransferState`는 null 아니면 반영, **`AcsDestNodeId/TransportCommandId/Path`는 빈 값도 직접 대입(완료 시 "" 클리어 반영)**. 기존 필드 머지 로직은 회귀 방지 위해 유지.

**한계(범위 밖):** SignalR는 `MapViewModel._vehicles`에만 머지 → 맵/호버 팝업만 실시간. 차량 그리드(`VehicleViewModel`/`VehicleListViewModel`)는 `VehicleDto`가 INotifyPropertyChanged 미구현이라 여전히 REST 새로고침 유지. AlarmState는 RAIL-VEHICLEALARM 경로 그대로.

**검증:** `dotnet build ACS.sln` 확인. 런타임 재검증 필요: EI→TS 흐름에서 control 로그 `VehicleUpdate broadcast ... proc=... tc=...`에 새 값 채워짐, ACS.UI 맵 호버에서 충전 노드 도착 시 ProcessingState CHARGE, 배터리≥15%에서 IDLE, 충전잡 완료 시 TransportCommandId 비워짐, POSE 위치/회전 회귀 없음.

**문서:** `docs/signalr_pose_status.md` 갱신(개요·흐름도·DTO/payload/모델 표·머지 규칙·한계 반영).

**이관 절차:** 구 서버 `backup-schema.ps1` + `backup-master.ps1 -IncludeApplication` → 신 서버에 schema 파일을 init으로 배치 후 `docker-compose up -d` → `restore-master.ps1 -InputFile ... -Truncate` → `appsettings.json`/MQTT/NIO 사이트 종속 값 수정.

**핵심 사실:** Windows PowerShell 5.1은 BOM 없는 `.ps1`을 ANSI(CP949)로 읽어 UTF-8 한글이 깨짐 → `docker/scripts/*.ps1`은 **UTF-8 BOM 필수**(Write 도구는 무 BOM 저장하므로 생성 후 BOM 재인코딩). 백업 제외: `NA_T_TRANSPORTCMD`, `NA_T_CURRENTINTERSECTION`, `NA_A_ALARM`, 차량 런타임(`NA_R_VEHICLE_IDLE/ORDER/CROSS_WAIT`), `NA_H_*`, `NA_L_*`/`NA_Q_*`/`NA_U_*`.

**검증:** 세 `.ps1` 파싱 OK(`Parser::ParseFile`), `backup-schema.ps1` UTF-8 BOM 확인. **미완료(Docker Desktop 미기동)**: 실제 산출물 생성·DryRun·복원 검증은 컨테이너 기동 후 필요. 신규 서버에서 `NA_R_NODE`에 데모 N001~N012가 아니라 현재 노드만, `NA_R_VEHICLE`에 AMR001 아닌 실제 차량 확인, 앱 기동 시 맵 레이아웃 로드 확인.

---

## jobId / transportCommandId 컬럼 길이 256 확장

**날짜:** 2026-06-05
**작업:** MES JobID 75자 수신 → `NA_T_TRANSPORTCMD.jobId varchar(64)` 초과로 `SaveChanges` 실패 → JOBREPORT `ErrorCode=03` 사고 대응.

**변경:**
- `docker/init/01_init_acsdb.sql`: `jobId` 3개(`NA_T_TRANSPORTCMD`, `NA_H_TRANSPORTCMDHISTORY`, `NA_Q_TRANSPORTCMDREQUEST`) + `transportCommandId` 6개(`NA_A_ALARM`, `NA_H_ALARMRPTHISTORY`, `NA_H_ALARMTIMEHISTORY`, `NA_H_VEHICLEHISTORY`, `NA_L_LOGMESSAGE`, `NA_R_VEHICLE`)를 `character varying(64)` → `character varying(256)` 으로 통일.
- `src/ACS/ACS.App/Database/AcsDbContext.cs`: 대응하는 EF Core 매핑 9개 라인 `HasMaxLength(64)` → `HasMaxLength(256)`.
- 신규 `docker/init/migration_jobid_256.sql`: 운영 DB(`acsdb@10.0.26.2`) 적용용 멱등 ALTER 스크립트. 9개 ALTER 모두 포함(사용자가 이미 수동 적용한 `NA_T_TRANSPORTCMD.jobId` 포함 — PostgreSQL 은 동일 길이면 사실상 no-op).

**핵심 사실:** `transportCommandId` 는 `NA_T_TRANSPORTCMD.jobId` 값을 참조 저장하므로 길이 불일치 시 alarm/history/log 기록 시점에 동일 truncation 오류 발생. jobId 만 늘리면 안 됨.

**검증:** `ACS.Elsa` 빌드 통과. 운영 적용은 `psql -h 10.0.26.2 -U acsuser -d acsdb -f docker/init/migration_jobid_256.sql` 후 `\d "<table>"` 로 타입 확인. 사고 메시지 재현 시 `TransportCommand created` + JOBREPORT `<ErrorCode>0</ErrorCode>` 예상.

---

## 37. ACS.UI 중앙 배포/자동 업데이트 (Velopack + CS 정적 릴리스 피드)

**날짜:** 2026-06-10
**작업:** 수동 exe 복사로 배포하던 ACS.UI를 외부(사내망/VPN) 클라이언트 PC가 CS에서 내려받아 설치하고 자동 업데이트 받도록 구현. 결정: **Velopack 1.2.0** + CS 웹 호스트(5100) 정적 서빙 + 사내망 전용(HTTP 무인증).

**아키텍처:**
- Pack ID `AcsUi`, 클라이언트 설치 위치 `%LocalAppData%\AcsUi\`. 최초 설치: `http://<CS>:5100/releases/ui/AcsUi-win-Setup.exe`.
- 릴리스 피드: CS(control 프로세스)가 `Acs:Api:ClientReleasePath`(기본 `C:\acs\releases\ui`)를 `/releases/ui`로 정적 서빙 (`ServeUnknownFileTypes=true` 필수 — .nupkg/RELEASES/releases.win.json은 기본 MIME 매핑에 없음).
- 업데이트 UX: 시작 직후 + 4시간 주기 백그라운드 체크 → 다운로드 완료 시 `WaitExitThenApplyUpdates`(종료 시 자동 적용) 예약 + 상태바 배너("지금 재시작" 버튼, 모달 없음). 서버 미접속/미설치(IDE 실행)는 전부 무시 — 오프라인 기동 보장(`UpdateManager.IsInstalled` 가드).
- publish: **self-contained win-x64** (클라이언트 .NET 런타임 불필요, 델타로 갱신 용량 최소). **PublishSingleFile 금지**(Velopack 미지원).

**변경:**
- `ACS.UI.csproj`: `Velopack 1.2.0`, `<Version>1.0.0</Version>`(스크립트가 `-p:Version` 덮어씀), `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>`.
- `ACS.UI/Program.cs`: `Main` 최상단 `VelopackApp.Build().Run()` — Avalonia 초기화보다 반드시 먼저(설치/업데이트 훅에서 즉시 종료 가능, vpk pack이 이 호출 존재를 정적 검증함).
- 신규 `ACS.UI/Services/UpdateService.cs`: `UpdateManager`+`SimpleWebSource($"{baseUrl}/releases/ui")` 래핑, CheckAndDownload/ApplyAndRestart/ApplyOnExit.
- `ACS.UI/App.axaml.cs`: ProgramData 설정 오버레이(`C:\ProgramData\ACS.UI\appsettings.json`, optional) — Velopack은 업데이트마다 `current\`를 통째로 교체하므로 사이트별 `Backend.Host`는 여기 1회 작성(업데이트에도 보존). + 백그라운드 업데이트 루프.
- `MainWindowViewModel`: `UpdateReadyVersion`/`IsUpdateReady` + `NotifyUpdateReady(version, applyAndRestart)` + `RestartForUpdateCommand`. `MainWindow.axaml` 상태바 가운데 칼럼에 배너.
- `ACS.App/Program.cs` RunWebHost: `UseStaticFiles(PhysicalFileProvider)` — `Directory.CreateDirectory` 가드 필수(PhysicalFileProvider는 폴더 부재 시 예외). `ACS.App/appsettings.json` `Acs:Api:ClientReleasePath` 추가.
- 신규 `src/ACS/publish-ui.ps1`(UTF-8 BOM): `dotnet publish`(self-contained) → `vpk pack`(outputDir `src/ACS/releases/ui`, 회차 간 보존 시 델타 자동 생성) → `-ReleaseDir` 지정 시 robocopy `/E`(절대 `/MIR` 금지 — 이전 릴리스 누적 유지). 사전조건 `dotnet tool install -g vpk`.
- `.gitignore`: `.publish-ui-staging/` 추가(`[Rr]eleases/`는 기존 패턴이 커버).

**핵심 사실:**
- 버전은 릴리스마다 SemVer 증가 필수(vpk가 중복 거부). 스크립트가 publish/pack 양쪽에 동일 `-Version` 전달.
- 업데이트 동작은 Setup.exe로 설치된 상태에서만 테스트 가능(bin 직접 실행은 `IsInstalled=false`로 no-op).
- 다른 PC에서 빌드 시 델타 생성용 이전 릴리스 복원: `vpk download http --url http://<CS>:5100/releases/ui`.

**검증:** ACS.UI/ACS.App 빌드 0 오류. `publish-ui.ps1 -Version 1.0.0` 실제 실행 성공 — `AcsUi-win-Setup.exe`(50MB)/`AcsUi-1.0.0-full.nupkg`/`releases.win.json` 생성, vpk가 `VelopackApp.Run()` 호출 검증 통과. **런타임 미검증**: CS 기동 후 피드 URL 200 확인, 테스트 PC Setup.exe 설치, 1.0.1 발행 시 배너 표시→재시작 적용·델타 생성, ProgramData 설정 보존, CS 중지 상태 오프라인 기동.

---

## 38. ACS.UI 사용자 로그인/권한 관리 (NA_X_USER + 3단계 역할)

**날짜:** 2026-06-10
**작업:** UI 실행 시 누구나 모든 CRUD/업데이트 가능했던 무인증 상태를 제거. Admin/Operator/Viewer 3단계 역할 기반 권한 + BCrypt 해시 비밀번호 + 메모리 세션(12h 슬라이딩) 도입. 초기 부트스트랩 `admin/admin` 시드 후 최초 로그인 시 강제 변경.

**아키텍처:**
- 사용자 데이터: ACS.App 의 PostgreSQL `NA_X_USER` (Seq SERIAL, UserId UNIQUE, PasswordHash, Role, MustChangePassword, IsActive, LastLoginTime + TimedEntity 표준 컬럼).
- 인증: REST `POST /api/auth/login` → GUID 토큰 + 역할. `Authorization: Bearer <token>` 헤더로 보호된 엔드포인트(`/api/users`, `/api/auth/logout`, `/api/auth/change-password`) 호출. 세션 저장소는 메모리(`ConcurrentDictionary`) — 재시작 시 전부 무효화(UI는 401 → 재로그인).
- 권한 매트릭스: **Admin** = 사용자 관리 + 데이터 CRUD + 업데이트 적용 / **Operator** = 데이터 CRUD + 업데이트 적용 / **Viewer** = 조회 전용.
- UI 시작 시퀀스: `LoginWindow` 모달 → 인증 실패 → 종료. `mustChangePassword=true` → `ChangePasswordWindow` 강제(취소 시 로그아웃·종료) → `MainWindow` + SignalR/UpdateService 기동. `ShowDialog(null)` 미지원이라 LoginWindow를 임시 MainWindow로 띄우고 Closed 이벤트에서 실제 MainWindow로 교체.
- 권한 게이트 바인딩: `UserSession.Current` 정적 인스턴스(재할당 없이 상태만 갱신 — INPC 발생) 를 XAML에서 `{x:Static svc:UserSession.Current}` 로 직접 바인딩. CRUD View(Mqtt/Node/Station/Link/Bay/Zone/Port/TransferCommand/Vehicle) 의 Add/Edit/Delete 버튼에 `IsEnabled="{Binding ... Path=CanEdit}"` 추가. USER 메뉴는 `CanManageUsers` 로 가시성 제어. 상태바 업데이트 배너 버튼은 `CanUpdateUi` 로 제어 + `App.axaml.cs` 백그라운드 루프에서 Viewer 시 다운로드 자체 생략.

**변경:**
- 신규 `ACS.Core/User/Model/User.cs` (NamedEntity 아닌 TimedEntity 상속).
- `ACS.App/Database/AcsDbContext.cs`: `DbSet<User> Users` + `NA_X_USER` Fluent 매핑.
- `ACS.App/Executor.cs`: `SeedAdminUser(dbContext)` — User 테이블 비어있으면 admin/admin + MustChangePassword=true 삽입.
- `ACS.App/ACS.App.csproj`: `BCrypt.Net-Next 4.0.3` 추가.
- 신규 `ACS.App/Web/Auth/{PasswordHasher,SessionStore,AcsAuthorizeAttribute}.cs`. `Program.cs` 에 `SessionStore` 싱글톤 DI 등록.
- 신규 `ACS.App/Web/Controllers/{AuthController,UsersController}.cs`. `ACS.Core.User.Model.User` 가 `ControllerBase.User` 와 충돌 → `using UserEntity = ACS.Core.User.Model.User;` 별칭 사용.
- 신규 `ACS.Communication/Http/Models/UserDto.cs` (UserDto + LoginRequestDto + LoginResponseDto + ChangePasswordRequestDto).
- 신규 `ACS.UI/Services/{UserSession,AuthHeaderHandler}.cs`. `AcsApiService` 생성자에 `UserSession` 주입 + `DelegatingHandler` 로 Bearer 자동 부착. `IAcsApiService` 에 Login/Logout/ChangePassword/Users CRUD 추가.
- 신규 `ACS.UI/Views/{LoginWindow,ChangePasswordWindow,UserView,UserEditWindow,ResetPasswordDialog}.axaml(.cs)` + `ViewModels/UserViewModel.cs`. `ApplicationRibbonView` 에 USER 카테고리(Admin 가시성) 추가, `ApplicationViewModel` 의 SelectMenu switch 에 `USER → UserManagement` 매핑 추가.
- `MainWindowViewModel`: `UserSession` 의존성 + `UserViewModel` 추가 + `OpenPopupView` switch + `LogoutCommand` 추가. `MainWindow.axaml` 상태바에 사용자 표시(`UserSession.Current.DisplayName`) + Logout 버튼.
- `App.axaml.cs`: `UserSession.Current` 단일 인스턴스 사용. LoginWindow를 desktop.MainWindow 로 임시 지정 → Closed 핸들러에서 인증 결과에 따라 처리.

**핵심 사실:**
- `UserSession.Current` 는 재할당하지 않고 상태만 갱신 — XAML x:Static 바인딩이 끊어지지 않는다. CommunityToolkit.Mvvm 의 `[ObservableProperty]` + `[NotifyPropertyChangedFor]` 로 권한 플래그가 즉시 전파.
- `ControllerBase.User` 속성과 `ACS.Core.User.Model.User` 가 동일 이름 — Controller에서는 `UserEntity` 별칭 필수.
- 데이터 CRUD 백엔드 엔드포인트(`/api/nodes` 등) 의 서버측 인가는 이번 PR 범위에 없음 (UI 버튼 비활성화로 1차 통제). 향후 별도 작업으로 `[AcsAuthorize(Role="Admin,Operator")]` 부착 필요.
- 사용자 관리 화면에서 본인 삭제 / 마지막 활성 Admin 강등·비활성화·삭제는 백엔드가 거부 (lock-out 방지).
- 부트스트랩 admin 의 MustChangePassword=true 는 시드 시점에만 설정 — 이후 admin 본인이 변경하면 false.
- **NA_X_USER 테이블 생성 갭 보완**: `EnsureCreated()` 는 비어 있지 않은 DB 에서는 no-op 이라 EF Fluent 매핑만으로는 기존 acsdb 에 신규 테이블이 생기지 않는다. 두 경로 모두 명시적 DDL 추가 — (i) `docker/init/01_init_acsdb.sql` 의 상단 DROP CONSTRAINT 블록 / DROP TABLE 블록 / NA_X_OPTION 다음 CREATE TABLE / 하단 ALTER ADD CONSTRAINT 블록 4곳에 NA_X_USER 항목 삽입(신규 docker 설치 경로), (ii) `Executor.MigrateUserTable` 가 `pg_class` 조회 → 없으면 멱등 `CREATE TABLE` 실행하여 `MigrateLogMessageTable` 다음, `SeedAdminUser` 직전에 호출(기존 DB 업그레이드 경로). 두 DDL 의 컬럼명/타입/PK/UNIQUE 가 EF 매핑(`AcsDbContext.cs` 의 NA_X_USER 블록) 과 정확히 일치해야 한다.

**검증:** `dotnet build ACS.sln` 0 오류(경고 19, 모두 무관한 기존 nullable/legacy 경고). **런타임 미검증**: ACS.App 최초 실행 후 `NA_X_USER` 자동 생성 + `admin/admin` 시드 (psql 확인), UI 실행 → LoginWindow → admin/admin → 강제 변경 다이얼로그 → MainWindow 진입. Admin 로그인 시 Application 탭에 USER 메뉴 + CRUD 버튼 활성, Operator는 USER 메뉴 숨김·CRUD 활성·업데이트 가능, Viewer는 USER 메뉴 숨김·CRUD 버튼 비활성·업데이트 배너 버튼 비활성. 백엔드 재시작 후 401 → 자동 재로그인 유도.

---

## 39. ACS.UI 배포 절차 문서화 (deploy-ui.md 신설)

**날짜:** 2026-06-12
**작업:** 섹션 37의 Velopack 자동 업데이트 체계에 대한 **회차 배포 운영 절차서**를 `docs/deploy-ui.md`로 신설. 코드/스크립트 변경 없음(문서화만).

**배경:** 배포 절차가 `publish-ui.ps1` 주석과 `ACS.App/Program.cs`·`UpdateService.cs` 코드에만 흩어져 있어 운영자 참조용 독립 문서가 부재했음.

**문서 내용:** 구성 요소 표(스크립트/피드 경로·URL/버전 규칙/권한), 사전 준비(.NET 8 SDK + `vpk` CLI), 델타 원리, **절차 A**(CS에서 직접 빌드 — `publish-ui.ps1 -Version <v> -ReleaseDir C:\acs\releases\ui` 한 줄), **절차 B**(빌드 PC≠CS — `vpk download`로 피드 동기화 → pack → 누적 복사), 클라이언트 반영, 흔한 실수, 검증 체크리스트.

**핵심 사실(재확인):** 델타는 `vpk pack` 시 출력 폴더에 직전 `.nupkg`가 있어야 생성됨 → 빌드 PC가 CS와 분리되면 빌드 전 `vpk download http --url http://<CS>:5100/releases/ui`로 동기화 필수. 피드 복사 시 `/MIR` 금지(이전 회차/델타 삭제 = 체인 붕괴). 버전은 회차마다 증가(SemVer, vpk 중복 거부).

## 40. EXCHANGE S3 배선 완성 — EXCHANGECMD "Unknown host message" 해결

**날짜:** 2026-08-12
**작업:** MES EXCHANGECMD 가 HS 에서 `[Bridge] Unknown host message` 로 드롭되던 문제 해결. S3 슬라이스(파서·검증·1-TC·JOBREPORT) 코드는 이미 완성 상태였고, 부족한 것은 배선 2가지였음.

**원인 분석:**
1. 실행 중이던 HS 바이너리가 구버전 — `HostBridgeService.cs` 의 `case "EXCHANGECMD"` 분기가 작업 트리에만 있고 `publish/HS01_P` 에는 미컴파일 상태.
2. `ACS.Elsa/elsa-migration.json` 의 `ElsaCommands` 에 `EXCHANGECMD` 미등록 → 리빌드해도 `ElsaWorkflowManagerBridge.IsElsaCommand()` 가 legacy `WorkflowManagerImpl` 로 폴백, legacy 엔 EXCHANGECMD BizProcess 가 없어 조용히 실패. **새 Host 커맨드 추가 시 (a) HostBridgeService switch case, (b) elsa-migration.json ElsaCommands, (c) `Host{커맨드}Workflow` 클래스명(OrdinalIgnoreCase 매칭) 3가지가 모두 맞아야 함.**

**변경:**
- `ACS.Elsa/elsa-migration.json`: `ElsaCommands` 에 `"EXCHANGECMD"` 추가 (유일한 라우팅 수정)
- `ACS.Elsa/Activities/HostExchangeActivities.cs`: `using Elsa.Extensions;` 누락 보완 (Input<T>.Get 확장 — 빌드 오류였음)
- 부수 수정: `ACS.App/` 안의 `C:\acs\releases\ui` 라는 이름의 빈 디렉토리 제거 — Windows 경로가 macOS 에서 문자 그대로 폴더로 생성된 것으로, MSBuild glob(`**/*.resx`) 확장을 깨뜨려 MSB3552 빌드 실패 유발
- `deploy/HS01_P/appsettings.json`(PC 로컬): DB Password 를 실제 docker postgres 값(1234)으로 교정 — config-templates 시딩 기본값은 P@4083w0rd

**E2E 검증 (2026-08-12, MES 시뮬레이터 = 3333 수신 + 3334 송신, LE 4바이트 길이 프레이밍):**
- NACK 경로: 미존재 위치(TEST_LOAD_RACK_01) → JOBREPORT(RECEIVE, Step=10, ErrorCode=25 SOURCEMACHINENOTFOUND) 회신 확인
- ACK 경로: IN_BUF → EQP1:LEFT → EQP2 (모두 bay DEMO) → JOBREPORT(RECEIVE, Step=10, ErrorCode=0) + `NA_T_TRANSPORTCMD` 에 `state=EXCHANGE_QUEUED, jobType=EXCHANGE, eqpId=ACS01, portId=NULL, bayId=DEMO, additionalInfo=STEP=10;TRIP=;...` 1행 생성 확인 (사양 §2.1 스냅샷 일치, 검증 후 테스트 행 삭제)
- 단위 테스트 52건 통과 (`dotnet test ACS.Core.Tests`)

**현재 상태:** S3 검증점("TC 1행 + RECEIVE 회신")까지 동작. `EXCHANGE_QUEUED` TC 를 집어가는 스케줄러(S4 배차)는 미착수라 TC 는 의도적으로 대기 상태에 머무름.

## 41. EXCHANGE S4 배차 — SCHEDULE-EXCHANGEJOB 신설

**날짜:** 2026-08-12
**작업:** `EXCHANGE_QUEUED` TC 를 AMR 에 배차하는 S4 슬라이스. 범위는 배차까지 — 조회 → 적격 차량(IDLE+CONNECT+기할당없음+**4슬롯 전부 EMPTY**) → 슬롯 페어 예약 → RAIL-CARRIERTRANSFER(Origin행) → JOBREPORT(START, Step=10, PICKUP_NEW). 도착 이후는 다음 슬라이스. 기존 MOVECMD 배차 경로는 0줄 수정 (D4/D5 유지 — 전부 additive/신규 파일).

**설계 결정:**
- `STATE_EXCHANGE_ASSIGNED`("EXCHANGE_ASSIGNED", 17자) 신설 — 기존 ASSIGNED 계열 워크플로우와 상태 격리.
- **EXCHANGE-JOBREPORT 병렬 릴레이 신설**: 기존 TS→HS JOBREPORT 릴레이(JobReportData/ExtractJobReportFromInput/ForwardJobReportToMesActivity/IHostMessageService.BuildJobReport 4곳)에는 Step/StepName/CarrierSlot 필드가 없어, 기존 경로 수정 대신 messageName="EXCHANGE-JOBREPORT" 를 신설. HS 의 리스너는 header.messageName 라우팅이므로 elsa-migration 등록만으로 분기됨.
- RAIL-CARRIERTRANSFER 는 기존 `SendCarrierTransferWithRetryActivity` 재사용 (`UseSource=true` + `JOBTYPE_UNLOAD` = Origin 픽업 지시, EXCHANGE TC 의 Source=OriginLoc 이므로 그대로 동작).
- TRIP 은 배칭 미도입이라 빈 값 유지, STEP 은 도착 전이므로 10 유지. AdditionalInfo 에 LOADSLOT/UNLOADSLOT 기록.
- 롤백은 EXCHANGE 전용(`RollbackExchangeAssignmentActivity`): `ReleaseAllByJobId` 로 슬롯 예약 해제 후 EXCHANGE_ASSIGNED 인 경우에만 EXCHANGE_QUEUED/Vehicle IDLE 복원 + LOADSLOT/UNLOADSLOT 초기화.

**신규 파일:**
- `ACS.App/Scheduling/Awake/AwakeExchangeTransportJob.cs` — 10초 주기, Bay 순회, EXCHANGE_QUEUED 존재 시 SCHEDULE-EXCHANGEJOB 발행. **namespace 는 폴더와 다른 `ACS.Scheduling`** (SchedulingModule 이 Type.GetType 문자열 로딩 — 불일치 시 조용히 미등록).
- `ACS.Elsa/Activities/ScheduleExchangeActivities.cs` — 액티비티 5종 (Get/Find/Assign/Rollback/SendStart).
- `ACS.Elsa/Workflows/Trans/ScheduleExchangeJobWorkflow.cs` — DefinitionId=SCHEDULE-EXCHANGEJOB.
- `ACS.Elsa/Workflows/Host/ExchangeJobReportWorkflow.cs` — DefinitionId=EXCHANGE-JOBREPORT (TS JSON → MES XML 전달).
- `ACS.Core.Tests/Exchange/TransportCommandExDescriptionTests.cs`

**수정 파일(additive):** TransportCommandEx(STATE_EXCHANGE_ASSIGNED, GetMaterialType), ITransferManagerEx/TransferManagerExImplement(GetExchangeQueuedTransportCommandsByBayId), IMessageManagerEx/MessageManagerExImplement(SendExchangeJobReportToHost), HostExchangeActivities.cs 말미(ExchangeJobReportData/Extract/Forward), SchedulingModule(등록 1줄), elsa-migration.json(SCHEDULE-EXCHANGEJOB·EXCHANGE-JOBREPORT 추가, deploy 5부 동기화), ExchangeConstantsTests/ExchangeInfoTests 확장.

**검증:** `dotnet build ACS.sln` 오류 0, `dotnet test ACS.Core.Tests` 64건 전체 통과. E2E(배포 후 Validator EXCHANGE 시나리오: START Step=10 XML + NA_T_TRANSPORTCMD state=EXCHANGE_ASSIGNED + NA_R_VEHICLE_SLOT 예약 확인)는 미실시 — 다음 배포 테스트에서 확인 필요. DS/TS/HS 가 공유 어셈블리를 쓰므로 동시 재배포 필요.

**현재 상태:** S4 코드 완성. 다음 슬라이스(S5)는 Origin 도착/픽업 완료 → MOVE_TO_EQUIP(Step=20) 진행 — RailVehicleDestArrived/AcquireCompleted 계열에 EXCHANGE 분기 필요.
