using System;
using System.Collections.Generic;

namespace ACS.Core.Transfer
{
    /// <summary>투어(트립) 다음 행동 종류.</summary>
    public enum ExchangeTourActionKind
    {
        /// <summary>대상 TC 의 Origin 으로 픽업행 (moveCmd UNLOAD, amrSlot=LOADSLOT).</summary>
        PickupMove,
        /// <summary>대상 TC 의 설비 구간 — STEP=20 이면 설비행 발행, 30/40 이면 게이트 대기(발행 없음).</summary>
        MidPhase,
        /// <summary>대상 TC 의 Dest 로 반납행 (moveCmd LOAD, amrSlot=UNLOADSLOT).</summary>
        DestMove,
        /// <summary>트립 내 활성 TC 없음 — 차량 초기화(종결).</summary>
        TripComplete
    }

    /// <summary>ExchangeTour.NextAfter 의 결과 — 다음 행동과 대상 TC.</summary>
    public sealed class ExchangeTourAction
    {
        public ExchangeTourActionKind Kind { get; }
        /// <summary>대상 TC JobId (TripComplete 이면 null).</summary>
        public string JobId { get; }
        /// <summary>대상 TC 의 현재 STEP (TripComplete 이면 0).</summary>
        public int Step { get; }

        public ExchangeTourAction(ExchangeTourActionKind kind, string jobId, int step)
        {
            Kind = kind;
            JobId = jobId;
            Step = step;
        }
    }

    /// <summary>
    /// EXCHANGE 배칭 투어의 "다음 할 일" 유도 순수 로직 (구현사양서 §4.10 / D9).
    /// 반송 순서 고정: 픽업들 → 설비들(각 게이트 2회) → 반납들.
    /// 입력은 트립 내 **활성**(미종결) TC 들의 (jobId, STEP) — LOADSLOT 오름차순 정렬 전제.
    /// STEP 조합만으로 항상 다음 행동이 유도되므로 crash 후에도 재개 가능 (별도 저장 상태 없음).
    /// 호출 시점은 완료 이벤트 직후·복구 시점으로 한정 — 이중 발행 없음.
    /// </summary>
    public static class ExchangeTour
    {
        /// <summary>vehicle.TransportCommandId 의 트립 ID prefix.</summary>
        public const string TRIP_PREFIX = "TRIP";

        /// <summary>트립 ID 생성 (구현사양서 §4.9: TRIP+yyyyMMddHHmmssfff).</summary>
        public static string NewTripId(DateTime now)
        {
            return TRIP_PREFIX + now.ToString("yyyyMMddHHmmssfff");
        }

        /// <summary>vehicle.TransportCommandId 가 트립 ID 인지 (2건 배칭 트립).</summary>
        public static bool IsTripId(string transportCommandId)
        {
            return !string.IsNullOrEmpty(transportCommandId)
                   && transportCommandId.StartsWith(TRIP_PREFIX, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 트립 내 활성 TC 들의 STEP 조합 → 다음 행동.
        ///  - STEP=10 인 TC 존재 → 그 TC 픽업행 (D9: 픽업들 먼저)
        ///  - 아니고 STEP∈{20,30,40} 인 TC 존재 → 그 TC 설비 구간 (20=설비행 발행 필요, 30/40=게이트 대기)
        ///  - 아니고 STEP=50 인 TC 존재 → 그 TC 반납행
        ///  - 없으면 TripComplete
        /// 같은 단계의 TC 가 여럿이면 입력 순서(LOADSLOT 오름차순 = 배차 순서)의 첫 TC.
        /// </summary>
        public static ExchangeTourAction NextAfter(IReadOnlyList<(string JobId, int Step)> activeTripTcs)
        {
            if (activeTripTcs == null || activeTripTcs.Count == 0)
                return new ExchangeTourAction(ExchangeTourActionKind.TripComplete, null, 0);

            foreach (var tc in activeTripTcs)
            {
                if (tc.Step == ExchangeSteps.STEP_PICKUP_NEW)
                    return new ExchangeTourAction(ExchangeTourActionKind.PickupMove, tc.JobId, tc.Step);
            }

            foreach (var tc in activeTripTcs)
            {
                if (tc.Step == ExchangeSteps.STEP_MOVE_TO_EQUIP
                    || tc.Step == ExchangeSteps.STEP_UNLOAD_OLD
                    || tc.Step == ExchangeSteps.STEP_LOAD_NEW)
                    return new ExchangeTourAction(ExchangeTourActionKind.MidPhase, tc.JobId, tc.Step);
            }

            foreach (var tc in activeTripTcs)
            {
                if (tc.Step == ExchangeSteps.STEP_RETURN_OLD)
                    return new ExchangeTourAction(ExchangeTourActionKind.DestMove, tc.JobId, tc.Step);
            }

            return new ExchangeTourAction(ExchangeTourActionKind.TripComplete, null, 0);
        }
    }
}
