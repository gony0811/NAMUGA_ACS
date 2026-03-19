using System.Xml.Serialization;

namespace ACS.Communication.Host.Models
{
    /// <summary>
    /// Host 메시지 Header 요소.
    /// </summary>
    public class HostHeader
    {
        [XmlElement("DestSubject")]
        public string DestSubject { get; set; } = "";

        [XmlElement("ReplySubject")]
        public string ReplySubject { get; set; } = "";
    }
}
