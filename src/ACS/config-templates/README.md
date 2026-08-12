# config-templates — 사이트별 appsettings 원본 템플릿

새 PC/서버에서 배포를 시작할 때 `deploy/<SITE>/appsettings.json` 의 씨앗이 되는 폴더다.
`publish-deploy.ps1` 과 `run-all.sh` 가 `deploy/<SITE>/appsettings.json` 이 없으면
여기서 자동으로 복사(시딩)한다. 이미 있으면 절대 덮어쓰지 않는다.

## 폴더 역할 요약

| 폴더 | git | 용도 |
|---|---|---|
| `config-templates/<SITE>/` | 추적 O | 사이트별 설정 **원본 템플릿**. 공통 변경은 여기 수정 후 커밋 |
| `deploy/<SITE>/` | 제외 | **실행·배포 폴더** (PC별). 배포 대상은 항상 여기. 최초 실행 시 템플릿에서 자동 생성 |
| `publish/` | 제외 | run-all.sh 등 빌드 임시 산출물. 배포와 무관, 지워도 됨 |
| `releases/ui/` | 제외 | UI Velopack 릴리스(델타 체인). 절대 지우지 말 것 |

## 사이트별 차이 (이것만 다르고 나머지는 동일)

| SITE | Acs:Process:Type | Acs:Api:ListenPort |
|---|---|---|
| CS01_P | control (UI 백엔드 겸함) | 5100 |
| HS01_P | host | 5101 |
| TS01_P | trans | 5103 |
| ES01_P | ei | 5104 |
| DS01_P | daemon | 5105 |

## 시딩 후 반드시 확인

- `ConnectionStrings:DefaultConnection` — 해당 PC 의 PostgreSQL 계정/비밀번호로 수정
- RabbitMQ / MQTT 호스트 주소 — 해당 환경 기준으로 수정
- 수정은 `deploy/<SITE>/appsettings.json` (해당 PC 전용) 또는 여기 템플릿(전 사이트 공통) 중
  성격에 맞는 쪽에 할 것
