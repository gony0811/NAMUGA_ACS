# ACS 프로젝트 작업 예정 리스트

## 1. Map view 완성

**완료날짜:** 2026-03-27

## 2. MOVECMD 명령 워크플로우 완성

**작업내용**
-. MOVECMD를 TransferMessage로 가공해서 TS 큐로 전달
-. DAEMON은 Transport queued 작업 스케줄링
-. Trans는 AMR에 작업 할당
-. Ei는 AMR로 명령 전송
-. Ei는 AMR로 부터 송신되는 상태, 위치, 알람, 응답(movecmd received, arrived, loaded, unloaded, completed 등) 메시지 trans로 전달
-. trans는 host로 응답 전달 및 transport 진행 및 완료처리

## 3. AMR 통신 및 보고/명령 워크플로우 완성

## 3. ACTIONCMD 명령 워크플로우 완성