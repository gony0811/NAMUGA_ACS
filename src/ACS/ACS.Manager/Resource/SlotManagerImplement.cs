using System;
using System.Collections;
using System.Collections.Generic;
using ACS.Core.Base;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;

namespace ACS.Manager.Resource
{
    /// <summary>
    /// EXCHANGE(v2): NA_R_VEHICLE_SLOT 점유 관리 구현 — 슬롯 전이의 단일 진입점.
    /// 선택 판정은 VehicleSlotExs(순수 로직)에 위임하고 여기서는 영속만 담당한다.
    /// 갱신 시각은 UTC (memory.md 규율). 모든 전이는 INFO 로그.
    /// 참조: ACS_EXCHANGE_구현사양서.md §4.7
    /// </summary>
    public class SlotManagerImplement : AbstractManager, ISlotManagerEx
    {
        public IList<VehicleSlotEx> GetSlots(string vehicleId)
        {
            var result = new List<VehicleSlotEx>();
            IList rows = this.PersistentDao.FindByAttributeOrderBy(
                typeof(VehicleSlotEx), "VehicleId", vehicleId, "SlotNo");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row is VehicleSlotEx slot) result.Add(slot);
                }
            }
            return result;
        }

        public void EnsureSlots(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId)) return;
            var existing = GetSlots(vehicleId);
            if (existing.Count >= 4) return;

            var have = new HashSet<int>();
            foreach (var s in existing) have.Add(s.SlotNo);

            for (int no = 1; no <= 4; no++)
            {
                if (have.Contains(no)) continue;
                var slot = new VehicleSlotEx
                {
                    VehicleId = vehicleId,
                    SlotNo = no,
                    Role = VehicleSlotExs.RoleOf(no),
                    State = VehicleSlotEx.STATE_EMPTY,
                    JobId = null,
                    Phase = null,
                    UpdatedTime = DateTime.UtcNow
                };
                this.PersistentDao.Save(slot);
                logger.Info($"SlotManager: slot seeded vehicle={vehicleId}, slotNo={no}, role={slot.Role}");
            }
        }

        public bool AreAllSlotsEmpty(string vehicleId)
        {
            return VehicleSlotExs.AreAllEmpty(GetSlots(vehicleId));
        }

        public Tuple<int, int> ReserveExchangePair(string vehicleId, string jobId)
        {
            var slots = GetSlots(vehicleId);
            var pair = VehicleSlotExs.SelectExchangePair(slots);
            if (pair == null)
            {
                logger.Warn($"SlotManager: reserve failed (no empty pair) vehicle={vehicleId}, job={jobId}");
                return null;
            }

            UpdateSlot(slots, pair.Item1, s => { s.JobId = jobId; });
            UpdateSlot(slots, pair.Item2, s => { s.JobId = jobId; });
            logger.Info($"SlotManager: pair reserved vehicle={vehicleId}, job={jobId}, loadSlot={pair.Item1}, unloadSlot={pair.Item2}");
            return pair;
        }

        public void Occupy(string vehicleId, int slotNo, string jobId, string phase)
        {
            var slots = GetSlots(vehicleId);
            UpdateSlot(slots, slotNo, s =>
            {
                s.State = VehicleSlotEx.STATE_OCCUPIED;
                s.JobId = jobId;
                s.Phase = phase;
            });
            logger.Info($"SlotManager: occupy vehicle={vehicleId}, slotNo={slotNo}, job={jobId}, phase={phase}");
        }

        public void Release(string vehicleId, int slotNo)
        {
            var slots = GetSlots(vehicleId);
            UpdateSlot(slots, slotNo, s =>
            {
                s.State = VehicleSlotEx.STATE_EMPTY;
                s.JobId = null;
                s.Phase = null;
            });
            logger.Info($"SlotManager: release vehicle={vehicleId}, slotNo={slotNo}");
        }

        public void ReleaseAllByJobId(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;
            IList rows = this.PersistentDao.FindByAttribute(typeof(VehicleSlotEx), "JobId", jobId);
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row is not VehicleSlotEx slot) continue;
                slot.State = VehicleSlotEx.STATE_EMPTY;
                slot.JobId = null;
                slot.Phase = null;
                slot.UpdatedTime = DateTime.UtcNow;
                this.PersistentDao.Update(slot);
                logger.Info($"SlotManager: released by job cleanup vehicle={slot.VehicleId}, slotNo={slot.SlotNo}, job={jobId}");
            }
        }

        private void UpdateSlot(IList<VehicleSlotEx> slots, int slotNo, Action<VehicleSlotEx> mutate)
        {
            foreach (var s in slots)
            {
                if (s.SlotNo != slotNo) continue;
                mutate(s);
                s.UpdatedTime = DateTime.UtcNow;
                this.PersistentDao.Update(s);
                return;
            }
            logger.Error($"SlotManager: slot row not found slotNo={slotNo} (EnsureSlots 미실행?)");
        }
    }
}
