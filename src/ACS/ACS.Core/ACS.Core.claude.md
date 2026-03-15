# ACS.Core

전체 시스템의 기반 라이브러리. 인터페이스, 베이스 클래스, 유틸리티를 제공한다.

## 주요 하위 디렉토리

- `Alarm/` — 알람 인터페이스 및 모델
- `Application/` — ApplicationInitializer, Settings, IApplicationControlManager
- `Base/` — 공통 베이스 클래스
- `Cache/` — 캐시 추상화
- `Communication/` — 통신 인터페이스 (IMessageAgent, ISynchronousMessageAgent 등)
- `Control/` — IControlServerManager, IControlServerManagerEx
- `Database/` — DB 영속성 추상화
- `DependencyInjection/` — DI 헬퍼
- `Framework/` — 프레임워크 베이스
- `History/` — 이력 관리 인터페이스
- `Host/` — 호스트 통신 인터페이스
- `Logging/` — Serilog 로깅 설정
- `Material/` — 자재/캐리어 모델
- `Message/` — 메시지 모델 및 인터페이스
- `Path/` — 경로 관리 인터페이스
- `Resource/` — 리소스 관리
- `Route/` — 라우팅 인터페이스
- `Transfer/` — 반송(Transfer) 인터페이스
- `Utility/` — 공용 유틸리티
- `Workflow/` — 워크플로우 추상화

## 의존성 규칙

ACS.Core는 다른 ACS 프로젝트를 참조하지 않는다. 모든 프로젝트가 ACS.Core를 참조한다.
