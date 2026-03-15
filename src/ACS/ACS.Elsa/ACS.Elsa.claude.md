# ACS.Elsa + ACS.Activity

Elsa 3.5.3 워크플로우 엔진 통합 계층.

## ACS.Elsa

- `Bridge/AutofacContainerAccessor.cs` — Autofac ↔ Elsa DI 브릿지. Elsa Activity에서 Autofac 컨테이너의 ACS 서비스에 접근 가능하게 한다.
- `Activities/` — Elsa 커스텀 액티비티 등록

## ACS.Activity

- `Activities/` — ACS 도메인 전용 Elsa 커스텀 액티비티 구현
- `Workflows/` — 워크플로우 정의

## ACS.Elsa.Studio + ACS.Elsa.Studio.Client

워크플로우 디자이너 웹 UI:
- `ACS.Elsa.Studio` — ASP.NET Core 호스트 (포트 5200)
- `ACS.Elsa.Studio.Client` — Blazor WebAssembly 프론트엔드 (Elsa Studio 3.3.0)

```bash
dotnet run --project ACS.Elsa.Studio/ACS.Elsa.Studio.csproj
```

## Elsa 워크플로우 DB

PostgreSQL `acsdb_elsa` 데이터베이스에 워크플로우 상태를 저장한다.
