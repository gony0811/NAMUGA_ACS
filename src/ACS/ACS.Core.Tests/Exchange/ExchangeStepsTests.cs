using ACS.Core.Transfer;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// EXCHANGE(v2) S5 — ExchangeSteps 순수 로직 테스트.
    /// 여정 규약: 10 PICKUP_NEW → 20 MOVE_TO_EQUIP → 30 UNLOAD_OLD →
    ///           40 LOAD_NEW → 50 RETURN_OLD → 60 DONE.
    /// 참조: ACS_EXCHANGE_구현사양서.md §2.1, docs/memory.md S5.
    /// </summary>
    public class ExchangeStepsTests
    {
        // ── 상수 가드: 값이 바뀌면 MES 보고 계약이 깨진다 ──

        [Fact]
        public void StepConstants_HaveContractValues()
        {
            Assert.Equal(10, ExchangeSteps.STEP_PICKUP_NEW);
            Assert.Equal(20, ExchangeSteps.STEP_MOVE_TO_EQUIP);
            Assert.Equal(30, ExchangeSteps.STEP_UNLOAD_OLD);
            Assert.Equal(40, ExchangeSteps.STEP_LOAD_NEW);
            Assert.Equal(50, ExchangeSteps.STEP_RETURN_OLD);
            Assert.Equal(60, ExchangeSteps.STEP_DONE);
        }

        [Theory]
        [InlineData(10, "PICKUP_NEW")]
        [InlineData(20, "MOVE_TO_EQUIP")]
        [InlineData(30, "UNLOAD_OLD")]
        [InlineData(40, "LOAD_NEW")]
        [InlineData(50, "RETURN_OLD")]
        [InlineData(60, "DONE")]
        public void StepName_MapsContractNames(int step, string expected)
        {
            Assert.Equal(expected, ExchangeSteps.StepName(step));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        [InlineData(70)]
        [InlineData(-10)]
        public void StepName_UndefinedStep_ReturnsEmpty(int step)
        {
            Assert.Equal("", ExchangeSteps.StepName(step));
        }

        // ── GetStep: AdditionalInfo 파싱 ──

        [Fact]
        public void GetStep_ParsesFromAdditionalInfo()
        {
            string info = ExchangeInfo.BuildInitial("EQL", "EQU"); // STEP=10
            Assert.Equal(10, ExchangeSteps.GetStep(info));

            string advanced = ExchangeInfo.Set(info, ExchangeInfo.KEY_STEP, "50");
            Assert.Equal(50, ExchangeSteps.GetStep(advanced));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("TRIP=;LOADSLOT=1")]          // STEP 키 부재
        [InlineData("STEP=;TRIP=")]               // 빈 값
        [InlineData("STEP=abc")]                  // 비정수
        public void GetStep_MissingOrInvalid_ReturnsZero(string info)
        {
            Assert.Equal(0, ExchangeSteps.GetStep(info));
        }

        // ── BuildMidLocationId ──

        [Fact]
        public void BuildMidLocationId_JoinsWithColon()
        {
            Assert.Equal("EQP01:LEFT", ExchangeSteps.BuildMidLocationId("EQP01", "LEFT"));
            Assert.Equal("EQP01:LEFT", ExchangeSteps.BuildMidLocationId(" EQP01 ", " LEFT "));
        }

        [Theory]
        [InlineData(null, "LEFT")]
        [InlineData("", "LEFT")]
        [InlineData("EQP01", null)]
        [InlineData("EQP01", "")]
        [InlineData("  ", "LEFT")]
        public void BuildMidLocationId_MissingPart_ReturnsEmpty(string midLoc, string midPortId)
        {
            Assert.Equal("", ExchangeSteps.BuildMidLocationId(midLoc, midPortId));
        }

        // ── ResolveArrivedStep: STEP × 현재 노드 → ARRIVED step ──

        private const string SRC = "N1011";
        private const string MID = "N1021";
        private const string DST = "N1031";

        [Theory]
        [InlineData(10, SRC, 10)]   // origin 이동 중 origin 도착
        [InlineData(20, MID, 20)]   // 설비 이동 중 설비 도착
        [InlineData(50, DST, 50)]   // 반납 이동 중 dest 도착
        public void ResolveArrivedStep_MatchingWaypoint_ReturnsStep(int step, string node, int expected)
        {
            Assert.Equal(expected, ExchangeSteps.ResolveArrivedStep(step, node, SRC, MID, DST));
        }

        [Theory]
        [InlineData(10, MID)]   // origin 이동 중 다른 노드
        [InlineData(20, SRC)]
        [InlineData(50, MID)]
        [InlineData(30, SRC)]   // 작업 단계(30/40)는 도착 보고 없음
        [InlineData(40, MID)]
        [InlineData(60, DST)]
        [InlineData(0, SRC)]    // STEP 파싱 실패(0)
        public void ResolveArrivedStep_NonMatching_ReturnsNull(int step, string node)
        {
            Assert.Null(ExchangeSteps.ResolveArrivedStep(step, node, SRC, MID, DST));
        }

        [Fact]
        public void ResolveArrivedStep_CaseInsensitive()
        {
            Assert.Equal(20, ExchangeSteps.ResolveArrivedStep(20, "n1021", SRC, MID, DST));
        }

        [Fact]
        public void ResolveArrivedStep_NullInputs_ReturnsNull()
        {
            Assert.Null(ExchangeSteps.ResolveArrivedStep(10, null, SRC, MID, DST));
            Assert.Null(ExchangeSteps.ResolveArrivedStep(10, SRC, null, MID, DST));
            Assert.Null(ExchangeSteps.ResolveArrivedStep(20, MID, SRC, "", DST));
        }

        // ── CarrierSlotFor: 단계별 보고 슬롯 ──

        [Theory]
        [InlineData(10, "1")]   // 신자재 픽업 → loadSlot
        [InlineData(40, "1")]   // 신자재 투입 → loadSlot
        [InlineData(30, "3")]   // 구자재 회수 → unloadSlot
        [InlineData(50, "3")]   // 구자재 반납 → unloadSlot
        [InlineData(20, "")]    // 이동 단계 → 없음
        [InlineData(60, "")]    // 완료 단계 → 없음
        public void CarrierSlotFor_MapsSlotByStep(int step, string expected)
        {
            Assert.Equal(expected, ExchangeSteps.CarrierSlotFor(step, "1", "3"));
        }

        [Fact]
        public void CarrierSlotFor_NullSlots_ReturnsEmpty()
        {
            Assert.Equal("", ExchangeSteps.CarrierSlotFor(10, null, "3"));
            Assert.Equal("", ExchangeSteps.CarrierSlotFor(30, "1", null));
        }
    }
}
