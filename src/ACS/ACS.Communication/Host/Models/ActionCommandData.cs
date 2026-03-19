using System.Xml.Serialization;

namespace ACS.Communication.Host.Models
{
    /// <summary>
    /// ACTIONCMD DataLayer 필드 (Host → ACS).
    /// </summary>
    public class ActionCommandData
    {
        [XmlElement("AcsId")]
        public string AcsId { get; set; } = "";

        [XmlElement("TargetLoc")]
        public string TargetLoc { get; set; } = "";

        [XmlElement("TargetPort")]
        public string TargetPort { get; set; } = "";

        [XmlElement("JobID")]
        public string JobID { get; set; } = "";

        [XmlElement("MaterialType")]
        public string MaterialType { get; set; } = "";

        [XmlElement("ActionType")]
        public string ActionType { get; set; } = "";

        [XmlElement("UserID")]
        public string UserID { get; set; } = "";
    }
}
