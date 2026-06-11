# ACS.UI

Avalonia 11 기반 AMR 모니터링/관제 데스크탑 애플리케이션.

## 실행

```bash
dotnet run --project ACS.UI/ACS.UI.csproj
```


백엔드 API 서버(ACS.App, 포트 5100)가 먼저 실행되어 있어야 한다.

## 아키텍처

MVVM 패턴 (CommunityToolkit.Mvvm):
- `ViewModels/` — ViewModel 클래스
- `Views/` — Avalonia XAML 뷰
- `Models/` — 데이터 모델
- `Controls/` — 커스텀 컨트롤 (MapCanvas 등)
- `Services/` — HTTP API 클라이언트 등 서비스
- `Converters/` — 값 변환기

## 주요 기능

- **MapCanvas**: 노드/링크/차량을 렌더링하는 커스텀 컨트롤 (줌/팬 지원)
- **VehicleListView**: DataGrid로 차량 상태, 배터리, 현재 노드 등 표시
- 1초 주기 폴링으로 동적 데이터(차량/명령) 갱신
- **로그인/권한 관리**: 시작 시 `LoginWindow` 모달 → 백엔드 인증(Bearer 토큰). Admin/Operator/Viewer 3단계 역할.

## 인증/권한 패턴

- `Services/UserSession.cs` — `UserSession.Current` 정적 인스턴스(재할당 없이 상태만 갱신). XAML에서 `{Binding Source={x:Static svc:UserSession.Current}, Path=CanEdit}` 형태로 권한 게이트 바인딩.
- `Services/AuthHeaderHandler.cs` — `AcsApiService` 의 HttpClient에 부착되어 모든 요청에 `Authorization: Bearer <token>` 자동 부착.
- `Views/LoginWindow` — 시작 직후 표시. 성공 시 `MustChangePassword=true` 이면 `ChangePasswordWindow` 강제.
- `Views/UserView` — Application 탭 USER 메뉴(Admin 가시성). MQTT CRUD 와 동일 패턴으로 사용자 등록/수정/삭제/비밀번호 리셋.
- 권한 매트릭스: Admin = 모든 권한 / Operator = 데이터 CRUD + UI 업데이트 / Viewer = 조회 전용.

## 의존성

- Avalonia 11.2.3 (Desktop, Themes.Fluent, Controls.DataGrid)
- CommunityToolkit.Mvvm 8.4.0
