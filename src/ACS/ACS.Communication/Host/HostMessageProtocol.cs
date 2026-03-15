using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Host
{
    /// <summary>
    /// Host TCP/IP 메시지 프레이밍 프로토콜.
    /// 프레임 형식: [4바이트 길이(little-endian)] + [UTF-8 XML 본문 (BOM 포함 가능)]
    /// </summary>
    public static class HostMessageProtocol
    {
        /// <summary>
        /// 메시지를 4바이트 길이 헤더(little-endian) + XML 본문으로 전송.
        /// </summary>
        public static async Task WriteMessageAsync(NetworkStream stream, string xml, CancellationToken ct = default)
        {
            byte[] header = BitConverter.GetBytes(xml.Length);
            byte[] body = Encoding.UTF8.GetBytes(xml);

            await stream.WriteAsync(header, 0, 4, ct).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 4바이트 길이 헤더(little-endian)를 읽고 해당 길이만큼 본문을 읽어 반환.
        /// 본문의 BOM(EF BB BF)은 자동 스킵.
        /// 연결 끊어지면 null 반환.
        /// </summary>
        public static async Task<string> ReadMessageAsync(NetworkStream stream, CancellationToken ct = default)
        {
            // 1. 4바이트 길이 헤더 읽기
            byte[] header = new byte[4];
            int read = await ReadExactAsync(stream, header, 0, 4, ct).ConfigureAwait(false);
            if (read < 4)
                return null; // 연결 끊김

            // little-endian → int
            int length = BitConverter.ToInt32(header, 0);

            if (length <= 0 || length > 10 * 1024 * 1024) // 최대 10MB
                return null;

            // 2. 본문 읽기
            byte[] body = new byte[length];
            read = await ReadExactAsync(stream, body, 0, length, ct).ConfigureAwait(false);
            if (read < length)
                return null;

            // 3. BOM 스킵 (EF BB BF)
            int start = 0;
            if (body.Length >= 3 && body[0] == 0xEF && body[1] == 0xBB && body[2] == 0xBF)
                start = 3;

            return Encoding.UTF8.GetString(body, start, body.Length - start).Trim();
        }

        /// <summary>
        /// XML 문자열에서 메시지 이름(Command) 추출.
        /// Host 메시지 형식: &lt;Msg&gt;&lt;Command&gt;LOAD_COMPLETED&lt;/Command&gt;...&lt;/Msg&gt;
        /// </summary>
        public static string ExtractMessageName(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return "UNKNOWN";

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                // 실제 Host 메시지 형식: <Msg><Command>LOAD_COMPLETED</Command>...
                string[] paths = new[]
                {
                    "//Command", "//command", "//COMMAND",
                    "//Header/MsgName", "//Header/MessageName",
                    "//MsgName", "//MessageName"
                };

                foreach (var path in paths)
                {
                    var node = doc.SelectSingleNode(path);
                    if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
                        return node.InnerText.Trim();
                }

                // fallback: 루트 요소 이름
                if (doc.DocumentElement != null)
                    return doc.DocumentElement.Name;

                return "UNKNOWN";
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// Host로 보낼 XML 메시지를 생성하는 헬퍼.
        /// Host와 동일한 형식: &lt;Msg&gt;&lt;Command&gt;...&lt;/Command&gt;&lt;Header&gt;...&lt;/Header&gt;&lt;DataLayer&gt;...&lt;/DataLayer&gt;&lt;/Msg&gt;
        /// </summary>
        public static string BuildMessage(string command, string destSubject, string replySubject,
            Action<XmlElement> dataBuilder = null)
        {
            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            var root = doc.CreateElement("Msg");
            doc.AppendChild(root);

            var cmdElem = doc.CreateElement("Command");
            cmdElem.InnerText = command;
            root.AppendChild(cmdElem);

            var header = doc.CreateElement("Header");
            root.AppendChild(header);

            var dest = doc.CreateElement("DestSubject");
            dest.InnerText = destSubject ?? "";
            header.AppendChild(dest);

            var reply = doc.CreateElement("ReplySubject");
            reply.InnerText = replySubject ?? "";
            header.AppendChild(reply);

            var dataLayer = doc.CreateElement("DataLayer");
            root.AppendChild(dataLayer);

            dataBuilder?.Invoke(dataLayer);

            return doc.OuterXml;
        }

        /// <summary>
        /// 정확히 count 바이트를 읽을 때까지 반복.
        /// 연결 끊기면 읽은 바이트 수 반환.
        /// </summary>
        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct).ConfigureAwait(false);
                if (read == 0)
                    break; // 연결 끊김
                totalRead += read;
            }
            return totalRead;
        }
    }
}
