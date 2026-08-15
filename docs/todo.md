# ACS 프로젝트 작업 예정 리스트

(상세 이력·설계 결정은 `docs/memory.md` 참조)

## 완료

1. Map view 완성 (2026-03-27)
2. MOVECMD 명령 워크플로우 완성 (Host→Trans→EI→AMR 왕복, memory.md §12~)
3. AMR 통신 및 보고/명령 워크플로우 완성 (MQTT 가상 차량 E2E, §44~)
4. ACTIONCMD 명령 워크플로우 완성 (§48)
5. EXCHANGE 정상 경로 (S1~S6): STEP 10→60 완주 E2E 실증 — Validator "completed successfully" (§49)
6. JOBCANCEL C1~C4 + cancelCmd 프로토콜 + MAGAZINE_NOT_FOUND 실패 경로 (§50)
7. 운영 reset/delete 의 EXCHANGE 인지화 + 차량 reset 슬롯·알람 동반 초기화 (§49~50)

## 남은 작업

1. **MAGAZINE_NOT_FOUND 실측**: 코드 완료 — 시뮬레이터 GUI Fail 주입으로 수동 확인 필요 (§50)
2. **TRIP 배칭**: AdditionalInfo `TRIP` 키 예약만 됨. 배칭 도입 시 JOBCANCEL C5(페어 연대 종결, `EXCHANGE_CANCELED`) 함께 구현
3. **AMR 일반 실패 경로(FAILED/REJECTED)**: MAGAZINE_NOT_FOUND 외 실패의 보고 방식은 MES 와 오류 코드 체계 협의 후 (사양서 "취소·오류" 시트 §3 단서)
4. **cancelCmd 실 AMR 협의**: reply(CANCELED status) 추가 여부 등 프로토콜 확정 (`docs/mqtt_interface.md` §cancelCmd)
5. 시뮬레이터 status 발행 타이머 안정성 관찰 (§50 부수 관찰 — 1회 무발행 발생, 원인 미상)
