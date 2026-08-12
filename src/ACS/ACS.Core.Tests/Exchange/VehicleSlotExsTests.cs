using System;
using System.Collections.Generic;
using ACS.Core.Resource.Model;
using Xunit;

namespace ACS.Core.Tests
{
    /// <summary>
    /// EXCHANGE(v2) S2: 슬롯 선택 순수 로직(VehicleSlotExs) 단위 테스트.
    /// 참조: ACS_EXCHANGE_구현사양서.md §4.7 DoD
    /// </summary>
    public class VehicleSlotExsTests
    {
        private static VehicleSlotEx Slot(int no, string state, string jobId = null)
        {
            return new VehicleSlotEx
            {
                VehicleId = "AMR01",
                SlotNo = no,
                Role = VehicleSlotExs.RoleOf(no),
                State = state,
                JobId = jobId,
                UpdatedTime = DateTime.UtcNow
            };
        }

        private static IList<VehicleSlotEx> AllEmpty()
        {
            return new List<VehicleSlotEx>
            {
                Slot(1, VehicleSlotEx.STATE_EMPTY),
                Slot(2, VehicleSlotEx.STATE_EMPTY),
                Slot(3, VehicleSlotEx.STATE_EMPTY),
                Slot(4, VehicleSlotEx.STATE_EMPTY)
            };
        }

        [Theory]
        [InlineData(1, "INSERT")]
        [InlineData(2, "INSERT")]
        [InlineData(3, "RETRIEVE")]
        [InlineData(4, "RETRIEVE")]
        public void RoleOf_ValidSlots(int slotNo, string expected)
        {
            Assert.Equal(expected, VehicleSlotExs.RoleOf(slotNo));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(-1)]
        public void RoleOf_InvalidSlots_ReturnsNull(int slotNo)
        {
            Assert.Null(VehicleSlotExs.RoleOf(slotNo));
        }

        [Fact]
        public void AreAllEmpty_AllEmpty_True()
        {
            Assert.True(VehicleSlotExs.AreAllEmpty(AllEmpty()));
        }

        [Fact]
        public void AreAllEmpty_OneOccupied_False()
        {
            var slots = AllEmpty();
            slots[2].State = VehicleSlotEx.STATE_OCCUPIED;
            Assert.False(VehicleSlotExs.AreAllEmpty(slots));
        }

        [Fact]
        public void AreAllEmpty_MissingRows_False()
        {
            // 슬롯 행이 4개 미만(미시드 차량)이면 배차 부적격
            var slots = new List<VehicleSlotEx> { Slot(1, VehicleSlotEx.STATE_EMPTY) };
            Assert.False(VehicleSlotExs.AreAllEmpty(slots));
            Assert.False(VehicleSlotExs.AreAllEmpty(new List<VehicleSlotEx>()));
            Assert.False(VehicleSlotExs.AreAllEmpty(null));
        }

        [Fact]
        public void SelectExchangePair_AllEmpty_Returns1And3()
        {
            // 교환A = 슬롯1 + 슬롯3 (낮은 번호 우선, D3)
            var pair = VehicleSlotExs.SelectExchangePair(AllEmpty());
            Assert.NotNull(pair);
            Assert.Equal(1, pair.Item1);
            Assert.Equal(3, pair.Item2);
        }

        [Fact]
        public void SelectExchangePair_FirstPairTaken_Returns2And4()
        {
            // 교환B = 슬롯2 + 슬롯4 (슬롯1·3 점유 시)
            var slots = AllEmpty();
            slots[0].State = VehicleSlotEx.STATE_OCCUPIED;
            slots[2].State = VehicleSlotEx.STATE_OCCUPIED;
            var pair = VehicleSlotExs.SelectExchangePair(slots);
            Assert.NotNull(pair);
            Assert.Equal(2, pair.Item1);
            Assert.Equal(4, pair.Item2);
        }

        [Fact]
        public void SelectExchangePair_InsertGroupFull_ReturnsNull()
        {
            // INSERT 군(1·2)만 다 찬 경우 — 페어 불가 (§4.7 DoD 명시 케이스)
            var slots = AllEmpty();
            slots[0].State = VehicleSlotEx.STATE_OCCUPIED;
            slots[1].State = VehicleSlotEx.STATE_OCCUPIED;
            Assert.Null(VehicleSlotExs.SelectExchangePair(slots));
        }

        [Fact]
        public void SelectExchangePair_RetrieveGroupFull_ReturnsNull()
        {
            var slots = AllEmpty();
            slots[2].State = VehicleSlotEx.STATE_OCCUPIED;
            slots[3].State = VehicleSlotEx.STATE_OCCUPIED;
            Assert.Null(VehicleSlotExs.SelectExchangePair(slots));
        }

        [Fact]
        public void SelectExchangePair_UnorderedInput_LowestFirst()
        {
            // 입력 순서와 무관하게 낮은 번호 우선
            var slots = new List<VehicleSlotEx>
            {
                Slot(4, VehicleSlotEx.STATE_EMPTY),
                Slot(2, VehicleSlotEx.STATE_EMPTY),
                Slot(3, VehicleSlotEx.STATE_EMPTY),
                Slot(1, VehicleSlotEx.STATE_EMPTY)
            };
            var pair = VehicleSlotExs.SelectExchangePair(slots);
            Assert.Equal(1, pair.Item1);
            Assert.Equal(3, pair.Item2);
        }

        [Fact]
        public void SelectExchangePair_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(VehicleSlotExs.SelectExchangePair(null));
            Assert.Null(VehicleSlotExs.SelectExchangePair(new List<VehicleSlotEx>()));
        }
    }
}
