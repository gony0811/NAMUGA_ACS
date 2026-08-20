# MCS(MES)-ACS 통신 포맷 사양서 v1.0 (2026-07-28)

> 정본은 xlsx (MCS-ACS_통신포맷_사양서.xlsx). 본 문서는 세션 간 참조용 요약.

## 메시지 목록
| Command | 방향 | 용도 | 근거 |
|---|---|---|---|
| MOVECMD | MCS→ACS | 일반 반송 (LOAD/UNLOAD, 1캐리어) | 기존 유지 |
| EXCHANGECMD | MCS→ACS | 매거진 교체 (1 Job, 슬롯 필드 공백=ACS 자동배정) | D1·D10 |
| ACTIONCMD | MCS→ACS | 설비 준비신호 중계 (STEP 20→UNLOAD, 30→LOAD 게이팅) | D11 |
| JOBCANCEL | MCS→ACS | 반송 취소 — EXCHANGE·MOVECMD 공통 | D13 |
| JOBREPORT | ACS→MCS | 응답·단계 보고 (Type/Step/StepName/CarrierSlot/ErrorCode) | 확장 |

## EXCHANGE 단계 보고
10 RECEIVE/PICKUP_NEW → START → 20 ARRIVED/MOVE_TO_EQUIP → 30 STEP_COMPLETE/UNLOAD_OLD(slot 3|4) → 40 LOAD_NEW(slot 1|2) → 50 RETURN_OLD(MOVE, 3|4) → 60 COMPLETE/DONE

## JOBCANCEL 판정 (공통)
- C1 배차 전(QUEUED/EXCHANGE_QUEUED): 즉시 취소 → CANCEL(0)
- C2 픽업 전(ASSIGNED): 즉시 취소·예약 해제·차량 IDLE → CANCEL(0)
- C3 적재 후(EXCHANGE=슬롯 점유 / MOVECMD=FullState=FULL): 충전소 복귀 + Job 삭제 + 차량 ALARM → 작업자 조치. CANCEL(0)+알람
- C4 종료 상태: 거부 → CANCEL_REJECTED
- C5 [EXCHANGE] 배칭 1건(적재 후): 반송 전체 중단, 페어 Job COMPLETE+EXCHANGE_CANCELED

## Abnormal
- MAGAZINE_NOT_FOUND: Source 매거진 부재 → JOBREPORT(COMPLETE, MAGAZINE_NOT_FOUND) 즉시 종결, 재시도 없음(MCS 재요청)

## 오류 코드
0 정상 · 21 DESTMACHINENOTFOUND · 22 NOTSAMEBAY · 25 SOURCEMACHINENOTFOUND · 102 COMMANDALREADYREQUESTED · 106 SOURCEDESTMACHINEDUPLICATE · 200 OPERATOR_ABORT · 신규(코드값 협의): MAGAZINE_NOT_FOUND / CANCEL_REJECTED / EXCHANGE_CANCELED

## 협의 필요
1. MOVECANCEL ↔ JOBCANCEL 관계 (통일 vs 병존)
2. 신규 오류 코드 값 체계 (문자열 vs 숫자)
