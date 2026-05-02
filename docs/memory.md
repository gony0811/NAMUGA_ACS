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

## 13-1. AMR ARRIVED → RAIL-VEHICLEARRIVED 포워더 추가

**날짜:** 2026-05-02
**작업:** EI 프로세스가 `amr/{id}/reply` 토픽에서 `status="ARRIVED"`를 받으면 RabbitMQ 통해 Trans로 `RAIL-VEHICLEARRIVED` JSON 전송.

**파이프라인:** MQTT(AMR reply, status=ARRIVED) → `MqttInterfaceManager.HandleReplyMessage` → `AMR-REPLY-RECEIVED` 워크플로우 → `HandleAmrReplyActivity` → `IMessageManagerEx.SendVehicleUpdateJson` → TsAgent(`TransAgentSender`) → Trans `/HQ/NMG/ES/TS/LISTENER`.

**status 라이프사이클 확장:** `ACCEPTED → EXECUTING → ARRIVED → COMPLETED` (ARRIVED 신규).

**변경 파일:**
| 파일 | 변경 |
|------|------|
| `ACS.Communication/Mqtt/Model/RailVehicleArrivedMessage.cs` | 신규. Header(messageName/transactionId/timestamp/sender) + Data(commandId/vehicleId/nodeId/resultCode/errorCode/errorMessage) |
| `ACS.Communication/Mqtt/Model/AmrReplyMessage.cs` | `nodeId` 필드 추가, status enum 코멘트에 ARRIVED 추가 |
| `ACS.Elsa/Activities/MqttActivities.cs::HandleAmrReplyActivity` | status별 분기로 재구성 (ARRIVED/COMPLETED). nodeId가 비면 cmdId로 TC 조회 후 jobType별 Source(UNLOAD)/Dest(LOAD) 보완 |
| `docs/mqtt_interface.md` | reply payload에 nodeId 명시, status 라이프사이클/라우팅 표 추가 |

**Trans 측 consumer는 별도 PR 스코프:** Trans의 `RAIL-VEHICLEARRIVED` 워크플로우/액티비티는 미구현 — 본 작업은 EI→RabbitMQ 발행까지만.

---

## 13-2. RAIL-VEHICLEARRIVED Trans 측 consumer 워크플로우 구현

**날짜:** 2026-05-02
**작업:** Trans 프로세스가 `RAIL-VEHICLEARRIVED` 메시지 수신 시 jobType별 source/dest 도착 분기 + EQP 도착 시 Host JOBREPORT(ARRIVED) 송신.

**파이프라인:** RabbitMQ(Trans 큐) → `GenericWorkflowRabbitMQListener.OnJsonMessage()` → `header.messageName="RAIL-VEHICLEARRIVED"` 라우팅 → `RailVehicleArrivedWorkflow` → `HandleVehicleArrivedActivity` → 상태 전이 + (EQP일 때만) `IMessageManagerEx.SendJobReportToHost("ARRIVED", ...)` → HostAgent → Host TCP → MES.

**jobType 분기:**
| jobType | TC.State | Vehicle.TransferState | 의미 |
|---------|----------|-----------------------|------|
| UNLOAD  | ARRIVED_SOURCE | ASSIGNED_ACQUIRING  | AMR이 source 도착, 픽업 직전 |
| LOAD    | ARRIVED_DEST   | ASSIGNED_DEPOSITING | AMR이 dest 도착, 드롭 직전   |

**JobReport 분기 (NA_R_LOCATION.Type 조회):** `IResourceManagerEx.GetLocationByStationId(nodeId)` → `Type=="EQP"` → JOBREPORT(ARRIVED) 송신, `Type=="BUFFER"` → 자재 포트이므로 송신 생략.

**ActionType:** 기존 COMPLETE 보고와 동일 패턴 — `SendJobReportToHost("ARRIVED", tc.JobId, vehicleId, tc.JobType, tc.Description)`. MES 가 `Type=ARRIVED + ActionType=LOAD/UNLOAD` 로 도착 위치 구분.

**변경 파일:**
| 파일 | 변경 |
|------|------|
| `ACS.Communication/Mqtt/Model/RailVehicleArrivedMessage.cs` | EI 보낸 JSON에 `JobType` 필드 포함 (사용자 직접 추가) |
| `ACS.Elsa/Workflows/Trans/RailVehicleArrivedWorkflow.cs` | 신규. Workflow + HandleVehicleArrivedActivity (5 step) |
| `ACS.Elsa/elsa-migration.json` | `RAIL-VEHICLEARRIVED` 추가 |
| `publish/{CS,DS,ES,HS,TS,UI}01_P/, base/elsa-migration.json` | 위와 동일 7개 사본 동기화 |

**참고:** EI 측에서 PascalCase `"JobType"` 으로 직렬화하므로 Trans 측 파서는 `JobType`/`jobType` 두 형태 모두 허용. `nodeId`는 EI에서 reply.NodeId 또는 cmdId→TC 조회로 채워서 송신.

---

## 13. run-all.sh 수정

**날짜:** 2026-03-17
**작업:** `< /dev/null` stdin redirect 추가 (nohup 백그라운드 프로세스 안정성)

**알려진 이슈:** `PROCESSES` 배열에 CS01_P 누락 — 사용자가 수정 거부하여 수동 시작 필요

---

## 14. 현재 상태 및 미완료 항목

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
