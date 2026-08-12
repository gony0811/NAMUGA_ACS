using System;
using System.Xml;

namespace ACS.Core.Transfer
{
    /// <summary>
    /// EXCHANGE(v2): MES EXCHANGECMD DataLayer 강타입 모델 + 파서.
    /// 파싱은 순수 로직(XmlDocument 만 의존)으로 두어 단위 테스트 대상이 된다.
    /// 참조: ACS_EXCHANGE_구현사양서.md §4.1, MCS-ACS_통신포맷_사양서
    /// </summary>
    public class ExchangeCmdModel
    {
        public string AcsId { get; set; } = "";
        public string JobId { get; set; } = "";
        public string EquipId { get; set; } = "";
        public string Port { get; set; } = "";
        public string Model { get; set; } = "";
        public string LoadEquipJobId { get; set; } = "";     // Optional — additionalInfo 저장만 (EQJOB_L)
        public string UnloadEquipJobId { get; set; } = "";   // Optional — additionalInfo 저장만 (EQJOB_U)
        public string LoadSourceLoc { get; set; } = "";
        public string UnloadDestLoc { get; set; } = "";
        public string LoadCarrierSlot { get; set; } = "";    // 공백 허용 → ACS 자동배정 (D10)
        public string UnloadCarrierSlot { get; set; } = "";  // 공백 허용 → ACS 자동배정 (D10)
        public string MaterialType { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string UserId { get; set; } = "";
        public string ReplySubject { get; set; } = "";       // Header — JOBREPORT 응답 라우팅용

        /// <summary>
        /// EXCHANGECMD XML → 모델. 필드 누락 시 빈 문자열(예외 없음).
        /// 기존 CreateTransportCommandActivity 의 //DataLayer/필드 → //필드 이중 fallback 관례.
        /// </summary>
        public static ExchangeCmdModel Parse(XmlDocument doc)
        {
            var m = new ExchangeCmdModel();
            if (doc == null) return m;
            m.AcsId            = Extract(doc, "AcsId");
            m.JobId            = Extract(doc, "JobID");
            m.EquipId          = Extract(doc, "EquipID");
            m.Port             = Extract(doc, "Port");
            m.Model            = Extract(doc, "Model");
            m.LoadEquipJobId   = Extract(doc, "LoadEquipJobID");
            m.UnloadEquipJobId = Extract(doc, "UnloadEquipJobID");
            m.LoadSourceLoc    = Extract(doc, "LoadSourceLoc");
            m.UnloadDestLoc    = Extract(doc, "UnloadDestLoc");
            m.LoadCarrierSlot  = Extract(doc, "LoadCarrierSlot");
            m.UnloadCarrierSlot = Extract(doc, "UnloadCarrierSlot");
            m.MaterialType     = Extract(doc, "MaterialType");
            m.ActionType       = Extract(doc, "ActionType");
            m.UserId           = Extract(doc, "UserID");
            m.ReplySubject     = ExtractAt(doc, "//Header/ReplySubject");
            return m;
        }

        private static string Extract(XmlDocument doc, string field)
        {
            var v = ExtractAt(doc, "//DataLayer/" + field);
            if (v.Length == 0) v = ExtractAt(doc, "//" + field);
            return v;
        }

        private static string ExtractAt(XmlDocument doc, string xpath)
        {
            try
            {
                var node = doc.SelectSingleNode(xpath);
                var text = node?.InnerText;
                return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
