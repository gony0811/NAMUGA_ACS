using System.Xml;
using ACS.Core.Transfer;
using Xunit;

namespace ACS.Core.Tests
{
    /// <summary>
    /// EXCHANGE(v2) S3: EXCHANGECMD XML 파서 단위 테스트.
    /// 실제 MES 송신 XML(2026-08-12 통합 테스트 캡처) 기준.
    /// 참조: ACS_EXCHANGE_구현사양서.md §4.1 DoD
    /// </summary>
    public class ExchangeCmdModelTests
    {
        // MES 가 실제로 송신한 EXCHANGECMD (LoadCarrierSlot/UnloadCarrierSlot 공백 = 자동배정)
        private const string RealXml =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Msg><Command>EXCHANGECMD</Command>" +
            "<Header><DestSubject>/HQ/ACS01</DestSubject><ReplySubject>/HQ/MES01</ReplySubject></Header>" +
            "<DataLayer><AcsId>ACS01</AcsId><JobID>MES_ACS_TEST_EXCHANGE_20260812121028028_0001</JobID>" +
            "<EquipID>192.168.32.36</EquipID><Port>LEFT</Port><Model>TEST_MODEL</Model>" +
            "<LoadEquipJobID>MES_ACS_TEST_EXCHANGE_20260812121028028_0001</LoadEquipJobID>" +
            "<UnloadEquipJobID>MES_ACS_TEST_EXCHANGE_20260812121028028_0001</UnloadEquipJobID>" +
            "<LoadSourceLoc>TEST_LOAD_RACK_01</LoadSourceLoc><UnloadDestLoc>TEST_UNLOAD_RACK_01</UnloadDestLoc>" +
            "<LoadCarrierSlot></LoadCarrierSlot><UnloadCarrierSlot></UnloadCarrierSlot>" +
            "<MaterialType>MAGAZINE</MaterialType><ActionType>EXCHANGE</ActionType><UserID>MES01</UserID></DataLayer></Msg>";

        private static XmlDocument Load(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        [Fact]
        public void Parse_RealMesXml_All14Fields()
        {
            var m = ExchangeCmdModel.Parse(Load(RealXml));

            Assert.Equal("ACS01", m.AcsId);
            Assert.Equal("MES_ACS_TEST_EXCHANGE_20260812121028028_0001", m.JobId);
            Assert.Equal("192.168.32.36", m.EquipId);
            Assert.Equal("LEFT", m.Port);
            Assert.Equal("TEST_MODEL", m.Model);
            Assert.Equal("MES_ACS_TEST_EXCHANGE_20260812121028028_0001", m.LoadEquipJobId);
            Assert.Equal("MES_ACS_TEST_EXCHANGE_20260812121028028_0001", m.UnloadEquipJobId);
            Assert.Equal("TEST_LOAD_RACK_01", m.LoadSourceLoc);
            Assert.Equal("TEST_UNLOAD_RACK_01", m.UnloadDestLoc);
            Assert.Equal("", m.LoadCarrierSlot);      // 공백 = ACS 자동배정 (D10)
            Assert.Equal("", m.UnloadCarrierSlot);
            Assert.Equal("MAGAZINE", m.MaterialType);
            Assert.Equal("EXCHANGE", m.ActionType);
            Assert.Equal("MES01", m.UserId);
        }

        [Fact]
        public void Parse_ReplySubject_FromHeader()
        {
            var m = ExchangeCmdModel.Parse(Load(RealXml));
            Assert.Equal("/HQ/MES01", m.ReplySubject);
        }

        [Fact]
        public void Parse_MissingFields_EmptyStringNoThrow()
        {
            var m = ExchangeCmdModel.Parse(Load("<Msg><Command>EXCHANGECMD</Command><Header/><DataLayer/></Msg>"));
            Assert.Equal("", m.JobId);
            Assert.Equal("", m.EquipId);
            Assert.Equal("", m.ActionType);
            Assert.Equal("", m.ReplySubject);
        }

        [Fact]
        public void Parse_NullDocument_EmptyModel()
        {
            var m = ExchangeCmdModel.Parse(null);
            Assert.Equal("", m.JobId);
        }

        [Fact]
        public void Parse_FieldsWithoutDataLayerWrapper_FallbackWorks()
        {
            // //DataLayer/필드 → //필드 이중 fallback 관례
            var m = ExchangeCmdModel.Parse(Load("<Msg><JobID>J1</JobID><ActionType>EXCHANGE</ActionType></Msg>"));
            Assert.Equal("J1", m.JobId);
            Assert.Equal("EXCHANGE", m.ActionType);
        }

        [Fact]
        public void Parse_WhitespaceTrimmed()
        {
            var m = ExchangeCmdModel.Parse(Load("<Msg><DataLayer><JobID>  J2  </JobID></DataLayer></Msg>"));
            Assert.Equal("J2", m.JobId);
        }
    }
}
