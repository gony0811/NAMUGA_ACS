using ACS.Core.Transfer.Model;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// EXCHANGE(v2) 신규 상수 가드 테스트.
    /// 확정 결정 D5: EXCHANGE TC 는 기존 스케줄러의 State="QUEUED" 조회에 절대 걸리면 안 된다.
    /// 참조: ACS_EXCHANGE_구현사양서.md §0(D5), §2.4
    /// </summary>
    public class ExchangeConstantsTests
    {
        [Fact]
        public void StateExchangeQueued_HasContractValue()
        {
            Assert.Equal("EXCHANGE_QUEUED", TransportCommandEx.STATE_EXCHANGE_QUEUED);
        }

        [Fact]
        public void StateExchangeQueued_FitsVarchar20()
        {
            // NA_T_TRANSPORTCMD.state 는 varchar(20).
            Assert.True(TransportCommandEx.STATE_EXCHANGE_QUEUED.Length <= 20,
                $"state value too long: {TransportCommandEx.STATE_EXCHANGE_QUEUED.Length}");
        }

        [Fact]
        public void StateExchangeQueued_IsNotLegacyQueued()
        {
            // D5 핵심 가드: 기존 스케줄러 배제의 전제. 값이 "QUEUED" 와 같아지는 순간
            // 기존 SCHEDULE-QUEUEJOB 이 EXCHANGE TC 를 낚아채는 회귀가 발생한다.
            Assert.NotEqual(TransportCommandEx.STATE_QUEUED, TransportCommandEx.STATE_EXCHANGE_QUEUED);
        }

        [Fact]
        public void JobTypeExchange_HasContractValue()
        {
            // AMR MQTT 규약(moveCmd.jobType)과 RAIL-CARRIERTRANSFER.jobType 이 이 값을 그대로 사용한다.
            Assert.Equal("EXCHANGE", TransportCommandEx.JOBTYPE_EXCHANGE);
        }

        [Fact]
        public void JobTypeExchange_DistinctFromLoadUnload()
        {
            Assert.NotEqual(TransportCommandEx.JOBTYPE_LOAD, TransportCommandEx.JOBTYPE_EXCHANGE);
            Assert.NotEqual(TransportCommandEx.JOBTYPE_UNLOAD, TransportCommandEx.JOBTYPE_EXCHANGE);
        }
    }
}
