using ACS.Core.Transfer;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// AMR reply 해석 규약 (docs/ACS-AMR_mqtt_exchange.md v0.3 §5, §8.1) — AmrReplyPolicy 순수 로직 테스트.
    /// EI(HandleAmrReplyActivity)와 Trans(RailVehicleJobfailedWorkflow)가 공유하는 결정표의 회귀 가드.
    /// </summary>
    public class AmrReplyPolicyTests
    {
        // ── 상수 가드: 값이 바뀌면 AMR 인터페이스 계약이 깨진다 ──

        [Fact]
        public void StatusConstants_HaveContractValues()
        {
            Assert.Equal("ACCEPTED", AmrReplyPolicy.STATUS_ACCEPTED);
            Assert.Equal("EXECUTING", AmrReplyPolicy.STATUS_EXECUTING);
            Assert.Equal("ARRIVED", AmrReplyPolicy.STATUS_ARRIVED);
            Assert.Equal("STEP_COMPLETE", AmrReplyPolicy.STATUS_STEP_COMPLETE);
            Assert.Equal("COMPLETED", AmrReplyPolicy.STATUS_COMPLETED);
            Assert.Equal("REJECTED", AmrReplyPolicy.STATUS_REJECTED);
            Assert.Equal("FAILED", AmrReplyPolicy.STATUS_FAILED);
            Assert.Equal("CANCELED", AmrReplyPolicy.STATUS_CANCELED);
        }

        [Fact]
        public void ResultCodeConstants_HaveContractValues()
        {
            Assert.Equal(0, AmrReplyPolicy.RC_OK);
            Assert.Equal(2, AmrReplyPolicy.RC_UNSUPPORTED_COMMAND);
            Assert.Equal(10, AmrReplyPolicy.RC_MODBUS_DISCONNECTED);
            Assert.Equal(11, AmrReplyPolicy.RC_BUSY);
            Assert.Equal(20, AmrReplyPolicy.RC_NODE_UNMAPPED);
            Assert.Equal(21, AmrReplyPolicy.RC_SLOT_MISMATCH);
            Assert.Equal(22, AmrReplyPolicy.RC_COBOT_NOT_READY);
            Assert.Equal(30, AmrReplyPolicy.RC_MAGAZINE_NOT_FOUND);
            Assert.Equal(31, AmrReplyPolicy.RC_SLOT_STATE_MISMATCH);
            Assert.Equal(32, AmrReplyPolicy.RC_GATE_TIMEOUT);
            Assert.Equal(40, AmrReplyPolicy.RC_CANCEL_REJECTED);
            Assert.Equal(99, AmrReplyPolicy.RC_INTERNAL_ERROR);
        }

        // ── EI 라우팅 ──

        [Theory]
        [InlineData("COMPLETED", AmrReplyAction.RouteCompleted)]
        [InlineData("completed", AmrReplyAction.RouteCompleted)]
        [InlineData("STEP_COMPLETE", AmrReplyAction.RouteCompleted)]
        [InlineData("ARRIVED", AmrReplyAction.RouteArrived)]
        [InlineData("FAILED", AmrReplyAction.RouteFailed)]
        [InlineData("REJECTED", AmrReplyAction.RouteFailed)]
        [InlineData("CANCELED", AmrReplyAction.LogCanceled)]
        [InlineData("ACCEPTED", AmrReplyAction.Ignore)]
        [InlineData("EXECUTING", AmrReplyAction.Ignore)]
        [InlineData("UNKNOWN", AmrReplyAction.Ignore)]
        [InlineData("", AmrReplyAction.Ignore)]
        [InlineData(null, AmrReplyAction.Ignore)]
        public void Route_MapsStatusToAction(string status, AmrReplyAction expected)
        {
            Assert.Equal(expected, AmrReplyPolicy.Route(status));
        }

        [Theory]
        [InlineData("STEP_COMPLETE", true)]
        [InlineData("COMPLETED", false)]
        [InlineData("ARRIVED", false)]
        public void RequiresStep_OnlyForStepComplete(string status, bool expected)
        {
            Assert.Equal(expected, AmrReplyPolicy.RequiresStep(status));
        }

        // ── EXCHANGE STEP → 구간 jobType 역추정 (reply 에 jobType 이 없을 때) ──

        [Theory]
        [InlineData(10, "UNLOAD")]
        [InlineData(20, "EXCHANGE")]
        [InlineData(30, "EXCHANGE")]
        [InlineData(40, "EXCHANGE")]
        [InlineData(50, "LOAD")]
        [InlineData(60, null)]
        [InlineData(0, null)]
        public void ResolveExchangeJobType_ByStep(int step, string expected)
        {
            Assert.Equal(expected, AmrReplyPolicy.ResolveExchangeJobType(step));
        }

        // ── Trans FAILED/REJECTED 처리 결정 ──

        [Theory]
        [InlineData("FAILED", true, 10, AmrFailedDisposition.MagazineNotFound)]
        [InlineData("failed", true, 10, AmrFailedDisposition.MagazineNotFound)]
        [InlineData("REJECTED", true, 10, AmrFailedDisposition.RollbackToQueued)]
        [InlineData("FAILED", true, 20, AmrFailedDisposition.LogOnly)]
        [InlineData("REJECTED", true, 20, AmrFailedDisposition.LogOnly)]
        [InlineData("FAILED", true, 50, AmrFailedDisposition.LogOnly)]
        [InlineData("FAILED", false, 10, AmrFailedDisposition.LogOnly)]
        [InlineData("REJECTED", false, 10, AmrFailedDisposition.LogOnly)]
        [InlineData("COMPLETED", true, 10, AmrFailedDisposition.LogOnly)]
        public void DecideFailed_Matrix(string status, bool isExchange, int step, AmrFailedDisposition expected)
        {
            Assert.Equal(expected, AmrReplyPolicy.DecideFailed(status, isExchange, step));
        }

        [Fact]
        public void DecideFailed_IgnoresResultCode_OnlyStatusStepMatter()
        {
            // resultCode 30(MAGAZINE_NOT_FOUND) 이든 900(시뮬레이터 기본값) 이든 FAILED@10 은 동일 처리
            Assert.Equal(AmrFailedDisposition.MagazineNotFound, AmrReplyPolicy.DecideFailed("FAILED", true, 10));
        }

        [Theory]
        [InlineData(40, true)]
        [InlineData(0, false)]
        [InlineData(99, false)]
        public void IsCancelRejected_OnlyCode40(int resultCode, bool expected)
        {
            Assert.Equal(expected, AmrReplyPolicy.IsCancelRejected(resultCode));
        }
    }
}
