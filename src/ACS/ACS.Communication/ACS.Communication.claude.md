# ACS.Communication

산업용/메시징 프로토콜 구현 계층.

## 지원 프로토콜

- **MINA.NET** — 네트워크 프레임워크 (외부 DLL: `Library/Mina.NET.dll`)
- **RabbitMQ** — 메시지 브로커 (`RabbitMQ.Client 6.4.0`)
- **TIBCO Rendezvous** — 메시징 미들웨어 (외부 DLL: `Library/TIBCO.Rendezvous.dll`)
- **HTTP** — REST API 통신, `Http/Handlers/ApiRequestHandler.cs`
- **SECS** — 반도체 장비 통신 프로토콜
- **Xbee** — 무선 통신 (외부 DLL: `Library/transceiverxnet.dll`)

## HTTP API 모델

`Http/Models/` 디렉토리에 DTO 정의:
- VehicleDto, NodeDto, LinkDto, TransportCommandDto

## 외부 DLL

`Library/` 디렉토리의 비-NuGet DLL을 참조한다. 빌드 시 출력 디렉토리로 복사됨.
