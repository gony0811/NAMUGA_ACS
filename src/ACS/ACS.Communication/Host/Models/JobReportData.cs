using System.Xml.Serialization;

namespace ACS.Communication.Host.Models
{
    /// <summary>
    /// JOBREPORT DataLayer 필드 (ACS → Host).
    /// </summary>
    public class JobReportData
    {
        [XmlElement("AcsId")]
        public string AcsId { get; set; } = "";

        [XmlElement("Type")]
        public string Type { get; set; } = "";

        [XmlElement("AmrId")]
        public string AmrId { get; set; } = "";

        [XmlElement("ActionType")]
        public string ActionType { get; set; } = "";

        [XmlElement("JobID")]
        public string JobID { get; set; } = "";

        [XmlElement("MaterialType")]
        public string MaterialType { get; set; } = "";

        [XmlElement("UserID")]
        public string UserID { get; set; } = "";

        [XmlElement("ErrorCode")]
        public string ErrorCode { get; set; } = "";

        [XmlElement("ErrorMsg")]
        public string ErrorMsg { get; set; } = "";

        // MES XML 에는 매핑되지 않는 라우팅 헤더 (header.routedFrom). Trans→Host 라우팅 누수 시
        // ForwardJobReportToMesActivity 가 hostAgent 로 재발행할 때 루프 검출에 사용.
        [XmlIgnore]
        public string RoutedFrom { get; set; } = "";
    }
}
