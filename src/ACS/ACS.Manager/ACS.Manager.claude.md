# ACS.Manager

비즈니스 로직 매니저 계층. ACS.Core의 인터페이스를 구현한다.

## 주요 매니저

- `Host/` — 호스트 통신 매니저
- `Path/` — 경로 관리 매니저
- `Transfer/` — 반송 명령 매니저
- `Message/` — 메시지 처리 매니저
- `History/` — 이력 기록 매니저
- `Application/` — 애플리케이션 제어 매니저
- `Alarm/` — 알람 매니저

## 의존성

ACS.Core, ACS.Communication을 참조한다. ACS.App을 참조하지 않는다 (순환 참조 해소 완료).
