using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// JOBCANCEL C1~C4 판정 (시나리오 사양서 "취소·오류" 시트).
    /// </summary>
    public class JobCancelJudgeTests
    {
        private const string EX = "EXCHANGE";
        private const string LOAD = "LOAD";

        // ── C1: 배차 전 즉시 취소 ──

        [Theory]
        [InlineData("QUEUED")]
        [InlineData("EXCHANGE_QUEUED")]
        [InlineData("CREATED")]
        [InlineData("WAITING")]
        [InlineData("PREASSIGNED")]
        public void BeforeAssign_C1(string state)
        {
            Assert.Equal(JobCancelVerdict.CancelBeforeAssign,
                JobCancelJudge.Judge(state, EX, 0, anySlotOccupied: false));
        }

        // ── C2: 픽업 전 (이동 중) ──

        [Theory]
        [InlineData("ASSIGNED")]
        [InlineData("ARRIVED_SOURCE")]
        [InlineData("TRANSFERRING_SOURCE")]
        public void Generic_BeforePickup_C2(string state)
        {
            Assert.Equal(JobCancelVerdict.CancelBeforePickup,
                JobCancelJudge.Judge(state, LOAD, 0, anySlotOccupied: false));
        }

        [Fact]
        public void Exchange_Step10_NoLoad_C2()
        {
            Assert.Equal(JobCancelVerdict.CancelBeforePickup,
                JobCancelJudge.Judge("EXCHANGE_ASSIGNED", EX, ExchangeSteps.STEP_PICKUP_NEW, anySlotOccupied: false));
        }

        // ── C3: 적재 후 전 구간 ──

        [Theory]
        [InlineData("TRANSFERRING_DEST")]
        [InlineData("ARRIVED_DEST")]
        public void Generic_AfterLoad_C3(string state)
        {
            Assert.Equal(JobCancelVerdict.CancelAfterLoad,
                JobCancelJudge.Judge(state, LOAD, 0, anySlotOccupied: false));
        }

        [Theory]
        [InlineData(20, false)]  // 설비 이동 중 (신자재 적재됨 — STEP 기준)
        [InlineData(30, true)]
        [InlineData(40, true)]
        [InlineData(50, true)]   // 반납 이동 중 (구자재 적재)
        [InlineData(10, true)]   // STEP=10 이라도 실물 적재면 C3 (픽업 완료 직후 전이 전 틈)
        public void Exchange_AfterLoad_C3(int step, bool slotOccupied)
        {
            Assert.Equal(JobCancelVerdict.CancelAfterLoad,
                JobCancelJudge.Judge("EXCHANGE_ASSIGNED", EX, step, slotOccupied));
        }

        // ── C5: 배칭 중 1건 적재 후 취소 — 페어 연대 종결 ──

        [Theory]
        [InlineData(20, false)]
        [InlineData(30, true)]
        [InlineData(50, true)]
        public void Exchange_AfterLoad_WithTripMate_C5(int step, bool slotOccupied)
        {
            Assert.Equal(JobCancelVerdict.CancelAfterLoadBatch,
                JobCancelJudge.Judge("EXCHANGE_ASSIGNED", EX, step, slotOccupied, hasActiveTripMate: true));
        }

        [Fact]
        public void Exchange_BeforePickup_WithTripMate_StaysC2()
        {
            // 픽업 전 취소는 배칭이어도 C2 — 페어는 계속 진행
            Assert.Equal(JobCancelVerdict.CancelBeforePickup,
                JobCancelJudge.Judge("EXCHANGE_ASSIGNED", EX, ExchangeSteps.STEP_PICKUP_NEW, false, hasActiveTripMate: true));
        }

        [Fact]
        public void Generic_AfterLoad_TripMateFlag_Ignored()
        {
            // 일반 반송에는 트립 개념 없음 — C3 유지
            Assert.Equal(JobCancelVerdict.CancelAfterLoad,
                JobCancelJudge.Judge("TRANSFERRING_DEST", LOAD, 0, false, hasActiveTripMate: true));
        }

        // ── C4: 종료/취소 진행 상태 거부 ──

        [Theory]
        [InlineData("COMPLETED")]
        [InlineData("CANCELED")]
        [InlineData("CANCELING")]
        [InlineData("ABORTED")]
        [InlineData("ABORTING")]
        [InlineData("COMPLETEFAILED")]
        [InlineData("CHARGECOMPLETED")]
        public void Terminal_Reject_C4(string state)
        {
            Assert.Equal(JobCancelVerdict.Reject,
                JobCancelJudge.Judge(state, EX, 60, anySlotOccupied: false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("SOMETHING_UNKNOWN")]
        public void MissingOrUnknownState_Reject(string state)
        {
            Assert.Equal(JobCancelVerdict.Reject,
                JobCancelJudge.Judge(state, EX, 0, anySlotOccupied: false));
        }

        [Fact]
        public void StateComparison_IsCaseInsensitive()
        {
            Assert.Equal(JobCancelVerdict.CancelBeforeAssign,
                JobCancelJudge.Judge("exchange_queued", EX, 10, false));
        }

        [Fact]
        public void ErrorCodes_MatchSpec()
        {
            Assert.Equal("0", JobCancelJudge.ERR_OK);
            Assert.Equal("CANCEL_REJECTED", JobCancelJudge.ERR_CANCEL_REJECTED);
            Assert.Equal("MAGAZINE_NOT_FOUND", JobCancelJudge.ERR_MAGAZINE_NOT_FOUND);
            Assert.Equal("EXCHANGE_CANCELED", JobCancelJudge.ERR_EXCHANGE_CANCELED);
        }
    }
}
