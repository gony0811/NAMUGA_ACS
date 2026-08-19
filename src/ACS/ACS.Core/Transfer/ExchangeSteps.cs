using System;

namespace ACS.Core.Transfer
{
    /// <summary>
    /// EXCHANGE(v2) 단계(STEP) 규약의 단일 출처 (ACS_EXCHANGE_구현사양서.md §2.1).
    /// 여정: 10 PICKUP_NEW → 20 MOVE_TO_EQUIP → 30 UNLOAD_OLD → 40 LOAD_NEW → 50 RETURN_OLD → 60 DONE.
    /// TC 상태는 여정 내내 EXCHANGE_ASSIGNED 를 유지하고(D5 상태 격리),
    /// 단계 추적은 ExchangeInfo.KEY_STEP 단독으로 한다. 이 클래스는 순수 로직만 담는다.
    /// </summary>
    public static class ExchangeSteps
    {
        public const int STEP_PICKUP_NEW = 10;
        public const int STEP_MOVE_TO_EQUIP = 20;
        public const int STEP_UNLOAD_OLD = 30;
        public const int STEP_LOAD_NEW = 40;
        public const int STEP_RETURN_OLD = 50;
        public const int STEP_DONE = 60;

        public const string NAME_PICKUP_NEW = "PICKUP_NEW";
        public const string NAME_MOVE_TO_EQUIP = "MOVE_TO_EQUIP";
        public const string NAME_UNLOAD_OLD = "UNLOAD_OLD";
        public const string NAME_LOAD_NEW = "LOAD_NEW";
        public const string NAME_RETURN_OLD = "RETURN_OLD";
        public const string NAME_DONE = "DONE";

        /// <summary>단계 번호 → StepName. 미정의 값은 "".</summary>
        public static string StepName(int step)
        {
            switch (step)
            {
                case STEP_PICKUP_NEW: return NAME_PICKUP_NEW;
                case STEP_MOVE_TO_EQUIP: return NAME_MOVE_TO_EQUIP;
                case STEP_UNLOAD_OLD: return NAME_UNLOAD_OLD;
                case STEP_LOAD_NEW: return NAME_LOAD_NEW;
                case STEP_RETURN_OLD: return NAME_RETURN_OLD;
                case STEP_DONE: return NAME_DONE;
                default: return "";
            }
        }

        /// <summary>
        /// AdditionalInfo 에서 현재 STEP 을 파싱한다. 키 부재/비정수는 0 (호출자가 무효 처리).
        /// </summary>
        public static int GetStep(string additionalInfo)
        {
            string raw = ExchangeInfo.Get(additionalInfo, ExchangeInfo.KEY_STEP);
            int step;
            return int.TryParse(raw, out step) ? step : 0;
        }

        /// <summary>
        /// 설비(mid) LocationId 조립: MidLoc(EquipId) + ":" + MidPortId.
        /// 둘 중 하나라도 비어 있으면 Location 조회가 불가능하므로 "" 반환.
        /// </summary>
        public static string BuildMidLocationId(string midLoc, string midPortId)
        {
            if (string.IsNullOrWhiteSpace(midLoc) || string.IsNullOrWhiteSpace(midPortId))
                return "";
            return midLoc.Trim() + ":" + midPortId.Trim();
        }

