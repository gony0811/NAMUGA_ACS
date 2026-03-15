using System;
using System.Xml;
using Microsoft.Extensions.Configuration;
using ACS.Core.Host;
using ACS.Core.Logging;

namespace ACS.App.Host
{
    /// <summary>
    /// Host 메시지 빌드 및 전송 서비스.
    ///
    /// Host(MES) XML 형식:
    /// <![CDATA[
    /// <Msg>
    ///   <Command>JOBREPORT</Command>
    ///   <Header>
    ///     <DestSubject>/HQ/MES01</DestSubject>
    ///     <ReplySubject>/HQ/ACS01</ReplySubject>
    ///   </Header>
    ///   <DataLayer>
    ///     <AcsId>ACS01</AcsId>
    ///     <Type>RECEIVE</Type>
    ///     <AmrId>AMR01</AmrId>
    ///     <JobID>JOB20260303141832001</JobID>
    ///     <MaterialType>MAGAZINE</MaterialType>
    ///     <UserID>MES01</UserID>
    ///   </DataLayer>
    /// </Msg>
    /// ]]>
    /// </summary>
    public class HostMessageService : IHostMessageService
    {
        private readonly IHostTcpGateway _tcpGateway;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger = Logger.GetLogger(typeof(HostMessageService));

        public HostMessageService(IHostTcpGateway tcpGateway, IConfiguration configuration)
        {
            _tcpGateway = tcpGateway;
            _configuration = configuration;
        }

        public XmlDocument BuildJobReport(
            string reportType,
            string jobId,
            string amrId = "",
            string materialType = "",
            string acsId = "",
            string userId = "",
            string destSubject = "",
            string replySubject = "")
        {
            // appsettings.json에서 기본값 가져오기
            if (string.IsNullOrEmpty(acsId))
                acsId = _configuration["Acs:Process:Name"] ?? "ACS01";
            if (string.IsNullOrEmpty(destSubject))
                destSubject = _configuration["Acs:Host:DestSubject"] ?? "/HQ/MES01";
            if (string.IsNullOrEmpty(replySubject))
                replySubject = _configuration["Acs:Host:ReplySubject"] ?? "/HQ/ACS01";

            var doc = new XmlDocument();

            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            var msg = doc.CreateElement("Msg");
            doc.AppendChild(msg);

            // Command
            AppendElement(doc, msg, "Command", "JOBREPORT");

            // Header
            var header = doc.CreateElement("Header");
            msg.AppendChild(header);
            AppendElement(doc, header, "DestSubject", destSubject);
            AppendElement(doc, header, "ReplySubject", replySubject);

            // DataLayer
            var dataLayer = doc.CreateElement("DataLayer");
            msg.AppendChild(dataLayer);
            AppendElement(doc, dataLayer, "AcsId", acsId);
            AppendElement(doc, dataLayer, "Type", reportType);
            AppendElement(doc, dataLayer, "AmrId", amrId ?? "");
            AppendElement(doc, dataLayer, "JobID", jobId);
            AppendElement(doc, dataLayer, "MaterialType", materialType ?? "");
            AppendElement(doc, dataLayer, "UserID", userId ?? "");

            _logger.Info($"[HostMessageService] Built JOBREPORT: Type={reportType}, JobID={jobId}");
            return doc;
        }

        public XmlDocument BuildJobReportFromMoveCmd(XmlDocument moveCmdXml, string reportType = "RECEIVE")
        {
            // MOVECMD XML에서 필드 추출
            string jobId = ExtractValue(moveCmdXml, "//JobID")
                        ?? ExtractValue(moveCmdXml, "//DataLayer/JobID")
                        ?? GenerateJobId();

            string amrId = ExtractValue(moveCmdXml, "//AmrId")
                        ?? ExtractValue(moveCmdXml, "//DataLayer/AmrId")
                        ?? "";

            string materialType = ExtractValue(moveCmdXml, "//MaterialType")
                               ?? ExtractValue(moveCmdXml, "//DataLayer/MaterialType")
                               ?? "";

            string acsId = ExtractValue(moveCmdXml, "//AcsId")
                        ?? ExtractValue(moveCmdXml, "//DataLayer/AcsId")
                        ?? "";

            string userId = ExtractValue(moveCmdXml, "//UserID")
                         ?? ExtractValue(moveCmdXml, "//DataLayer/UserID")
                         ?? "";

            string destSubject = ExtractValue(moveCmdXml, "//Header/ReplySubject") ?? "";
            string replySubject = ExtractValue(moveCmdXml, "//Header/DestSubject") ?? "";

            return BuildJobReport(reportType, jobId, amrId, materialType, acsId, userId,
                destSubject, replySubject);
        }

        public void SendToHost(string messageName, XmlDocument document)
        {
            if (document == null)
            {
                _logger.Warn($"[HostMessageService] SendToHost - null document for {messageName}");
                return;
            }

            string xml = document.OuterXml;
            _logger.Info($"[HostMessageService] Sending {messageName} ({xml.Length} bytes)");
            _tcpGateway.SendToHost(messageName, xml);
        }

        private static string GenerateJobId()
        {
            return $"JOB{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999):D3}";
        }

        private static string ExtractValue(XmlDocument doc, string xpath)
        {
            try
            {
                var node = doc.SelectSingleNode(xpath);
                return string.IsNullOrWhiteSpace(node?.InnerText) ? null : node.InnerText.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static void AppendElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var elem = doc.CreateElement(name);
            elem.InnerText = value ?? "";
            parent.AppendChild(elem);
        }
    }
}
