using System;
using System.Collections.Generic;
using System.Text;

namespace ACS.Core.Resource.Model
{
    /// <summary>
    /// EXCHANGE(v2): AMR 상면 슬롯(1~4) 점유 엔티티 — NA_R_VEHICLE_SLOT.
    /// 슬롯 1·2 = 투입(INSERT, NEW 매거진), 3·4 = 회수(RETRIEVE, OLD 매거진). (D3)
    /// 상태 전이는 ISlotManagerEx 단일 진입점으로만 수행한다.
    /// 참조: ACS_EXCHANGE_구현사양서.md §2.3, §4.7
    /// </summary>
    public class VehicleSlotEx
    {
        public static string ROLE_INSERT = "INSERT";     // slotNo 1,2 — NEW 투입
        public static string ROLE_RETRIEVE = "RETRIEVE"; // slotNo 3,4 — OLD 회수
        public static string STATE_EMPTY = "EMPTY";
        public static string STATE_OCCUPIED = "OCCUPIED";
        public static string PHASE_NEW = "NEW";
        public static string PHASE_OLD = "OLD";

        public virtual long Id { get; set; }
        public virtual string VehicleId { get; set; }
        public virtual int SlotNo { get; set; }
        public virtual string Role { get; set; }
        public virtual string State { get; set; }
        public virtual string JobId { get; set; }
        public virtual string Phase { get; set; }
        public virtual DateTime UpdatedTime { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicleSlot{");
            sb.Append("vehicleId=").Append(this.VehicleId);
            sb.Append(", slotNo=").Append(this.SlotNo);
            sb.Append(", role=").Append(this.Role);
            sb.Append(", state=").Append(this.State);
            sb.Append(", jobId=").Append(this.JobId);
            sb.Append(", phase=").Append(this.Phase);
            sb.Append("}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// 슬롯 선택 순수 로직 — DB 무관, 단위 테스트 대상.
    /// SlotManagerImplement 가 조회한 슬롯 목록에 대해 판정만 수행한다.
    /// </summary>
    public static class VehicleSlotExs
    {
        /// <summary>슬롯 역할 판정: 1·2=INSERT, 3·4=RETRIEVE, 그 외=null</summary>
        public static string RoleOf(int slotNo)
        {
            if (slotNo == 1 || slotNo == 2) return VehicleSlotEx.ROLE_INSERT;
            if (slotNo == 3 || slotNo == 4) return VehicleSlotEx.ROLE_RETRIEVE;
            return null;
        }

        /// <summary>4슬롯 전부 EMPTY·미예약인가 (배차 적격 판정 — 슬롯 행이 4개 미만이면 false).
        /// 예약(EMPTY + jobId 기록) 잔류도 부적격 — 이중 배정 방지.</summary>
        public static bool AreAllEmpty(IList<VehicleSlotEx> slots)
        {
            if (slots == null || slots.Count < 4) return false;
            foreach (var s in slots)
            {
                if (!IsAvailable(s))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 교환 페어 선택: INSERT 군(1·2)에서 가용 슬롯 1개 + RETRIEVE 군(3·4)에서 가용 슬롯 1개.
        /// 낮은 번호 우선(교환A=1+3, 교환B=2+4). 둘 중 하나라도 없으면 null.
        /// 가용 = EMPTY 이면서 미예약(jobId 없음) — 배칭 시 선예약(A=1·3) 을 건너뛰어 B=2·4 가 되게 한다 (D3).
        /// 선택만 하고 상태는 바꾸지 않는다 — 영속 전이는 SlotManager 트랜잭션에서.
        /// </summary>
        public static Tuple<int, int> SelectExchangePair(IList<VehicleSlotEx> slots)
        {
            if (slots == null) return null;
            int load = 0, unload = 0;
            foreach (var s in Sorted(slots))
            {
                if (!IsAvailable(s)) continue;
                if (load == 0 && VehicleSlotEx.ROLE_INSERT.Equals(s.Role, StringComparison.OrdinalIgnoreCase))
                    load = s.SlotNo;
                else if (unload == 0 && VehicleSlotEx.ROLE_RETRIEVE.Equals(s.Role, StringComparison.OrdinalIgnoreCase))
                    unload = s.SlotNo;
            }
            if (load == 0 || unload == 0) return null;
            return Tuple.Create(load, unload);
        }

        /// <summary>슬롯 가용 판정: EMPTY 이면서 예약(jobId) 없음.</summary>
        private static bool IsAvailable(VehicleSlotEx s)
        {
            return VehicleSlotEx.STATE_EMPTY.Equals(s.State, StringComparison.OrdinalIgnoreCase)
                   && string.IsNullOrEmpty(s.JobId);
        }

        private static IEnumerable<VehicleSlotEx> Sorted(IList<VehicleSlotEx> slots)
        {
            var copy = new List<VehicleSlotEx>(slots);
            copy.Sort((a, b) => a.SlotNo.CompareTo(b.SlotNo));
            return copy;
        }
    }
}
