using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ACS.Communication.Host.Models;

namespace ACS.Communication.Host
{
    /// <summary>
    /// Host TCP/IP 메시지 프레이밍 프로토콜.
    /// 프레임 형식: [4바이트 길이(big-endian, network byte order)] + [UTF-8 XML 본문 (BOM 포함 가능)]
    /// </summary>
    public static class HostMessageProtocol
    {
        /// <summary>
        /// 메시지를 4바이트 길이 헤더(big-endian) + XML 본문으로 전송.
        /// </summary>
        public static async Task WriteMessageAsync(NetworkStream stream, string xml, CancellationToken ct = default)
        {
            byte[] body = Encoding.UTF8.GetBytes(xml);
            byte[] length = BitConverter.GetBytes(body.Length);
            
            await stream.WriteAsync(length, 0, 4, ct).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }
        
        // 3. XML 헬퍼 메서드들
        private static async Task SendAsync(NetworkStream stream, string data)
        {
            // XmlSerializer를 사용하여 객체를 XML로 변환
            var serializer = new XmlSerializer(data.GetType());

            // UTF-8 인코딩을 명시적으로 사용하기 위해 전용 StringWriter/XmlWriter 설정 가능하지만,
            // MemoryStream과 StreamWriter(Encoding.UTF8) 조합이 가장 확실합니다.
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    serializer.Serialize(sw, data);
                    sw.Flush(); // StreamWriter 버퍼를 MemoryStream으로 밀어넣기
                }

                byte[] body = ms.ToArray();
                // 헤더: 데이터 본문의 길이를 4바이트(int)로 변환
                byte[] length = BitConverter.GetBytes(body.Length);

                // [길이 4바이트] + [본문 XML] 순서로 전송
                await stream.WriteAsync(length, 0, 4);
                await stream.WriteAsync(body, 0, body.Length);
                await stream.FlushAsync();
            }
        }

        /// <summary>
        /// Host에 TCP 연결 → 메시지 송신 → 연결 종료 (메시지별 connect-send-close 패턴).
        /// MES가 메시지 단위로 연결을 기대하는 경우 사용.
        /// </summary>
        public static async Task ConnectAndSendAsync(string host, int port, string xml, CancellationToken ct = default)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(host, port, ct).ConfigureAwait(false);
                using (var stream = client.GetStream())
                {
                    await WriteMessageAsync(stream, xml, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 객체를 직렬화하여 Host에 TCP 연결 → 메시지 송신 → 연결 종료.
        /// </summary>
        public static async Task ConnectAndSendAsync<T>(string host, int port, T obj, CancellationToken ct = default)
        {
            string xml = HostXmlSerializer.Serialize(obj);
            await ConnectAndSendAsync(host, port, xml, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 4바이트 길이 헤더(big-endian)를 읽고 해당 길이만큼 본문을 읽어 반환.
        /// 본문의 BOM(EF BB BF)은 자동 스킵.
        /// 연결 끊어지면 null 반환.
        /// </summary>
        /// <summary>
        /// 4바이트 길이 헤더를 읽고 해당 길이만큼 본문을 읽어 XmlSerializer로 역직렬화.
        /// SendAsync&lt;T&gt;의 대칭 메서드.
        /// 연결 끊어지면 default(T) 반환.
        /// </summary>
        public static async Task<T> ReceiveAsync<T>(NetworkStream stream, CancellationToken ct = default)
        {
            // 1. 4바이트 길이 헤더 읽기
            byte[] header = new byte[4];
            int read = await ReadExactAsync(stream, header, 0, 4, ct).ConfigureAwait(false);
            if (read < 4)
                return default;

            int length = BitConverter.ToInt32(header, 0);

            if (length <= 0 || length > 10 * 1024 * 1024) // 최대 10MB
                return default;

            // 2. 본문 읽기
            byte[] body = new byte[length];
            read = await ReadExactAsync(stream, body, 0, length, ct).ConfigureAwait(false);
            if (read < length)
                return default;

            // 3. XmlSerializer로 역직렬화 (MemoryStream + StreamReader)
            var serializer = new XmlSerializer(typeof(T));
            using (var ms = new MemoryStream(body))
            {
                using (var sr = new StreamReader(ms, Encoding.UTF8))
                {
                    return (T)serializer.Deserialize(sr);
                }
            }
        }

        /// <summary>
        /// 4바이트 길이 헤더를 읽고 해당 길이만큼 본문을 읽어 문자열로 반환.
        /// MemoryStream + StreamReader 사용 (SendAsync의 MemoryStream + StreamWriter와 대칭).
        /// 연결 끊어지면 null 반환.
        /// </summary>
        public static async Task<string> ReadMessageAsync(NetworkStream stream, CancellationToken ct = default)
        {
            // 1. 4바이트 길이 헤더 읽기
            byte[] header = new byte[4];
            int read = await ReadExactAsync(stream, header, 0, 4, ct).ConfigureAwait(false);
            if (read < 4)
                return null; // 연결 끊김

            int length = BitConverter.ToInt32(header, 0);

            if (length <= 0 || length > 10 * 1024 * 1024) // 최대 10MB
                return null;

            // 2. 본문 읽기
            byte[] body = new byte[length];
            read = await ReadExactAsync(stream, body, 0, length, ct).ConfigureAwait(false);
            if (read < length)
                return null;

            // 3. MemoryStream + StreamReader로 문자열 변환 (BOM 자동 처리)
            using (var ms = new MemoryStream(body))
            {
                using (var sr = new StreamReader(ms, Encoding.UTF8))
                {
                    return sr.ReadToEnd().Trim();
                }
            }
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
