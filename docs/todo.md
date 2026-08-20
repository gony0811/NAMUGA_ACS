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
10. AMR↔ACS EXCHANGE 인터페이스 v0.3 정합화 — reply(ARRIVED/STEP_COMPLETE/CANCELED, jobId/step/carrierSlot)·actionCmd type/jobId·cancelCmd jobId, REJECTED@10 롤백, EXCHANGE stuck 복구, 문서 md+docx (§53)
11. **TRIP 배칭(S7) + JOBCANCEL C5** — 트립당 2건 페어링(슬롯 1·3/2·4), ExchangeTour 투어 인터리브, trip 인지 취소/복구/reset. **실기 AMR E2E 완주**: 픽업×2→설비×2→반납×2, COMPLETE(60)×2, REJECTED@10 자가 회복 실측 (2026-08-20, §54)

## 남은 작업

1. **배칭 후속 실측**: 단독 1건 회귀(코드 경로 동일 — AdvanceTour 1건 축약), C5(배칭 중 적재 후 JOBCANCEL → 페어 EXCHANGE_CANCELED 연대 종결) 실측 (§54)
2. **AMR 일반 실패 경로(FAILED/REJECTED, STEP≥20)**: MAGAZINE_NOT_FOUND 외 실패의 MES 보고 방식은 오류 코드 체계 협의 후 (사양서 "취소·오류" 시트 §3 단서). ACS 측 resultCode 계약은 v0.3 §6 (§53)
3. **AMR 벤더 v0.3 협의**: ARRIVED reply 발행(실기 status 에 currentNodeId/pose 미탑재 — ARRIVED reply 기반 노드 동기화로 대응 중, §54), 일반 moveCmd COMPLETED 시점(작업 완료만), 게이트 대기 상한, resultCode 21/22/30/31/32 (`docs/ACS-AMR_mqtt_exchange.md` §7)
3-1. **구현사양서 §2.4 상태 모델 정정**: 실구현은 EXCHANGE_ASSIGNED + STEP 유지 (D5 격리) — 사양서 표기와 상이 (§54)
4. 시뮬레이터 status 발행 타이머 안정성 관찰 (§50 부수 관찰 — 1회 무발행 발생, 원인 미상). 설정 패널 조작 시 **차량 행 선택 선행** 필수 (§50 주의사항)
5. UI 릴리스: 슬롯 상세 표시를 설치본(Velopack)에 반영 — releases/ui 절차
