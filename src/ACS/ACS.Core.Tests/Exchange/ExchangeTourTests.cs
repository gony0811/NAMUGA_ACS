using System.Collections.Generic;
using ACS.Core.Transfer;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// 배칭 투어 전진 순수 로직 (구현사양서 §4.10 / D9: 픽업들 → 설비들 → 반납들).
    /// </summary>
    public class ExchangeTourTests
    {
        private const string A = "EX_A";
        private const string B = "EX_B";

        private static ExchangeTourAction Next(params (string JobId, int Step)[] tcs)
        {
            return ExchangeTour.NextAfter(new List<(string, int)>(tcs));
        }

        // ── 단독 교환 (기존 흐름과 동등해야 함) ──

        [Fact]
        public void Single_Step10_PicksUp()
        {
            var a = Next((A, 10));
            Assert.Equal(ExchangeTourActionKind.PickupMove, a.Kind);
            Assert.Equal(A, a.JobId);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(30)]
        [InlineData(40)]
        public void Single_EquipPhase_MidPhase(int step)
        {
            var a = Next((A, step));
            Assert.Equal(ExchangeTourActionKind.MidPhase, a.Kind);
            Assert.Equal(A, a.JobId);
            Assert.Equal(step, a.Step);
        }

        [Fact]
        public void Single_Step50_DestMove()
        {
            var a = Next((A, 50));
            Assert.Equal(ExchangeTourActionKind.DestMove, a.Kind);
        }

        [Fact]
        public void Empty_TripComplete()
        {
            var a = Next();
            Assert.Equal(ExchangeTourActionKind.TripComplete, a.Kind);
            Assert.Null(a.JobId);
        }

        // ── 2건 배칭 인터리브 전 시퀀스 (D9 순서) ──
        //  이벤트 경계마다 활성 TC 의 STEP 스냅샷 → 기대 행동

        [Fact]
        public void Batch_FullSequence()
        {
            // 배차 직후: A=10, B=10 → A 픽업행
            AssertAction(Next((A, 10), (B, 10)), ExchangeTourActionKind.PickupMove, A);
            // A 픽업 완료(A→20): B=10 남음 → B 픽업행 (설비보다 픽업 우선)
            AssertAction(Next((A, 20), (B, 10)), ExchangeTourActionKind.PickupMove, B);
            // B 픽업 완료(B→20): 픽업 없음 → A 설비행 (순서상 첫 TC)
            AssertAction(Next((A, 20), (B, 20)), ExchangeTourActionKind.MidPhase, A, 20);
            // A 게이트1 완료(A=30): 여전히 A 설비 구간 (게이트 대기 — 발행 없음)
            AssertAction(Next((A, 30), (B, 20)), ExchangeTourActionKind.MidPhase, A, 30);
            // A 설비 완료(A→50): B=20 → B 설비행
            AssertAction(Next((A, 50), (B, 20)), ExchangeTourActionKind.MidPhase, B, 20);
            // B 설비 완료(B→50): A 반납행 (순서상 첫 TC)
            AssertAction(Next((A, 50), (B, 50)), ExchangeTourActionKind.DestMove, A);
            // A 반납 완료(A 종결·목록 제외): B 반납행
            AssertAction(Next((B, 50)), ExchangeTourActionKind.DestMove, B);
            // B 반납 완료: 트립 종결
            AssertAction(Next(), ExchangeTourActionKind.TripComplete, null);
        }

        // ── 부분 취소 후 잔여 (C1/C2 로 A 가 빠진 경우) ──

        [Fact]
        public void Batch_AfterPartialCancel_RemainderContinues()
        {
            // A 취소(픽업 전) 후 B=10 → B 픽업행
            AssertAction(Next((B, 10)), ExchangeTourActionKind.PickupMove, B);
            // B 진행 중 상태들도 단독과 동일
            AssertAction(Next((B, 20)), ExchangeTourActionKind.MidPhase, B, 20);
            AssertAction(Next((B, 50)), ExchangeTourActionKind.DestMove, B);
        }

        // ── 트립 ID 헬퍼 ──

        [Fact]
        public void TripId_PrefixAndDetection()
        {
            var id = ExchangeTour.NewTripId(new System.DateTime(2026, 8, 20, 10, 30, 0, 123));
            Assert.StartsWith("TRIP", id);
            Assert.True(ExchangeTour.IsTripId(id));
            Assert.False(ExchangeTour.IsTripId("EX20260820103000123"));
            Assert.False(ExchangeTour.IsTripId(""));
            Assert.False(ExchangeTour.IsTripId(null));
        }

        private static void AssertAction(ExchangeTourAction a, ExchangeTourActionKind kind, string jobId, int? step = null)
        {
            Assert.Equal(kind, a.Kind);
            Assert.Equal(jobId, a.JobId);
            if (step.HasValue) Assert.Equal(step.Value, a.Step);
        }
    }
}
