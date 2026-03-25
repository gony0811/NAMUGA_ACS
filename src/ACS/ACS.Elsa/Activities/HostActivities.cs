using System;
using System.Xml;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Core.Base;
using ACS.Core.Path;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
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

        /// <summary>에러 코드 (0=정상, 그 외=에러)</summary>
        [Input(Description = "에러 코드 (0=정상, 그 외=에러)")]
        public Input<string> ErrCode { get; set; }

        /// <summary>에러 메시지</summary>
        [Input(Description = "에러 메시지")]
        public Input<string> ErrMsg { get; set; }

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
                string errCode = ErrCode?.Get(context) ?? "0";
                string errMsg = ErrMsg?.Get(context) ?? "";
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
                        amrId: AmrId?.Get(context) ?? "",
                        materialType: MaterialType?.Get(context) ?? "",
                        acsId: AcsId?.Get(context) ?? "",
                        userId: UserId?.Get(context) ?? "");
                }

                // ErrCode/ErrMsg를 DataLayer에 추가
                var dataLayer = jobReport.SelectSingleNode("//DataLayer");
                if (dataLayer != null)
                {
                    var errCodeNode = jobReport.CreateElement("ErrCode");
                    errCodeNode.InnerText = errCode;
                    dataLayer.AppendChild(errCodeNode);

                    var errMsgNode = jobReport.CreateElement("ErrMsg");
                    errMsgNode.InnerText = errMsg;
                    dataLayer.AppendChild(errMsgNode);
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

    /// <summary>
    /// MOVECMD XML에서 TransportCommand를 생성하여 DB에 저장하는 Activity.
    ///
    /// MOVECMD XML의 DataLayer 필드를 추출하여 TransportCommandEx 객체를 구성하고
    /// ITransferManagerEx.CreateTransportCommand()를 통해 DB에 insert.
    ///
    /// 필드 매핑:
    ///   - JobID → TransportCommandEx.Id
    ///   - SourceLoc:SourcePort → TransportCommandEx.Source
    ///   - DestLoc:DestPort → TransportCommandEx.Dest
    ///   - ActionType → TransportCommandEx.JobType
    ///   - MaterialType → TransportCommandEx.Description
    ///   - AcsId → TransportCommandEx.EqpId
    /// </summary>
    [Activity("ACS.Host", "Create Transport Command",
        "MOVECMD에서 TransportCommand를 생성하여 DB에 저장합니다.")]
    public class CreateTransportCommandActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        /// <summary>수신한 MOVECMD XML</summary>
        [Input(Description = "수신한 MOVECMD XmlDocument")]
        public Input<XmlDocument> MoveCmdXml { get; set; }

        /// <summary>생성된 TransportCommand ID (후속 Activity에서 사용 가능)</summary>
        [Output(Description = "생성된 TransportCommand ID")]
        public Output<string> TransportCommandId { get; set; }

        /// <summary>에러 코드 (성공 시 "0")</summary>
        [Output(Description = "에러 코드 (성공 시 '0')")]
        public Output<string> ErrCode { get; set; }

        /// <summary>에러 메시지 (성공 시 빈 문자열)</summary>
        [Output(Description = "에러 메시지 (성공 시 빈 문자열)")]
        public Output<string> ErrMsg { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();

                if (transferManager == null)
                {
                    logger.Error("CreateTransportCommandActivity: ITransferManagerEx not available");
                    context.Set(ErrCode, "03");
                    context.Set(ErrMsg, "ITransferManagerEx not available");
                    context.Set(Result, false);
                    return;
                }

                var moveCmdXml = MoveCmdXml?.Get(context);
                if (moveCmdXml == null)
                {
                    logger.Error("CreateTransportCommandActivity: MoveCmdXml is required");
                    context.Set(ErrCode, "03");
                    context.Set(ErrMsg, "MoveCmdXml is required");
                    context.Set(Result, false);
                    return;
                }

                // MOVECMD XML에서 필드 추출
                string jobId = ExtractValue(moveCmdXml, "//DataLayer/JobID")
                            ?? ExtractValue(moveCmdXml, "//JobID")
                            ?? $"JOB{DateTime.Now:yyyyMMddHHmmssffff}";
                string sourceLoc = ExtractValue(moveCmdXml, "//DataLayer/SourceLoc")
                                ?? ExtractValue(moveCmdXml, "//SourceLoc") ?? "";
                string sourcePort = ExtractValue(moveCmdXml, "//DataLayer/SourcePort")
                                 ?? ExtractValue(moveCmdXml, "//SourcePort") ?? "";
                string destLoc = ExtractValue(moveCmdXml, "//DataLayer/DestLoc")
                              ?? ExtractValue(moveCmdXml, "//DestLoc") ?? "";
                string destPort = ExtractValue(moveCmdXml, "//DataLayer/DestPort")
                               ?? ExtractValue(moveCmdXml, "//DestPort") ?? "";
                string actionType = ExtractValue(moveCmdXml, "//DataLayer/ActionType")
                                 ?? ExtractValue(moveCmdXml, "//ActionType") ?? "";
                string materialType = ExtractValue(moveCmdXml, "//DataLayer/MaterialType")
                                   ?? ExtractValue(moveCmdXml, "//MaterialType") ?? "";
                string acsId = ExtractValue(moveCmdXml, "//DataLayer/AcsId")
                            ?? ExtractValue(moveCmdXml, "//AcsId") ?? "";

                // Source, Dest 조합: "SourceLoc:SourcePort" 형식
                string source = string.IsNullOrEmpty(sourcePort) ? sourceLoc : $"{sourceLoc}:{sourcePort}";
                string dest = string.IsNullOrEmpty(destPort) ? destLoc : $"{destLoc}:{destPort}";

                // 중복 검증: 동일 JobID가 이미 DB에 존재하면 생성하지 않음
                if (transferManager.ExistTransportCommand(jobId))
                {
                    logger.Warn($"CreateTransportCommandActivity: TransportCommand already exists - Id={jobId}, skipping creation");
                    context.Set(TransportCommandId, jobId);
                    context.Set(ErrCode, AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1);
                    context.Set(ErrMsg, AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2);
                    context.Set(Result, false);
                    return;
                }

                // Source/Dest 동일 Bay 검증
                var pathManager = accessor.Resolve<IPathManagerEx>();
                var sourceLocation = pathManager.GetLocationByLocationId(source);
                if (sourceLocation == null)
                {
                    logger.Warn($"CreateTransportCommandActivity: Source location not found - Source={source}");
                    context.Set(ErrCode, AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item1);
                    context.Set(ErrMsg, AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item2);
                    context.Set(Result, false);
                    return;
                }

                var destLocation = pathManager.GetLocationByLocationId(dest);
                if (destLocation == null)
                {
                    logger.Warn($"CreateTransportCommandActivity: Dest location not found - Dest={dest}");
                    context.Set(ErrCode, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1);
                    context.Set(ErrMsg, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2);
                    context.Set(Result, false);
                    return;
                }

                string sameBayId = pathManager.GetCommonUseBayIdBySourceDest(
                    sourceLocation.StationId, destLocation.StationId, "Y");

                if (sameBayId == null)
                {
                    logger.Warn($"CreateTransportCommandActivity: No common Bay - Source={source}({sourceLocation.StationId}), Dest={dest}({destLocation.StationId})");
                    context.Set(ErrCode, AbstractManager.ID_RESULT_NOTSAMEBAY.Item1);
                    context.Set(ErrMsg, AbstractManager.ID_RESULT_NOTSAMEBAY.Item2);
                    context.Set(Result, false);
                    return;
                }

                // TransportCommandEx 생성
                var transportCommand = new TransportCommandEx
                {
                    JobId = jobId,
                    Source = source,
                    Dest = dest,
                    BayId = sameBayId,
                    Priority = TransportCommandEx.DEFAULT_PRIORITY,
                    State = TransportCommandEx.STATE_QUEUED,
                    JobType = actionType,
                    EqpId = acsId,
                    Description = materialType,
                    CreateTime = DateTime.Now,
                    QueuedTime = DateTime.Now,
                    // 나머지 시간 필드 null로 초기화
                    AssignedTime = null,
                    CompletedTime = null,
                    LoadArrivedTime = null,
                    LoadingTime = null,
                    StartedTime = null,
                    UnloadArrivedTime = null,
                    UnloadedTime = null,
                    UnloadingTime = null,
                    LoadedTime = null
                };

                // DB 저장
                transferManager.CreateTransportCommand(transportCommand);

                context.Set(TransportCommandId, jobId);
                context.Set(ErrCode, "0");
                context.Set(ErrMsg, "");
                context.Set(Result, true);

                logger.Info($"CreateTransportCommandActivity: TransportCommand created - Id={jobId}, Source={source}, Dest={dest}, BayId={sameBayId}, JobType={actionType}");
            }
            catch (Exception ex)
            {
                logger.Error($"CreateTransportCommandActivity: {ex.Message}", ex);
                context.Set(ErrCode, "03");
                context.Set(ErrMsg, ex.Message);
                context.Set(Result, false);
            }
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
    }

    // ═══════════════════════════════════════════════════════════════
    //  JOBREPORT 수신 → 검증 → MES 전달 Activities
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 워크플로우 Input(Arguments)에서 JOBREPORT XmlDocument를 추출하는 Activity.
    /// </summary>
    [Activity("ACS.Host", "Extract JobReport XML",
        "워크플로우 입력에서 JOBREPORT XmlDocument를 추출합니다.")]
    public class ExtractJobReportFromInput : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "추출된 JOBREPORT XmlDocument")]
        public Output<XmlDocument> OutputXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            XmlDocument result = null;

            var input = context.WorkflowExecutionContext.Input;
            if (input != null && input.TryGetValue("Arguments", out var args))
            {
                if (args is object[] argsArray && argsArray.Length > 0)
                {
                    if (argsArray[0] is XmlDocument xmlDoc)
                    {
                        result = xmlDoc;
                    }
                    else if (argsArray[0] is string xmlString)
                    {
                        result = new XmlDocument();
                        result.LoadXml(xmlString);
                    }
                }
                else if (args is XmlDocument singleDoc)
                {
                    result = singleDoc;
                }
            }

            if (result == null)
            {
                result = new XmlDocument();
                result.LoadXml("<Msg><Command>JOBREPORT</Command><Header/><DataLayer/></Msg>");
                logger.Warn("ExtractJobReportFromInput: No JOBREPORT XML found in input, using empty template");
            }

            context.Set(OutputXml, result);
            logger.Info("ExtractJobReportFromInput: JOBREPORT XML extracted from workflow input");
        }
    }

    /// <summary>
    /// JOBREPORT 메시지를 DB의 TransportCommandEx와 대조 검증하는 Activity.
    ///
    /// 검증 항목:
    ///   1. JobID로 TransportCommandEx 존재 여부 확인
    ///   2. JOBREPORT Type vs TC State 정합성 (이미 완료/취소된 건에 대한 중복 보고 차단)
    ///   3. 데이터 일치 확인 (MaterialType↔Description, ActionType↔JobType) — 불일치 시 경고 로그
    /// </summary>
    [Activity("ACS.Host", "Validate Job Report",
        "JOBREPORT 메시지를 DB의 TransportCommandEx와 대조 검증합니다.")]
    public class ValidateJobReportActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "JOBREPORT XmlDocument")]
        public Input<XmlDocument> JobReportXml { get; set; }

        [Output(Description = "검증 실패 사유 (성공 시 빈 문자열)")]
        public Output<string> ValidationError { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();

                if (transferManager == null)
                {
                    logger.Error("ValidateJobReportActivity: ITransferManagerEx not available");
                    context.Set(Result, false);
                    context.Set(ValidationError, "ITransferManagerEx not available");
                    return;
                }

                var xml = JobReportXml?.Get(context);
                if (xml == null)
                {
                    logger.Error("ValidateJobReportActivity: JobReportXml is required");
                    context.Set(Result, false);
                    context.Set(ValidationError, "JobReportXml is null");
                    return;
                }

                // XML에서 필드 추출
                string jobId = ExtractValue(xml, "//DataLayer/JobID") ?? ExtractValue(xml, "//JobID");
                string type = ExtractValue(xml, "//DataLayer/Type") ?? ExtractValue(xml, "//Type");
                string materialType = ExtractValue(xml, "//DataLayer/MaterialType") ?? ExtractValue(xml, "//MaterialType");
                string actionType = ExtractValue(xml, "//DataLayer/ActionType") ?? ExtractValue(xml, "//ActionType");
                string amrId = ExtractValue(xml, "//DataLayer/AmrId") ?? ExtractValue(xml, "//AmrId");

                if (string.IsNullOrEmpty(jobId))
                {
                    logger.Error("ValidateJobReportActivity: JobID is missing from JOBREPORT XML");
                    context.Set(Result, false);
                    context.Set(ValidationError, "JobID is missing from JOBREPORT XML");
                    return;
                }

                // DB 조회
                var tc = transferManager.GetTransportCommand(jobId);
                if (tc == null)
                {
                    logger.Error($"ValidateJobReportActivity: TransportCommand not found - JobID={jobId}");
                    context.Set(Result, false);
                    context.Set(ValidationError, $"TransportCommand not found: JobID={jobId}");
                    return;
                }

                logger.Info($"ValidateJobReportActivity: Found TC - JobID={jobId}, State={tc.State}, JobType={tc.JobType}");

                // Type vs State 정합성 확인
                string typeUpper = type?.ToUpperInvariant() ?? "";
                string stateUpper = tc.State?.ToUpperInvariant() ?? "";

                // 이미 종료 상태인 TC에 대한 COMPLETE/CANCEL 보고 차단
                if ((typeUpper == "COMPLETE" || typeUpper == "CANCEL") &&
                    (stateUpper == TransportCommandEx.STATE_COMPLETED.ToUpperInvariant() ||
                     stateUpper == TransportCommandEx.STATE_CANCELED.ToUpperInvariant() ||
                     stateUpper == TransportCommandEx.STATE_ABORTED.ToUpperInvariant()))
                {
                    string error = $"TC already in terminal state: Type={type}, TC.State={tc.State}";
                    logger.Warn($"ValidateJobReportActivity: {error}");
                    context.Set(Result, false);
                    context.Set(ValidationError, error);
                    return;
                }

                // 데이터 일치 확인 (경고 로그, 전달은 진행)
                if (!string.IsNullOrEmpty(materialType) && !string.IsNullOrEmpty(tc.Description) &&
                    !string.Equals(materialType, tc.Description, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"ValidateJobReportActivity: MaterialType mismatch - Message={materialType}, TC.Description={tc.Description}");
                }

                if (!string.IsNullOrEmpty(actionType) && !string.IsNullOrEmpty(tc.JobType) &&
                    !string.Equals(actionType, tc.JobType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"ValidateJobReportActivity: ActionType mismatch - Message={actionType}, TC.JobType={tc.JobType}");
                }

                if (!string.IsNullOrEmpty(amrId) && !string.IsNullOrEmpty(tc.VehicleId) &&
                    !string.Equals(amrId, tc.VehicleId, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"ValidateJobReportActivity: AmrId mismatch - Message={amrId}, TC.VehicleId={tc.VehicleId}");
                }

                // 검증 성공
                context.Set(Result, true);
                context.Set(ValidationError, "");
                logger.Info($"ValidateJobReportActivity: Validation passed - JobID={jobId}, Type={type}");
            }
            catch (Exception ex)
            {
                logger.Error($"ValidateJobReportActivity: {ex.Message}", ex);
                context.Set(Result, false);
                context.Set(ValidationError, ex.Message);
            }
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
    }

    /// <summary>
    /// 검증된 JOBREPORT를 MES로 TCP 전달하는 Activity.
    /// 기존 IHostMessageService.SendToHost()를 통해 SendHost:SendPort로 전송.
    /// </summary>
    [Activity("ACS.Host", "Forward Job Report to MES",
        "검증된 JOBREPORT를 MES로 TCP 전달합니다.")]
    public class ForwardJobReportToMesActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "전달할 JOBREPORT XmlDocument")]
        public Input<XmlDocument> JobReportXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var hostMessageService = accessor?.Resolve<IHostMessageService>();

                if (hostMessageService == null)
                {
                    logger.Error("ForwardJobReportToMesActivity: IHostMessageService not available");
                    context.Set(Result, false);
                    return;
                }

                var xml = JobReportXml?.Get(context);
                if (xml == null)
                {
                    logger.Error("ForwardJobReportToMesActivity: JobReportXml is null");
                    context.Set(Result, false);
                    return;
                }

                hostMessageService.SendToHost("JOBREPORT", xml);
                context.Set(Result, true);

                string jobId = xml.SelectSingleNode("//DataLayer/JobID")?.InnerText ?? "unknown";
                string type = xml.SelectSingleNode("//DataLayer/Type")?.InnerText ?? "unknown";
                logger.Info($"ForwardJobReportToMesActivity: JOBREPORT forwarded to MES - JobID={jobId}, Type={type}");
            }
            catch (Exception ex)
            {
                logger.Error($"ForwardJobReportToMesActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }

    /// <summary>
    /// JOBREPORT Type에 따라 TransportCommandEx 상태를 업데이트하는 Activity.
    ///
    /// Type → State 매핑:
    ///   - ARRIVED → STATE_ARRIVED_SOURCE, LoadArrivedTime 기록
    ///   - COMPLETE → STATE_COMPLETED, CompletedTime 기록
    ///   - CANCEL → STATE_CANCELED
    ///   - RECEIVE → 상태 변경 없음
    /// </summary>
    [Activity("ACS.Host", "Update TC State from JobReport",
        "JOBREPORT Type에 따라 TransportCommandEx 상태를 업데이트합니다.")]
    public class UpdateTransportCommandStateActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "JOBREPORT XmlDocument")]
        public Input<XmlDocument> JobReportXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();

                if (transferManager == null)
                {
                    logger.Error("UpdateTransportCommandStateActivity: ITransferManagerEx not available");
                    context.Set(Result, false);
                    return;
                }

                var xml = JobReportXml?.Get(context);
                if (xml == null)
                {
                    logger.Error("UpdateTransportCommandStateActivity: JobReportXml is null");
                    context.Set(Result, false);
                    return;
                }

                string jobId = ExtractValue(xml, "//DataLayer/JobID") ?? ExtractValue(xml, "//JobID");
                string type = ExtractValue(xml, "//DataLayer/Type") ?? ExtractValue(xml, "//Type");
                string amrId = ExtractValue(xml, "//DataLayer/AmrId") ?? ExtractValue(xml, "//AmrId");

                if (string.IsNullOrEmpty(jobId))
                {
                    logger.Error("UpdateTransportCommandStateActivity: JobID is missing");
                    context.Set(Result, false);
                    return;
                }

                var tc = transferManager.GetTransportCommand(jobId);
                if (tc == null)
                {
                    logger.Warn($"UpdateTransportCommandStateActivity: TC not found - JobID={jobId}");
                    context.Set(Result, false);
                    return;
                }

                string previousState = tc.State;

                // Type에 따라 TC 상태 업데이트
                switch (type?.ToUpperInvariant())
                {
                    case "ARRIVED":
                        tc.State = TransportCommandEx.STATE_ARRIVED_SOURCE;
                        tc.LoadArrivedTime = DateTime.Now;
                        break;
                    case "COMPLETE":
                        tc.State = TransportCommandEx.STATE_COMPLETED;
                        tc.CompletedTime = DateTime.Now;
                        break;
                    case "CANCEL":
                        tc.State = TransportCommandEx.STATE_CANCELED;
                        break;
                    case "RECEIVE":
                        // RECEIVE는 상태 변경 없음
                        break;
                    default:
                        logger.Warn($"UpdateTransportCommandStateActivity: Unknown Type={type}, no state change");
                        break;
                }

                // AmrId가 있으면 VehicleId 업데이트
                if (!string.IsNullOrEmpty(amrId))
                    tc.VehicleId = amrId;

                transferManager.UpdateTransportCommand(tc);
                context.Set(Result, true);

                logger.Info($"UpdateTransportCommandStateActivity: TC updated - JobID={jobId}, State={previousState}→{tc.State}");
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateTransportCommandStateActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
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
    }
}
