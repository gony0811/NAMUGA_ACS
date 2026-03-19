using System.Xml.Serialization;

namespace ACS.Communication.Host.Models
{
    /// <summary>
    /// Host XML 메시지 제네릭 루트 클래스.
    /// 형식: &lt;Msg&gt;&lt;Command&gt;...&lt;/Command&gt;&lt;Header&gt;...&lt;/Header&gt;&lt;DataLayer&gt;...&lt;/DataLayer&gt;&lt;/Msg&gt;
    /// </summary>
    [XmlRoot("Msg")]
    public class HostMessage<TData> where TData : class, new()
    {
        [XmlElement("Command")]
        public string Command { get; set; } = "";

        [XmlElement("Header")]
        public HostHeader Header { get; set; } = new HostHeader();

        [XmlElement("DataLayer")]
        public TData DataLayer { get; set; } = new TData();
    }
}
