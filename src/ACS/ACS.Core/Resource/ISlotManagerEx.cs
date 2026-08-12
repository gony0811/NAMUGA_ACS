using System;
using System.Collections.Generic;
using ACS.Core.Resource.Model;

namespace ACS.Core.Resource
{
    /// <summary>
    /// EXCHANGE(v2): AMR 슬롯(NA_R_VEHICLE_SLOT) 점유 관리 — 모든 슬롯 전이의 단일 진입점.
    /// 점유 갱신을 액티비티마다 직접 DAO 로 하면 정합이 깨진다 (§4.7 규율).
    /// </summary>
    public interface ISlotManagerEx
    {
        /// <summary>차량의 슬롯 4행 조회 (slotNo 오름차순)</summary>
        IList<VehicleSlotEx> GetSlots(string vehicleId);

        /// <summary>미시드 차량 대비: 슬롯 4행이 없으면 생성 (idempotent)</summary>
        void EnsureSlots(string vehicleId);

        /// <summary>배차 적격: 4슬롯 전부 EMPTY (트립 중간 상태 차량 배제)</summary>
        bool AreAllSlotsEmpty(string vehicleId);

        /// <summary>
        /// INSERT 군 빈 슬롯 1개 + RETRIEVE 군 빈 슬롯 1개를 jobId 로 예약(OCCUPIED 아님 — jobId 기록).
        /// 둘 중 하나라도 없으면 null. (load, unload) 슬롯 번호 반환.
        /// </summary>
        Tuple<int, int> ReserveExchangePair(string vehicleId, string jobId);

        /// <summary>실물 적재: EMPTY→OCCUPIED (phase: NEW=투입슬롯 적재, OLD=회수슬롯 적재)</summary>
        void Occupy(string vehicleId, int slotNo, string jobId, string phase);

        /// <summary>실물 하치: OCCUPIED→EMPTY (jobId/phase 해제)</summary>
        void Release(string vehicleId, int slotNo);

        /// <summary>실패/취소 정리: 해당 jobId 가 예약·점유한 모든 슬롯 해제</summary>
        void ReleaseAllByJobId(string jobId);
    }
}
