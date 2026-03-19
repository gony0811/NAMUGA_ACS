using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ACS.Communication.Host
{
    /// <summary>
    /// Host XML 메시지 직렬화/역직렬화 유틸리티.
    /// 네임스페이스 없는 UTF-8 XML을 생성.
    /// </summary>
    public static class HostXmlSerializer
    {
        private static readonly XmlSerializerNamespaces EmptyNamespaces;

        static HostXmlSerializer()
        {
            EmptyNamespaces = new XmlSerializerNamespaces();
            EmptyNamespaces.Add("", "");
        }

        /// <summary>
        /// 객체를 XML 문자열로 직렬화.
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            var serializer = new XmlSerializer(typeof(T));

            using (var ms = new MemoryStream())
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = false,
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(ms, settings))
                {
                    serializer.Serialize(writer, obj, EmptyNamespaces);
                }

                // UTF-8 BOM 스킵
                byte[] bytes = ms.ToArray();
                int offset = 0;
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    offset = 3;

                return Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
            }
        }

        /// <summary>
        /// XML 문자열을 객체로 역직렬화.
        /// </summary>
        public static T Deserialize<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));

            using (var sr = new StringReader(xml))
            {
                return (T)serializer.Deserialize(sr);
            }
        }
    }
}
