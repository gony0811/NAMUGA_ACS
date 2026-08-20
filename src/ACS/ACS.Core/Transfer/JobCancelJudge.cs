using System;
using ACS.Core.Transfer.Model;

namespace ACS.Core.Transfer
{
    /// <summary>JOBCANCEL 판정 결과 (시나리오 사양서 "취소·오류" 시트 C1~C4).</summary>
    public enum JobCancelVerdict
    {
        /// <summary>C1: 배차 전 — 즉시 취소 (이력 이관 후 삭제).</summary>
        CancelBeforeAssign,
        /// <summary>C2: 픽업 전 (이동 중) — 즉시 취소 + 반송 중지 + 차량 IDLE.</summary>
        CancelBeforePickup,
        /// <summary>C3: 적재 후 — 승인 보고 + 충전소 복귀 + Job 삭제 + 차량 ALARM (작업자 실물 회수).</summary>
        CancelAfterLoad,
        /// <summary>C5: 배칭 중 1건 적재 후 취소 — C3 수행 + 페어 Job 연대 종결(COMPLETE + EXCHANGE_CANCELED).</summary>
        CancelAfterLoadBatch,
        /// <summary>C4: 종료/취소 진행 상태 — 거부 (ErrorCode=CANCEL_REJECTED).</summary>
        Reject
    }

    /// <summary>
    /// JOBCANCEL 취소 가부 판정 순수 로직 — EXCHANGE·일반 반송 공통.
    /// 입력만으로 결정되며 부수효과 없음 (실행은 Trans 측 액티비티 책임).
    /// 적재 여부는 슬롯 OCCUPIED 기준 (LoadedTime 은 생성자 기본값 함정이 있어 사용하지 않음).
    /// </summary>
    public static class JobCancelJudge
    {
        /// <summary>취소 승인 ErrorCode.</summary>
        public const string ERR_OK = "0";
        /// <summary>취소 거부 ErrorCode (종료 상태).</summary>
        public const string ERR_CANCEL_REJECTED = "CANCEL_REJECTED";
        /// <summary>Source 매거진 부재 — 즉시 종결 (Abnormal, "취소·오류" 시트 §2).</summary>
        public const string ERR_MAGAZINE_NOT_FOUND = "MAGAZINE_NOT_FOUND";
        /// <summary>배칭 페어 연대 종결 ErrorCode (C5 — TRIP 배칭 도입 시 사용 예약).</summary>
        public const string ERR_EXCHANGE_CANCELED = "EXCHANGE_CANCELED";
        /// <summary>히스토리 이관 사유 태그.</summary>
        public const string CAUSE_JOBCANCEL = "JOBCANCEL";

        /// <summary>
        /// C1~C5 판정.
        /// </summary>
        /// <param name="tcState">TC 상태 (null/빈값 = TC 부재 → 거부)</param>
        /// <param name="jobType">TC JobType (EXCHANGE 분기용)</param>
        /// <param name="exchangeStep">EXCHANGE STEP (ExchangeSteps.GetStep 결과; 비EXCHANGE 는 무시)</param>
        /// <param name="anySlotOccupied">차량 슬롯 중 OCCUPIED 존재 여부 (실물 적재 판단)</param>
        /// <param name="hasActiveTripMate">배칭 트립의 다른 활성 TC 존재 여부 — 적재 후 취소가 C5 로 승격</param>
        public static JobCancelVerdict Judge(string tcState, string jobType, int exchangeStep, bool anySlotOccupied,
            bool hasActiveTripMate = false)
        {
            if (string.IsNullOrEmpty(tcState))
                return JobCancelVerdict.Reject;

            // C4: 종료·취소 진행 상태 — Job 불변, 거부
            if (Is(tcState, TransportCommandEx.STATE_COMPLETED)
                || Is(tcState, TransportCommandEx.STATE_CANCELED)
                || Is(tcState, TransportCommandEx.STATE_CANCELING)
                || Is(tcState, TransportCommandEx.STATE_ABORTED)
                || Is(tcState, TransportCommandEx.STATE_ABORTING)
                || Is(tcState, TransportCommandEx.STATE_COMPLETEFAILED)
                || Is(tcState, TransportCommandEx.STATE_CHARGE_COMPLETED))
                return JobCancelVerdict.Reject;

            // C1: 배차 전 (차량 미할당 대기 상태)
            if (Is(tcState, TransportCommandEx.STATE_CREATED)
                || Is(tcState, TransportCommandEx.STATE_QUEUED)
                || Is(tcState, TransportCommandEx.STATE_WAITING)
                || Is(tcState, TransportCommandEx.STATE_PREASSIGNED)
                || Is(tcState, TransportCommandEx.STATE_EXCHANGE_QUEUED))
                return JobCancelVerdict.CancelBeforeAssign;

            // EXCHANGE 여정: STEP=10(픽업 전) & 실물 미적재 → C2, 그 외(20~50 또는 적재) → C3
            // 배칭 트립에 다른 활성 TC 가 있으면 적재 후 취소는 C5 (페어 연대 종결)
            if (Is(tcState, TransportCommandEx.STATE_EXCHANGE_ASSIGNED))
            {
                if (exchangeStep == ExchangeSteps.STEP_PICKUP_NEW && !anySlotOccupied)
                    return JobCancelVerdict.CancelBeforePickup;
                return hasActiveTripMate ? JobCancelVerdict.CancelAfterLoadBatch : JobCancelVerdict.CancelAfterLoad;
            }

            // 일반 반송: 적재 전(소스행) → C2, 적재 후(목적지행) → C3
            if (Is(tcState, TransportCommandEx.STATE_ASSIGNED)
                || Is(tcState, TransportCommandEx.STATE_ARRIVED_SOURCE)
                || Is(tcState, TransportCommandEx.STATE_TRANSFERRING_SOURCE))
                return JobCancelVerdict.CancelBeforePickup;

            if (Is(tcState, TransportCommandEx.STATE_ARRIVED_DEST)
                || Is(tcState, TransportCommandEx.STATE_TRANSFERRING_DEST))
                return JobCancelVerdict.CancelAfterLoad;

            // 알 수 없는 상태 — 안전측 거부
            return JobCancelVerdict.Reject;
        }

        private static bool Is(string state, string constant)
        {
            return constant.Equals(state, StringComparison.OrdinalIgnoreCase);
        }
    }
}
