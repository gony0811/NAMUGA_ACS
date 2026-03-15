using System.Xml;

namespace ACS.Core.Host
{
    /// <summary>
    /// Host(MES)로 보내는 메시지를 빌드하고 전송하는 서비스.
    /// ACS.Manager 없이도 독립적으로 동작.
    /// </summary>
    public interface IHostMessageService
    {
        /// <summary>
        /// JOBREPORT 메시지 빌드.
        /// Host에게 작업 상태를 보고하는 XML 메시지를 생성.
        /// </summary>
        /// <param name="reportType">RECEIVE, ARRIVED, COMPLETE, CANCEL 등</param>
        /// <param name="jobId">작업 ID</param>
        /// <param name="amrId">AMR(AGV) ID</param>
        /// <param name="materialType">자재 타입 (MAGAZINE 등)</param>
        /// <param name="acsId">ACS 시스템 ID</param>
        /// <param name="userId">요청 사용자 ID</param>
        /// <param name="destSubject">목적지 subject (예: /HQ/MES01)</param>
        /// <param name="replySubject">응답 subject (예: /HQ/ACS01)</param>
        /// <returns>빌드된 JOBREPORT XML</returns>
        XmlDocument BuildJobReport(
            string reportType,
            string jobId,
            string amrId = "",
            string materialType = "",
            string acsId = "",
            string userId = "",
            string destSubject = "",
            string replySubject = "");

        /// <summary>
        /// 수신한 MOVECMD XML에서 필드를 추출하여 JOBREPORT RECEIVE 응답을 빌드.
        /// </summary>
        XmlDocument BuildJobReportFromMoveCmd(XmlDocument moveCmdXml, string reportType = "RECEIVE");

        /// <summary>
        /// 빌드된 XML을 Host TCP/IP로 전송.
        /// </summary>
        void SendToHost(string messageName, XmlDocument document);
    }
}
