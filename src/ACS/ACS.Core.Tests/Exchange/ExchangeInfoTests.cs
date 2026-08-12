using System;
using System.Collections.Generic;
using ACS.Core.Transfer;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// ExchangeInfo (AdditionalInfo 키-값 규약 파서/빌더) 단위 테스트.
    /// 참조: ACS_EXCHANGE_구현사양서.md §2.2, §4.2, §4.17 (S1 DoD)
    /// </summary>
    public class ExchangeInfoTests
    {
        // ---------- Parse ----------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_NullOrWhitespace_ReturnsEmpty(string input)
        {
            Assert.Empty(ExchangeInfo.Parse(input));
        }

        [Fact]
        public void Parse_TypicalInsertString_PreservesOrderAndValues()
        {
            string info = "STEP=10;TRIP=;LOADSLOT=;UNLOADSLOT=;EQJOB_L=PRD-X_LOAD_001;EQJOB_U=PRD-X_UNLOAD_001";
            var entries = ExchangeInfo.Parse(info);

            Assert.Equal(6, entries.Count);
            Assert.Equal("STEP", entries[0].Key);
            Assert.Equal("10", entries[0].Value);
            Assert.Equal("TRIP", entries[1].Key);
            Assert.Equal("", entries[1].Value);
            Assert.Equal("EQJOB_U", entries[5].Key);
            Assert.Equal("PRD-X_UNLOAD_001", entries[5].Value);
        }

        [Fact]
        public void Parse_ValueContainingEquals_SplitsOnFirstEqualsOnly()
        {
            // 설비 JobID 등에 '=' 가 섞여도 값이 잘리면 안 된다.
            var entries = ExchangeInfo.Parse("EQJOB_L=A=B=C");
            Assert.Single(entries);
            Assert.Equal("EQJOB_L", entries[0].Key);
            Assert.Equal("A=B=C", entries[0].Value);
        }

        [Fact]
        public void Parse_EmptySegmentsAndSpaces_AreIgnored()
        {
            var entries = ExchangeInfo.Parse(";; STEP = 20 ;;TRIP=T1;");
            Assert.Equal(2, entries.Count);
            Assert.Equal("STEP", entries[0].Key);
            Assert.Equal("20", entries[0].Value);
            Assert.Equal("T1", entries[1].Value);
        }

        // ---------- Build / Roundtrip ----------

        [Fact]
        public void BuildParse_Roundtrip_IsLossless()
        {
            var original = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("STEP", "30"),
                new KeyValuePair<string, string>("TRIP", "TRIP20260706103010"),
                new KeyValuePair<string, string>("LOADSLOT", "1"),
                new KeyValuePair<string, string>("UNLOADSLOT", "3")
            };

            var reparsed = ExchangeInfo.Parse(ExchangeInfo.Build(original));

            Assert.Equal(original.Count, reparsed.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i].Key, reparsed[i].Key);
                Assert.Equal(original[i].Value, reparsed[i].Value);
            }
        }

        [Fact]
        public void Build_Null_ReturnsEmptyString()
        {
            Assert.Equal("", ExchangeInfo.Build(null));
        }

        [Fact]
        public void Build_ValueWithSemicolon_Throws()
        {
            var entries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("STEP", "10;20")
            };
            Assert.Throws<ArgumentException>(() => ExchangeInfo.Build(entries));
        }

        // ---------- Get / Has ----------

        [Fact]
        public void Get_MissingKey_ReturnsEmpty()
        {
            Assert.Equal("", ExchangeInfo.Get("STEP=10", "TRIP"));
            Assert.Equal("", ExchangeInfo.Get(null, "STEP"));
            Assert.Equal("", ExchangeInfo.Get("STEP=10", null));
        }

        [Fact]
        public void Get_ExistingKey_IsCaseInsensitive()
        {
            string info = "STEP=40;TRIP=T1";
            Assert.Equal("40", ExchangeInfo.Get(info, "STEP"));
            Assert.Equal("40", ExchangeInfo.Get(info, "step"));
            Assert.Equal("T1", ExchangeInfo.Get(info, "Trip"));
        }

        [Fact]
        public void Has_ReflectsPresence()
        {
            string info = "STEP=10;TRIP=";
            Assert.True(ExchangeInfo.Has(info, "STEP"));
            Assert.True(ExchangeInfo.Has(info, "TRIP")); // 빈 값이어도 키는 존재
            Assert.False(ExchangeInfo.Has(info, "LOADSLOT"));
            Assert.False(ExchangeInfo.Has(null, "STEP"));
        }

        // ---------- Set ----------

        [Fact]
        public void Set_OnNullOrEmpty_CreatesEntry()
        {
            Assert.Equal("STEP=10", ExchangeInfo.Set(null, "STEP", "10"));
            Assert.Equal("STEP=10", ExchangeInfo.Set("", "STEP", "10"));
        }

        [Fact]
        public void Set_ExistingKey_UpdatesInPlace_KeepsOrder()
        {
            string info = "STEP=10;TRIP=T1;LOADSLOT=1";
            string updated = ExchangeInfo.Set(info, "STEP", "20");

            var entries = ExchangeInfo.Parse(updated);
            Assert.Equal(3, entries.Count);
            Assert.Equal("STEP", entries[0].Key);   // 위치 유지
            Assert.Equal("20", entries[0].Value);   // 값 갱신
            Assert.Equal("T1", entries[1].Value);   // 나머지 무변화
            Assert.Equal("1", entries[2].Value);
        }

        [Fact]
        public void Set_NewKey_AppendsAtEnd()
        {
            string updated = ExchangeInfo.Set("STEP=10", "TRIP", "T9");
            var entries = ExchangeInfo.Parse(updated);
            Assert.Equal(2, entries.Count);
            Assert.Equal("TRIP", entries[1].Key);
            Assert.Equal("T9", entries[1].Value);
        }

        [Fact]
        public void Set_NullValue_StoredAsEmpty()
        {
            string updated = ExchangeInfo.Set("STEP=10", "TRIP", null);
            Assert.True(ExchangeInfo.Has(updated, "TRIP"));
            Assert.Equal("", ExchangeInfo.Get(updated, "TRIP"));
        }

        [Fact]
        public void Set_DoesNotMutateInput()
        {
            string original = "STEP=10";
            ExchangeInfo.Set(original, "STEP", "20");
            Assert.Equal("STEP=10", original);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("BAD;KEY")]
        [InlineData("BAD=KEY")]
        public void Set_InvalidKey_Throws(string badKey)
        {
            Assert.Throws<ArgumentException>(() => ExchangeInfo.Set("", badKey, "v"));
        }

        [Fact]
        public void Set_ValueWithSemicolon_Throws()
        {
            Assert.Throws<ArgumentException>(() => ExchangeInfo.Set("", "STEP", "10;20"));
        }

        // ---------- BuildInitial (§2.1 insert 스냅샷) ----------

        [Fact]
        public void BuildInitial_MatchesInsertSnapshotContract()
        {
            string info = ExchangeInfo.BuildInitial("PRD-X_LOAD_001", "PRD-X_UNLOAD_001");

            Assert.Equal("10", ExchangeInfo.Get(info, ExchangeInfo.KEY_STEP));
            Assert.Equal("", ExchangeInfo.Get(info, ExchangeInfo.KEY_TRIP));
            Assert.Equal("", ExchangeInfo.Get(info, ExchangeInfo.KEY_LOADSLOT));
            Assert.Equal("", ExchangeInfo.Get(info, ExchangeInfo.KEY_UNLOADSLOT));
            Assert.Equal("PRD-X_LOAD_001", ExchangeInfo.Get(info, ExchangeInfo.KEY_EQJOB_L));
            Assert.Equal("PRD-X_UNLOAD_001", ExchangeInfo.Get(info, ExchangeInfo.KEY_EQJOB_U));
        }

        [Fact]
        public void BuildInitial_NullEquipJobIds_StoredAsEmpty()
        {
            string info = ExchangeInfo.BuildInitial(null, null);
            Assert.Equal("", ExchangeInfo.Get(info, ExchangeInfo.KEY_EQJOB_L));
            Assert.Equal("", ExchangeInfo.Get(info, ExchangeInfo.KEY_EQJOB_U));
        }

        // ---------- S4 배차: 슬롯 기록/롤백 갱신 ----------

        [Fact]
        public void Set_DispatchSlots_OnInitialSnapshot_PreservesOtherKeysAndOrder()
        {
            // S4 배차 시 AssignExchangeVehicleActivity 가 수행하는 갱신:
            // BuildInitial 스냅샷에 LOADSLOT/UNLOADSLOT 만 채워지고 STEP/TRIP/EQJOB_* 은 보존돼야 한다.
            string info = ExchangeInfo.BuildInitial("PRD-X_LOAD_001", "PRD-X_UNLOAD_001");
            string updated = ExchangeInfo.Set(
                ExchangeInfo.Set(info, ExchangeInfo.KEY_LOADSLOT, "1"),
                ExchangeInfo.KEY_UNLOADSLOT, "3");

            Assert.Equal("10", ExchangeInfo.Get(updated, ExchangeInfo.KEY_STEP));
            Assert.Equal("", ExchangeInfo.Get(updated, ExchangeInfo.KEY_TRIP));
            Assert.Equal("1", ExchangeInfo.Get(updated, ExchangeInfo.KEY_LOADSLOT));
            Assert.Equal("3", ExchangeInfo.Get(updated, ExchangeInfo.KEY_UNLOADSLOT));
            Assert.Equal("PRD-X_LOAD_001", ExchangeInfo.Get(updated, ExchangeInfo.KEY_EQJOB_L));
            Assert.Equal("PRD-X_UNLOAD_001", ExchangeInfo.Get(updated, ExchangeInfo.KEY_EQJOB_U));

            // 위치도 스냅샷 순서 그대로 (STEP, TRIP, LOADSLOT, UNLOADSLOT, EQJOB_L, EQJOB_U)
            var entries = ExchangeInfo.Parse(updated);
            Assert.Equal(6, entries.Count);
            Assert.Equal(ExchangeInfo.KEY_LOADSLOT, entries[2].Key);
            Assert.Equal(ExchangeInfo.KEY_UNLOADSLOT, entries[3].Key);
        }

        [Fact]
        public void Set_RollbackSlots_ClearsValuesRoundtrip()
        {
            // S4 롤백: LOADSLOT/UNLOADSLOT 을 빈 값으로 되돌리면 초기 스냅샷과 동등해야 한다.
            string initial = ExchangeInfo.BuildInitial("L1", "U1");
            string dispatched = ExchangeInfo.Set(
                ExchangeInfo.Set(initial, ExchangeInfo.KEY_LOADSLOT, "2"),
                ExchangeInfo.KEY_UNLOADSLOT, "4");
            string rolledBack = ExchangeInfo.Set(
                ExchangeInfo.Set(dispatched, ExchangeInfo.KEY_LOADSLOT, ""),
                ExchangeInfo.KEY_UNLOADSLOT, "");

            Assert.Equal(initial, rolledBack);
        }

        [Fact]
        public void BuildInitial_FitsInAdditionalInfoColumn()
        {
            // NA_T_TRANSPORTCMD.additionalInfo 는 varchar(1000).
            // 실측 최대 길이 수준의 설비 JobID 를 넣어도 여유가 커야 한다.
            string longJobId = new string('X', 100);
            string info = ExchangeInfo.BuildInitial(longJobId, longJobId);
            Assert.True(info.Length < 500, $"additionalInfo too long: {info.Length}");
        }
    }
}
