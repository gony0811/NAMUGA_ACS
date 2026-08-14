# config-templates — appsettings 원본 템플릿 (레이어드 구조)

새 PC/서버에서 배포를 시작할 때 `deploy/` 설정의 씨앗이 되는 폴더다.
`publish-deploy.ps1` 과 `run-all.sh` 가 대상 파일이 없으면 여기서 자동으로 복사(시딩)한다.
이미 있으면 절대 덮어쓰지 않는다.

## 레이어드 구조 (공통 1부 + 사이트별 슬림 파일)

설정은 두 레이어로 나뉘며, 나중에 로드되는 사이트 파일이 공통을 override 한다:

```
deploy/
├── appsettings.common.json    ← 공통 1부 (DB 접속, RabbitMQ, Message/Destination 라우팅 등)
├── CS01_P/appsettings.json    ← 사이트 정체성만 (Process:Name/Type, ListenPort, Serilog 로그 경로)
├── DS01_P/appsettings.json    ← 〃 (+ Acs:Amr — AMR 사용 사이트만)
└── …
```

- 로드 순서: `{사이트 폴더}\..\appsettings.common.json`(optional) → `{사이트 폴더}\appsettings.json`
  (`ACS.App/Executor.cs LoadConfiguration`, `Program.cs` — common 부재 시 기존 단일 파일 방식 그대로 동작)
- **DB 비밀번호·브로커 주소 등 공통 변경은 `deploy/appsettings.common.json` 1부만 수정**하면
  전 프로세스에 적용된다 (적용은 프로세스 재시작 시).

## 폴더 역할 요약

| 폴더 | git | 용도 |
|---|---|---|
| `config-templates/appsettings.common.json` | 추적 O | **공통 설정 원본 템플릿**. 공통 변경은 여기 수정 후 커밋 |
| `config-templates/<SITE>/` | 추적 O | 사이트별 **슬림 템플릿** (정체성 키만) |
| `deploy/appsettings.common.json` | 제외 | 공통 설정 **실사용본** (PC별 — DB 비번 등 환경값 포함) |
| `deploy/<SITE>/` | 제외 | **실행·배포 폴더** (PC별). 배포 대상은 항상 여기 |
| `publish/` | 제외 | run-all.sh 등 빌드 임시 산출물. 배포와 무관, 지워도 됨 |
| `releases/ui/` | 제외 | UI Velopack 릴리스(델타 체인). 절대 지우지 말 것 |

## 사이트별 차이 (사이트 파일에 있는 것 전부)

| SITE | Acs:Process:Type | Acs:Api:ListenPort | Acs:Amr |
|---|---|---|---|
| CS01_P | control (UI 백엔드 겸함) | 5100 | — |
| HS01_P | host | 5101 | — |
| TS01_P | trans | 5103 | O |
| ES01_P | ei | 5104 | O |
| DS01_P | daemon | 5105 | O |

(+ `Serilog` 로그 파일 경로 `logs/<SITE>-.log` — 사이트명 파생이라 사이트 파일에 유지)

## 시딩 후 반드시 확인

- `deploy/appsettings.common.json` 의 `ConnectionStrings:DefaultConnection` — 해당 PC 의 PostgreSQL 계정/비밀번호로 수정
- RabbitMQ / MQTT 호스트 주소 — 해당 환경 기준으로 수정 (모두 common 에 있음)
- 수정 위치 규칙: **환경값(그 PC 전용)** 은 `deploy/appsettings.common.json`,
  **전 사이트 공통의 영구 변경** 은 여기 템플릿에도 반영 후 커밋,
  **사이트 정체성**(포트/타입 등) 은 `deploy/<SITE>/appsettings.json`
