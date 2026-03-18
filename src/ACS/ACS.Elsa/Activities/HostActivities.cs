using System;
using System.Xml;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  Host Message Activities
    //  Category: ACS.Host
    //
    //  Host(MES)와 주고받는 메시지를 빌드하고 전송하는 Activity 모음.
    //  AutofacContainerAccessor를 통해 IHostMessageService resolve →
    //  XML 빌드 → HostTcpGateway로 전송.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// JOBREPORT 전송 Activity.
    ///
    /// MOVECMD 수신 후 워크플로우에서 호출하여 Host에 작업 수신 확인(RECEIVE),
    /// 도착(ARRIVED), 완료(COMPLETE), 취소(CANCEL) 등의 상태를 보고.
    ///
    /// 사용법 1 — MOVECMD XML 자동 변환:
    ///   MoveCmdXml 입력에 수신한 MOVECMD XmlDocument를 넣으면
    ///   자동으로 필드를 추출하여 JOBREPORT를 빌드.
    ///
    /// 사용법 2 — 개별 필드 지정:
    ///   JobId, AmrId, MaterialType 등을 직접 설정.
    /// </summary>
    [Activity("ACS.Host", "Send Job Report",
        "Host(MES)에 JOBREPORT 메시지를 빌드하여 전송합니다.")]
    public class SendJobReportActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        /// <summary>수신한 MOVECMD XML. 설정하면 자동으로 필드 추출.</summary>
        [Input(Description = "수신한 MOVECMD XmlDocument (자동 변환 시 사용)")]
        public Input<XmlDocument> MoveCmdXml { get; set; }

        /// <summary>리포트 타입: RECEIVE, ARRIVED, COMPLETE, CANCEL 등</summary>
        [Input(Description = "리포트 타입 (RECEIVE, ARRIVED, COMPLETE, CANCEL 등)")]
        public Input<string> ReportType { get; set; } = new("RECEIVE");

        /// <summary>작업 ID</summary>
        [Input(Description = "작업 ID (미설정 시 MOVECMD XML에서 추출)")]
        public Input<string> JobId { get; set; }

        /// <summary>AMR(AGV) ID</summary>
        [Input(Description = "AMR ID (미설정 시 MOVECMD XML에서 추출)")]
        public Input<string> AmrId { get; set; }

        /// <summary>자재 타입</summary>
        [Input(Description = "자재 타입 (MAGAZINE 등, 미설정 시 MOVECMD XML에서 추출)")]
        public Input<string> MaterialType { get; set; }

        /// <summary>ACS 시스템 ID</summary>
        [Input(Description = "ACS ID (미설정 시 appsettings.json에서 가져옴)")]
        public Input<string> AcsId { get; set; }

        /// <summary>사용자 ID</summary>
        [Input(Description = "사용자 ID (미설정 시 MOVECMD XML에서 추출)")]
        public Input<string> UserId { get; set; }

        /// <summary>빌드된 JOBREPORT XML (후속 Activity에서 사용 가능)</summary>
        [Output(Description = "빌드된 JOBREPORT XmlDocument")]
        public Output<XmlDocument> JobReportXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // AutofacContainerAccessor를 통해 Autofac 서비스 접근
                var accessor = context.GetService<AutofacContainerAccessor>();
                var hostMessageService = accessor?.Resolve<IHostMessageService>();

                if (hostMessageService == null)
                {
                    logger.Error("SendJobReportActivity: IHostMessageService not available (AutofacContainerAccessor not linked?)");
                    context.Set(Result, false);
                    return;
                }

                string reportType = ReportType?.Get(context) ?? "RECEIVE";
                var moveCmdXml = MoveCmdXml?.Get(context);
                XmlDocument jobReport;

                if (moveCmdXml != null)
                {
                    // MOVECMD XML에서 자동 변환
                    jobReport = hostMessageService.BuildJobReportFromMoveCmd(moveCmdXml, reportType);

                    // 개별 필드가 명시적으로 설정된 경우 오버라이드
                    OverrideField(context, jobReport, JobId, "//DataLayer/JobID");
                    OverrideField(context, jobReport, AmrId, "//DataLayer/AmrId");
                    OverrideField(context, jobReport, MaterialType, "//DataLayer/MaterialType");
                    OverrideField(context, jobReport, AcsId, "//DataLayer/AcsId");
                    OverrideField(context, jobReport, UserId, "//DataLayer/UserID");
                }
                else
                {
                    // 개별 필드로 빌드
                    string jobId = JobId?.Get(context);
                    if (string.IsNullOrEmpty(jobId))
                    {
                        logger.Error("SendJobReportActivity: JobId is required when MoveCmdXml is not provided");
                        context.Set(Result, false);
                        return;
                    }

                    jobReport = hostMessageService.BuildJobReport(
                        reportType,
                        jobId,
                        AmrId?.Get(context) ?? "",
                        MaterialType?.Get(context) ?? "",
                        AcsId?.Get(context) ?? "",
                        UserId?.Get(context) ?? "");
                }

                // Host로 전송
                hostMessageService.SendToHost("JOBREPORT", jobReport);

                // Output 설정
                context.Set(JobReportXml, jobReport);
                context.Set(Result, true);

                logger.Info($"SendJobReportActivity: JOBREPORT ({reportType}) sent successfully");
            }
            catch (Exception ex)
            {
                logger.Error($"SendJobReportActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }

        private void OverrideField(ActivityExecutionContext context, XmlDocument doc, Input<string> input, string xpath)
        {
            if (input == null) return;
            string value = input.Get(context);
            if (string.IsNullOrEmpty(value)) return;

            var node = doc.SelectSingleNode(xpath);
            if (node != null)
                node.InnerText = value;
        }
    }

    /// <summary>
    /// MOVECMD 수신 시 즉시 RECEIVE 응답을 보내는 단축 Activity.
    /// SendJobReportActivity의 ReportType=RECEIVE 프리셋.
    /// </summary>
    [Activity("ACS.Host", "Reply MoveCmd Receive",
        "MOVECMD에 대한 RECEIVE 응답(JOBREPORT)을 Host에 전송합니다.")]
    public class ReplyMoveCmdReceiveActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "수신한 MOVECMD XmlDocument")]
        public Input<XmlDocument> MoveCmdXml { get; set; }

        [Output(Description = "빌드된 JOBREPORT XmlDocument")]
        public Output<XmlDocument> JobReportXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var hostMessageService = accessor?.Resolve<IHostMessageService>();

                if (hostMessageService == null)
                {
                    logger.Error("ReplyMoveCmdReceiveActivity: IHostMessageService not available");
                    context.Set(Result, false);
                    return;
                }

                var moveCmdXml = MoveCmdXml?.Get(context);
                if (moveCmdXml == null)
                {
                    logger.Error("ReplyMoveCmdReceiveActivity: MoveCmdXml is required");
                    context.Set(Result, false);
                    return;
                }

                var jobReport = hostMessageService.BuildJobReportFromMoveCmd(moveCmdXml, "RECEIVE");
                hostMessageService.SendToHost("JOBREPORT", jobReport);

                context.Set(JobReportXml, jobReport);
                context.Set(Result, true);

                logger.Info("ReplyMoveCmdReceiveActivity: RECEIVE reply sent");
            }
            catch (Exception ex)
            {
                logger.Error($"ReplyMoveCmdReceiveActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }

    /// <summary>
    /// ACTIONCMD 수신 시 RECEIVE 응답을 보내는 Activity.
    /// </summary>
    [Activity("ACS.Host", "Reply ActionCmd Receive",
        "ACTIONCMD에 대한 RECEIVE 응답(JOBREPORT)을 Host에 전송합니다.")]
    public class ReplyActionCmdReceiveActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "수신한 ACTIONCMD XmlDocument")]
        public Input<XmlDocument> ActionCmdXml { get; set; }

        [Output(Description = "빌드된 JOBREPORT XmlDocument")]
        public Output<XmlDocument> JobReportXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var hostMessageService = accessor?.Resolve<IHostMessageService>();

                if (hostMessageService == null)
                {
                    logger.Error("ReplyActionCmdReceiveActivity: IHostMessageService not available");
                    context.Set(Result, false);
                    return;
                }

                var actionCmdXml = ActionCmdXml?.Get(context);
                if (actionCmdXml == null)
                {
                    logger.Error("ReplyActionCmdReceiveActivity: ActionCmdXml is required");
                    context.Set(Result, false);
                    return;
                }

                var jobReport = hostMessageService.BuildJobReportFromMoveCmd(actionCmdXml, "RECEIVE");
                hostMessageService.SendToHost("JOBREPORT", jobReport);

                context.Set(JobReportXml, jobReport);
                context.Set(Result, true);

                logger.Info("ReplyActionCmdReceiveActivity: RECEIVE reply sent");
            }
            catch (Exception ex)
            {
                logger.Error($"ReplyActionCmdReceiveActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }
}
