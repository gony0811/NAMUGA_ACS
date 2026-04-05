# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# 솔루션 빌드 (src/ACS/ 디렉토리에서)
dotnet build ACS.sln

# NuGet 패키지 복원
dotnet restore ACS.sln
```

테스트 프로젝트 및 CI/CD 파이프라인은 아직 없음.

## 시스템 개요

.NET 8.0 기반 **Automated Container System (ACS)** — 컨테이너 반송, 장비 인터페이스, 워크플로우를 관리하는 산업 자동화 플랫폼. 코드 주석은 한국어.

## 의존성 구조

```
ACS.Core (인터페이스, 베이스 클래스)
   ↑
ACS.Communication (프로토콜 구현)
ACS.Manager (비즈니스 로직)
ACS.Service (서비스 계층)
   ↑
ACS.App (진입점, DI 구성)
ACS.Elsa / ACS.Activity (워크플로우 엔진)
ACS.Elsa.Studio / ACS.Elsa.Studio.Client (워크플로우 디자이너 웹 UI)
ACS.UI (Avalonia 데스크탑 클라이언트)
```

### 프로세스 유형별 프로젝트 구성
    ## ACS.Manager: 공통 비즈니스 로직
        - MessageManager : 메시지 처리 및 라우팅
        - ResourceManager : Vehicle, Node, Path 등 도메인 모델 관리
        - PathManager : 경로 탐색 및 최적화
        - AlarmManager : 알람 상태 관리
        - TransferManager : 반송 작업 관리
        - MaterialManager : 자재 관리
        - HistoryManager : 작업 이력 관리
        - ApplicationManager : 애플리케이션 설정 및 관리
    ## ACS.Service : 프로세스 유형별 서비스 구현
        - AlarmService : 알람 상태 관리
        - TransferService : 반송 작업 관리
        - MaterialService : 자재 관리
        - HistoryService : 작업 이력 관리
        - InterfaceService : 통신 인터페이스 관리
        - ResourceService : 자원 관리 (Vehicle, Node, Path)
        - VehicleInterfaceService : 차량 인터페이스 관리
- ACS.UI: 데스크탑 UI (Avalonia)
- ACS.Elsa: 워크플로우 정의 및 액티비티 구현
- ACS.Core: 인터페이스 및 도메인 모델 정의
- ACS.Communication: 프로토콜별 통신 구현 (예: MQTT, RabbitMQ)
- ACS.Service: 프로세스 유형별 서비스 구현 (예: `AmrService`,

## 핵심 아키텍처 패턴

- **Executor 패턴**: `ACS.App/Executor.cs`가 설정 로드 → Autofac 컨테이너 빌드 → DB 초기화 → 스케줄러/서비스 시작을 순차 수행
- **모듈러 DI (Autofac)**: 프로세스 타입(`Acs:Process:Type`)과 사이트(`Acs:Site:Name`) 설정에 따라 서로 다른 Autofac 모듈을 등록하여 동일 코드베이스에서 다른 런타임 동작 생성
- **Elsa Bridge**: Autofac 컨테이너를 Elsa DI에 연결하여 워크플로우 액티비티에서 ACS 서비스 접근 가능

## 주요 기술 스택

| 영역 | 기술 |
|------|------|
| DI | Autofac 9.1 |
| DB | PostgreSQL + EF Core |
| 로깅 | Serilog |
| 스케줄링 | Quartz 3.13 |
| 워크플로우 | Elsa 3.5 |
| 데스크탑 UI | Avalonia 11 + CommunityToolkit.Mvvm |
| 웹 UI | ASP.NET Core + Blazor WASM |

## 프로젝트별 세부 문서

각 프로젝트의 상세 내용은 해당 프로젝트 디렉토리의 `*.claude.md` 파일 참조:
- `src/ACS/ACS.App/ACS.App.claude.md`
- `src/ACS/ACS.Core/ACS.Core.claude.md`
- `src/ACS/ACS.Communication/ACS.Communication.claude.md`
- `src/ACS/ACS.Manager/ACS.Manager.claude.md`
- `src/ACS/ACS.Elsa/ACS.Elsa.claude.md`
- `src/ACS/ACS.UI/ACS.UI.claude.md`

## 프로젝트 작업 일정 및 TO DO List
- 'src/ACS/docs/todo.md'

## 작업 내역 및 결정 기록

작업 이력, 설계 결정, 미완료 항목은 `docs/memory.md` 참조. 새 작업 완료 시 해당 파일에 기록할 것.
