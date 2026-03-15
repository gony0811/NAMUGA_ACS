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

## 의존성

- Avalonia 11.2.3 (Desktop, Themes.Fluent, Controls.DataGrid)
- CommunityToolkit.Mvvm 8.4.0