        /// <summary>
        /// 도착(ARRIVED) 판정: 현재 STEP 이 기대하는 waypoint 의 StationId 와
        /// 차량 현재 노드가 일치할 때만 해당 step 을 반환. 그 외 null (보고 생략).
        ///  - STEP=10(origin 이동 중) ↔ sourceStationId
        ///  - STEP=20(설비 이동 중)   ↔ midStationId
        ///  - STEP=50(반납 이동 중)   ↔ destStationId
        /// </summary>
        public static int? ResolveArrivedStep(int currentStep, string currentNodeId,
            string sourceStationId, string midStationId, string destStationId)
        {
            if (string.IsNullOrEmpty(currentNodeId))
                return null;

            switch (currentStep)
            {
                case STEP_PICKUP_NEW:
                    return NodeEquals(currentNodeId, sourceStationId) ? (int?)STEP_PICKUP_NEW : null;
                case STEP_MOVE_TO_EQUIP:
                    return NodeEquals(currentNodeId, midStationId) ? (int?)STEP_MOVE_TO_EQUIP : null;
                case STEP_RETURN_OLD:
                    return NodeEquals(currentNodeId, destStationId) ? (int?)STEP_RETURN_OLD : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 보고에 실을 CarrierSlot: 신자재 단계(10 픽업/40 투입)=loadSlot,
        /// 구자재 단계(30 회수/50 반납)=unloadSlot, 이동/완료 단계(20/60)="".
        /// </summary>
        public static string CarrierSlotFor(int step, string loadSlot, string unloadSlot)
        {
            switch (step)
            {
                case STEP_PICKUP_NEW:
                case STEP_LOAD_NEW:
                    return loadSlot ?? "";
                case STEP_UNLOAD_OLD:
                case STEP_RETURN_OLD:
                    return unloadSlot ?? "";
                default:
                    return "";
            }
        }

        /// <summary>stuck 복구 시 재푸시할 구간 (ResolveRecoverySegment 결과).</summary>
        public sealed class RecoverySegment
        {
            /// <summary>moveCmd jobType: UNLOAD(→Origin) / EXCHANGE(→Mid) / LOAD(→Dest)</summary>
            public string JobType { get; }
            /// <summary>대상 waypoint: SOURCE / MID / DEST</summary>
            public string Target { get; }
            /// <summary>사용할 슬롯 키: ExchangeInfo.KEY_LOADSLOT / KEY_UNLOADSLOT</summary>
            public string SlotKey { get; }
            public RecoverySegment(string jobType, string target, string slotKey)
            {
                JobType = jobType; Target = target; SlotKey = slotKey;
            }
        }

        public const string TARGET_SOURCE = "SOURCE";
        public const string TARGET_MID = "MID";
        public const string TARGET_DEST = "DEST";

        /// <summary>
        /// stuck 복구(RecoverStuckVehicles) 용: 현재 STEP/ACT 로부터 재푸시할 이동 구간을 유도한다.
        /// 재푸시 대상이 아니면 null.
        ///  - STEP=10                         → UNLOAD  /SOURCE/LOADSLOT   (Origin 픽업행)
        ///  - STEP=20 &amp;&amp; ACT 빈값 &amp;&amp; 현재≠mid → EXCHANGE/MID/LOADSLOT     (설비행). ACT 설정 = 설비 게이트 대기 중 → 재푸시 금지
        ///  - STEP=50                         → LOAD    /DEST/UNLOADSLOT   (반납행)
        ///  - 그 외(30/40/60, 20 이면서 이미 mid 에 있음)         → null
        /// jobType 문자열은 TransportCommandEx.JOBTYPE_* 과 동일 값("UNLOAD"/"EXCHANGE"/"LOAD").
        /// </summary>
        public static RecoverySegment ResolveRecoverySegment(int step, string act, string currentNodeId, string midStationId)
        {
            switch (step)
            {
                case STEP_PICKUP_NEW:
                    return new RecoverySegment("UNLOAD", TARGET_SOURCE, ExchangeInfo.KEY_LOADSLOT);
                case STEP_MOVE_TO_EQUIP:
                    if (!string.IsNullOrEmpty(act))
                        return null; // 설비 액션 진행/대기 중 — 이동 재푸시 금지
                    if (NodeEquals(currentNodeId ?? "", midStationId))
                        return null; // 이미 설비 노드 — MES ACTIONCMD 대기 중
                    return new RecoverySegment("EXCHANGE", TARGET_MID, ExchangeInfo.KEY_LOADSLOT);
                case STEP_RETURN_OLD:
                    return new RecoverySegment("LOAD", TARGET_DEST, ExchangeInfo.KEY_UNLOADSLOT);
                default:
                    return null;
            }
        }

        private static bool NodeEquals(string a, string b)
        {
            return !string.IsNullOrEmpty(b) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
