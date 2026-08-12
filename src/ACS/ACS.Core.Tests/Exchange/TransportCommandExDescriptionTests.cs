using ACS.Core.Transfer.Model;
using Xunit;

namespace ACS.Core.Tests.Exchange
{
    /// <summary>
    /// TransportCommandEx.Description 포맷("MODEL='&lt;m&gt;';&lt;MaterialType&gt;") 파싱 헬퍼 테스트.
    /// EXCHANGE S4 의 JOBREPORT(START) MaterialType 채움에 GetMaterialType 을 사용한다.
    /// </summary>
    public class TransportCommandExDescriptionTests
    {
        [Fact]
        public void GetMaterialType_ExchangeDescriptionFormat_ReturnsMaterialType()
        {
            var tc = new TransportCommandEx { Description = "MODEL='TEST_MODEL';MAGAZINE" };
            Assert.Equal("MAGAZINE", tc.GetMaterialType());
        }

        [Fact]
        public void GetMaterialType_KeepsModelIntact()
        {
            var tc = new TransportCommandEx { Description = "MODEL='TEST_MODEL';MAGAZINE" };
            Assert.Equal("TEST_MODEL", tc.GetModel());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("MODEL='X'")]      // 구분자 없음
        [InlineData("MODEL='X';")]     // 구분자 뒤 빈 값
        public void GetMaterialType_NonConformingFormat_ReturnsNull(string description)
        {
            var tc = new TransportCommandEx { Description = description };
            Assert.Null(tc.GetMaterialType());
        }
    }
}
