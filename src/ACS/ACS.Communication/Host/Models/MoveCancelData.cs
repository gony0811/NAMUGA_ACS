using System.Xml.Serialization;

namespace ACS.Communication.Host.Models
{
    /// <summary>
    /// MOVECANCEL DataLayer 필드 (Host → ACS).
    /// </summary>
    public class MoveCancelData
    {
        [XmlElement("AcsId")]
        public string AcsId { get; set; } = "";

        [XmlElement("DestLoc")]
        public string DestLoc { get; set; } = "";

        [XmlElement("SourceLoc")]
        public string SourceLoc { get; set; } = "";

        [XmlElement("JobId")]
        public string JobId { get; set; } = "";

        [XmlElement("MaterialType")]
        public string MaterialType { get; set; } = "";

        [XmlElement("UserId")]
        public string UserId { get; set; } = "";
    }
}
