using System;

namespace ACS.Core.Transfer
{
    /// <summary>EI 측 AMR reply 라우팅 결정 (status 기준).</summary>
    public enum AmrReplyAction
    {
        /// <summary>ACCEPTED / EXECUTING / 미정의 status — 로그만.</summary>
        Ignore,
        /// <summary>ARRIVED → RAIL-VEHICLEARRIVED.</summary>
        RouteArrived,
        /// <summary>COMPLETED / STEP_COMPLETE → RAIL-VEHICLE{ACQUIRE|DEPOSIT|EXCHANGE}COMPLETED.</summary>
        RouteCompleted,
        /// <summary>FAILED / REJECTED → (EXCHANGE TC 한정) RAIL-VEHICLEJOBFAILED.</summary>
        RouteFailed,
        /// <summary>CANCELED → 로그만 (Trans 는 reply 대기 없이 취소 처리 완료).</summary>
        LogCanceled
    }

    /// <summary>Trans 측 FAILED/REJECTED 처리 결정.</summary>
    public enum AmrFailedDisposition
    {
        /// <summary>FAILED@STEP10 (Origin 픽업 실패) → JOBREPORT COMPLETE(ErrorCode=MAGAZINE_NOT_FOUND) + TC 종결.</summary>
        MagazineNotFound,
        /// <summary>REJECTED@STEP10 (실물 이동 전 안전 구간) → TC EXCHANGE_QUEUED 롤백 + 슬롯 해제 + 차량 IDLE → 다음 틱 재배차.</summary>
        RollbackToQueued,
        /// <summary>그 외 → 로그만 (해당 STEP 정지 + 운영자 개입).</summary>
        LogOnly
    }

    /// <summary>
    /// AMR reply(status/resultCode/step) 해석 규약의 단일 출처 (docs/ACS-AMR_mqtt_exchange.md v0.3 §5).
    /// 순수 로직 — EI(HandleAmrReplyActivity)와 Trans(RailVehicleJobfailedWorkflow)가 같은 표를 참조한다.
    /// </summary>
    public static class AmrReplyPolicy
    {
        // ---- status ----
        public const string STATUS_ACCEPTED = "ACCEPTED";
        public const string STATUS_EXECUTING = "EXECUTING";
        public const string STATUS_ARRIVED = "ARRIVED";
        public const string STATUS_STEP_COMPLETE = "STEP_COMPLETE";
        public const string STATUS_COMPLETED = "COMPLETED";
        public const string STATUS_REJECTED = "REJECTED";
        public const string STATUS_FAILED = "FAILED";
        public const string STATUS_CANCELED = "CANCELED";

        // ---- resultCode (v0.3 §8.1) ----
        public const int RC_OK = 0;
        public const int RC_UNSUPPORTED_COMMAND = 2;   // REJECTED
        public const int RC_MODBUS_DISCONNECTED = 10;  // REJECTED
        public const int RC_BUSY = 11;                 // REJECTED (Idle 아님)
        public const int RC_NODE_UNMAPPED = 20;        // REJECTED
        public const int RC_SLOT_MISMATCH = 21;        // REJECTED (수락 단계 슬롯 점유 불일치)
        public const int RC_COBOT_NOT_READY = 22;      // REJECTED
        public const int RC_MAGAZINE_NOT_FOUND = 30;   // FAILED
        public const int RC_SLOT_STATE_MISMATCH = 31;  // FAILED (시퀀스 중)
        public const int RC_GATE_TIMEOUT = 32;         // FAILED (actionCmd 대기 상한)
        public const int RC_CANCEL_REJECTED = 40;      // CANCELED (취소 불가)
        public const int RC_INTERNAL_ERROR = 99;       // FAILED

        /// <summary>EI: status → 라우팅 결정. 대소문자 무시, null/빈값은 Ignore.</summary>
        public static AmrReplyAction Route(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return AmrReplyAction.Ignore;

            if (Eq(status, STATUS_COMPLETED) || Eq(status, STATUS_STEP_COMPLETE))
                return AmrReplyAction.RouteCompleted;
            if (Eq(status, STATUS_ARRIVED))
                return AmrReplyAction.RouteArrived;
            if (Eq(status, STATUS_FAILED) || Eq(status, STATUS_REJECTED))
                return AmrReplyAction.RouteFailed;
            if (Eq(status, STATUS_CANCELED))
                return AmrReplyAction.LogCanceled;
            return AmrReplyAction.Ignore;
        }

        /// <summary>STEP_COMPLETE 는 step 필수 — 없으면 라우팅하지 않는다.</summary>
        public static bool RequiresStep(string status)
        {
            return Eq(status, STATUS_STEP_COMPLETE);
        }

        /// <summary>
        /// EXCHANGE TC 의 현재 STEP → 이 구간의 완료 reply 가 의미하는 jobType.
        /// 10=Origin 픽업(UNLOAD), 20~40=설비 구간(EXCHANGE), 50=반납(LOAD), 그 외 null.
        /// 반환값은 TransportCommandEx.JOBTYPE_* 과 동일 문자열.
        /// </summary>
        public static string ResolveExchangeJobType(int step)
        {
            switch (step)
            {
                case ExchangeSteps.STEP_PICKUP_NEW: return "UNLOAD";
                case ExchangeSteps.STEP_MOVE_TO_EQUIP:
                case ExchangeSteps.STEP_UNLOAD_OLD:
                case ExchangeSteps.STEP_LOAD_NEW: return "EXCHANGE";
                case ExchangeSteps.STEP_RETURN_OLD: return "LOAD";
                default: return null;
            }
        }

        /// <summary>
        /// Trans: FAILED/REJECTED 처리 결정.
        /// 비EXCHANGE 는 항상 LogOnly (기존 정책: 정지 + 운영자).
        /// EXCHANGE: FAILED@STEP10 → MagazineNotFound, REJECTED@STEP10 → RollbackToQueued, 그 외 LogOnly.
        /// resultCode 는 결정에 쓰지 않는다 (AMR 코드 체계 미확정분 방어 — 로그에만 남김).
        /// </summary>
        public static AmrFailedDisposition DecideFailed(string status, bool isExchange, int step)
        {
            if (!isExchange || step != ExchangeSteps.STEP_PICKUP_NEW)
                return AmrFailedDisposition.LogOnly;
            if (Eq(status, STATUS_FAILED))
                return AmrFailedDisposition.MagazineNotFound;
            if (Eq(status, STATUS_REJECTED))
                return AmrFailedDisposition.RollbackToQueued;
            return AmrFailedDisposition.LogOnly;
        }

        /// <summary>CANCELED reply 가 취소 거부(resultCode=40)인지.</summary>
        public static bool IsCancelRejected(int resultCode)
        {
            return resultCode == RC_CANCEL_REJECTED;
        }

        private static bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
