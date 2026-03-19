using System.Xml.Serialization;

namespace ACS.Communication.Host.Models
{
    /// <summary>
    /// MOVECMD DataLayer 필드 (Host → ACS).
    /// </summary>
    public class MoveCommandData
    {
        [XmlElement("AcsId")]
        public string AcsId { get; set; } = "";

        [XmlElement("DestLoc")]
        public string DestLoc { get; set; } = "";

        [XmlElement("DestPort")]
        public string DestPort { get; set; } = "";

        [XmlElement("ActionType")]
        public string ActionType { get; set; } = "";

        [XmlElement("SourceLoc")]
        public string SourceLoc { get; set; } = "";

        [XmlElement("SourcePort")]
        public string SourcePort { get; set; } = "";

        [XmlElement("JobID")]
        public string JobID { get; set; } = "";

        [XmlElement("MaterialType")]
        public string MaterialType { get; set; } = "";

        [XmlElement("UserID")]
        public string UserID { get; set; } = "";
    }
}
