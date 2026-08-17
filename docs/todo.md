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
8. MAGAZINE_NOT_FOUND 실측 완료 — 전체 경로 검증 4종 통과 (2026-08-17, §50)
9. UI Vehicle View 슬롯 상세 표시 (행 선택 RowDetails, §51)

## 남은 작업

1. **TRIP 배칭**: AdditionalInfo `TRIP` 키 예약만 됨. 배칭 도입 시 JOBCANCEL C5(페어 연대 종결, `EXCHANGE_CANCELED`) 함께 구현
2. **AMR 일반 실패 경로(FAILED/REJECTED)**: MAGAZINE_NOT_FOUND 외 실패의 보고 방식은 MES 와 오류 코드 체계 협의 후 (사양서 "취소·오류" 시트 §3 단서)
3. **cancelCmd 실 AMR 협의**: reply(CANCELED status) 추가 여부 등 프로토콜 확정 (`docs/mqtt_interface.md` §cancelCmd)
4. 시뮬레이터 status 발행 타이머 안정성 관찰 (§50 부수 관찰 — 1회 무발행 발생, 원인 미상). 설정 패널 조작 시 **차량 행 선택 선행** 필수 (§50 주의사항)
5. UI 릴리스: 슬롯 상세 표시를 설치본(Velopack)에 반영 — releases/ui 절차
