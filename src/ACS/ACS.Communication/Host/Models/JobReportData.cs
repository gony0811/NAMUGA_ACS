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
    }
}
