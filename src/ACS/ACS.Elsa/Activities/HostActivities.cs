using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Host.Models;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Core.Base;
using ACS.Core.Cache;
using ACS.Communication.Msb;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
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

        /// <summary>리포트 타입: RECEIVE, START, CANCEL, ARRIVED, ACTION, COMPLETE</summary>
        [Input(Description = "리포트 타입 (RECEIVE, START, CANCEL, ARRIVED, ACTION, COMPLETE)")]
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
                    jobReport = hostMessageService.BuildJobReportFromMoveCmd(moveCmdXml, reportType, errCode, errMsg);

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
                        userId: UserId?.Get(context) ?? "",
                        errCode: errCode,
                        errMsg: errMsg);
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
    ///   - MODEL → TransportCommandEx.Description (포맷: MODEL='&lt;value&gt;')
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
                string model = ExtractValue(moveCmdXml, "//DataLayer/MODEL")
                            ?? ExtractValue(moveCmdXml, "//MODEL") ?? "";
                string descriptionValue = $"MODEL='{model}'";
                string acsId = ExtractValue(moveCmdXml, "//DataLayer/AcsId")
                            ?? ExtractValue(moveCmdXml, "//AcsId") ?? "";

                // MES.dest 의 station 타입이 ActionType 과 호환되는지 검증.
                //   LOAD  : 차량이 도착해서 'deposit' → station type ∈ {DEPOSIT, BOTH}
                //   UNLOAD: 차량이 도착해서 'acquire' → station type ∈ {ACQUIRE, BOTH}
                // station 타입이 일치하지 않으면 NACK. 데이터에 없는 location 은 후속 검증에서 따로 처리.
                {
                    bool isLoadCheck   = string.Equals(actionType, "LOAD",   StringComparison.OrdinalIgnoreCase);
                    bool isUnloadCheck = string.Equals(actionType, "UNLOAD", StringComparison.OrdinalIgnoreCase);
                    if ((isLoadCheck || isUnloadCheck) && !string.IsNullOrWhiteSpace(destLoc))
                    {
                        var cacheChk = accessor.Resolve<ICacheManagerEx>();
                        if (cacheChk != null)
                        {
                            var destKeyChk = string.IsNullOrEmpty(destPort) ? destLoc : $"{destLoc}:{destPort}";
                            var destLocChk = cacheChk.GetLocationByLocationId(destKeyChk);
                            var destStChk = destLocChk != null ? cacheChk.GetStationById(destLocChk.StationId) : null;
                            if (destStChk != null)
                            {
                                string expectedType = isLoadCheck ? StationExs.TYPE_DEPOSITE : StationExs.TYPE_ACQUIRE;
                                bool typeOk = string.Equals(destStChk.Type, expectedType, StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(destStChk.Type, StationExs.TYPE_BOTH, StringComparison.OrdinalIgnoreCase);
                                if (!typeOk)
                                {
                                    logger.Warn($"CreateTransportCommandActivity: {actionType} dest station type mismatch - Dest={destKeyChk}, Station={destStChk.Id}, expected={expectedType}/BOTH, actual={destStChk.Type}");
                                    context.Set(ErrCode, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1);
                                    context.Set(ErrMsg, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2);
                                    context.Set(Result, false);
                                    return;
                                }
                            }
                        }
                    }
                }

                // LOAD/UNLOAD 액션이고 SourceLoc + SourcePort 가 모두 비어 있으면 자동 해석.
                //   LOAD : DestLoc 의 zone 과 동일 zone 의 ACQUIRE BUFFER 를 source 로 채움. SourcePort="LEFT".
                //   UNLOAD: DestLoc/DestPort 를 source 로 옮기고, 동일 zone 의 DEPOSIT BUFFER 를 dest 로 채움.
                //           새 DestPort 는 "LEFT" 로 통일.
                bool isLoad   = string.Equals(actionType, "LOAD",   StringComparison.OrdinalIgnoreCase);
                bool isUnload = string.Equals(actionType, "UNLOAD", StringComparison.OrdinalIgnoreCase);
                bool autoResolved = false;
                if ((isLoad || isUnload)
                    && string.IsNullOrWhiteSpace(sourceLoc)
                    && string.IsNullOrWhiteSpace(sourcePort))
                {
                    if (isLoad)
                    {
                        var resolved = ResolveZoneMatchedBuffer(accessor, destLoc, destPort, LocationExs.LOCATION_TYPE_OUTPUT);
                        if (resolved == null)
                        {
                            logger.Warn($"CreateTransportCommandActivity: LOAD source auto-resolve failed - Dest={destLoc}");
                            context.Set(ErrCode, AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item1);
                            context.Set(ErrMsg, AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item2);
                            context.Set(Result, false);
                            return;
                        }
                        sourceLoc = resolved;
                        sourcePort = "LEFT";
                        autoResolved = true;
                        logger.Info($"CreateTransportCommandActivity: LOAD source auto-resolved - SourceLoc={sourceLoc}, SourcePort={sourcePort}, Dest={destLoc}");
                    }
                    else // UNLOAD
                    {
                        // 1) 원래 dest(=EQP) 를 source 로 이동
                        sourceLoc = destLoc;
                        sourcePort = destPort;

                        // 2) 동일 zone 의 INPUT Location 으로 dest 자동 해석
                        var resolved = ResolveZoneMatchedBuffer(accessor, sourceLoc, sourcePort, LocationExs.LOCATION_TYPE_INPUT);
                        if (resolved == null)
                        {
                            logger.Warn($"CreateTransportCommandActivity: UNLOAD dest auto-resolve failed - Source={sourceLoc}:{sourcePort}");
                            // dest 측 NOTFOUND 로 응답
                            context.Set(ErrCode, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1);
                            context.Set(ErrMsg, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2);
                            context.Set(Result, false);
                            return;
                        }
                        destLoc = resolved;
                        destPort = "LEFT";
                        autoResolved = true;
                        logger.Info($"CreateTransportCommandActivity: UNLOAD dest auto-resolved - Source={sourceLoc}:{sourcePort}, DestLoc={destLoc}, DestPort={destPort}");
                    }
                }

                // 새 사양: SourceLoc/Port=BUFFER 고정, DestLoc/Port=EQP 고정.
                // UNLOAD 는 물리 반송 방향이 EQP→BUFFER 이므로 source/dest 를 교환한다.
                // (레거시 빈-Source 자동해석이 이미 물리 방향으로 배치한 경우엔 이중 교환 방지를 위해 생략)
                if (isUnload && !autoResolved)
                {
                    (sourceLoc, destLoc)   = (destLoc, sourceLoc);
                    (sourcePort, destPort) = (destPort, sourcePort);
                    logger.Info($"CreateTransportCommandActivity: UNLOAD swap - Source={sourceLoc}:{sourcePort}, Dest={destLoc}:{destPort}");
                }

                // SourceLoc/DestLoc 만 있고 대응 포트가 비어있으면 NA_R_LOCATION 의 '<Loc>:<Port>' 키에서 첫 후보 포트로 보완.
                // MES 가 SourcePort 를 누락한 LOAD/UNLOAD 요청을 ACS 가 방어적으로 살리기 위함.
                // 후보 없으면 보완하지 않음 → 기존 location 미조회 분기에서 자연스럽게 NACK.
                if (!string.IsNullOrWhiteSpace(sourceLoc) && string.IsNullOrWhiteSpace(sourcePort))
                {
                    var resolvedPort = ResolveMissingPortByLocPrefix(accessor, sourceLoc);
                    if (!string.IsNullOrEmpty(resolvedPort))
                    {
                        sourcePort = resolvedPort;
                        logger.Info($"CreateTransportCommandActivity: SourcePort auto-filled - SourceLoc={sourceLoc}, SourcePort={sourcePort}");
                    }
                }
                if (!string.IsNullOrWhiteSpace(destLoc) && string.IsNullOrWhiteSpace(destPort))
                {
                    var resolvedPort = ResolveMissingPortByLocPrefix(accessor, destLoc);
                    if (!string.IsNullOrEmpty(resolvedPort))
                    {
                        destPort = resolvedPort;
                        logger.Info($"CreateTransportCommandActivity: DestPort auto-filled - DestLoc={destLoc}, DestPort={destPort}");
                    }
                }

                // LOAD 인데 DestPort 가 끝까지 비어 있으면 'LEFT' 로 강제 세팅.
                // MES 가 LOAD 의 DestPort 를 누락하고 캐시 후보로도 보충되지 않을 때 NACK 대신 EQP 표준 포트명 'LEFT' 로 진행.
                if (isLoad && !string.IsNullOrWhiteSpace(destLoc) && string.IsNullOrWhiteSpace(destPort))
                {
                    destPort = "LEFT";
                    logger.Info($"CreateTransportCommandActivity: LOAD DestPort defaulted to LEFT - DestLoc={destLoc}");
                }

                // Source, Dest 조합: "SourceLoc:SourcePort" 형식
                string source = string.IsNullOrEmpty(sourcePort) ? sourceLoc : $"{sourceLoc}:{sourcePort}";
                string dest = string.IsNullOrEmpty(destPort) ? destLoc : $"{destLoc}:{destPort}";

                // Source == Dest 차단 (자동해석 결과가 anchor 와 동일해지는 경우 등 방지)
                if (!string.IsNullOrEmpty(source) && string.Equals(source, dest, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"CreateTransportCommandActivity: source and dest are identical - {source}");
                    context.Set(ErrCode, AbstractManager.ID_RESULT_SOURCEDESTMACHINE_DUPLICATE.Item1);
                    context.Set(ErrMsg, AbstractManager.ID_RESULT_SOURCEDESTMACHINE_DUPLICATE.Item2);
                    context.Set(Result, false);
                    return;
                }

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

                // 동일 SourceLoc/DestLoc 의 기존 비-종료 TC 가 있으면 정책에 따라 처리.
                //   - 진행중(ASSIGNED/TRANSFERRING_SOURCE/TRANSFERRING_DEST): 기존 TC 의 MES 입력 필드 갱신
                //     + 차량의 TransportCommandId 도 신규 jobId 로 동기화. 새 TC 는 만들지 않고 성공 응답.
                //   - 그 외 비-종료: 기존 TC 삭제 후 아래의 생성 로직 진행.
                {
                    var inProgressStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        TransportCommandEx.STATE_ASSIGNED,
                        TransportCommandEx.STATE_TRANSFERRING_SOURCE,
                        TransportCommandEx.STATE_TRANSFERRING_DEST
                    };

                    var matched = transferManager.FindActiveTransportCommandByLocationMatch(sourceLoc, destLoc);
                    if (matched != null)
                    {
                        if (inProgressStates.Contains(matched.State))
                        {
                            var oldJobId   = matched.JobId;
                            var oldVehicle = matched.VehicleId;
                            matched.JobId       = jobId;
                            matched.Source      = source;
                            matched.Dest        = dest;
                            matched.JobType     = actionType;
                            matched.Description = descriptionValue;
                            matched.EqpId       = acsId;
                            matched.BayId       = sameBayId;
                            transferManager.UpdateTransportCommand(matched);

                            if (!string.IsNullOrWhiteSpace(oldVehicle))
                            {
                                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                                if (resourceManager != null)
                                {
                                    int upd = resourceManager.UpdateVehicleTransportCommandId(oldVehicle, jobId);
                                    logger.Info($"CreateTransportCommandActivity: vehicle TC ptr updated - VehicleId={oldVehicle}, oldJobId={oldJobId}, newJobId={jobId}, rows={upd}");
                                }
                                else
                                {
                                    logger.Warn($"CreateTransportCommandActivity: IResourceManagerEx not available - vehicle {oldVehicle} TransportCommandId not synced");
                                }
                            }

                            logger.Info($"CreateTransportCommandActivity: matched in-progress TC updated - oldJobId={oldJobId}, newJobId={jobId}, State={matched.State}, VehicleId={oldVehicle}, Source={source}, Dest={dest}");

                            context.Set(TransportCommandId, jobId);
                            context.Set(ErrCode, "0");
                            context.Set(ErrMsg, "");
                            context.Set(Result, true);
                            return;
                        }
                        else
                        {
                            logger.Info($"CreateTransportCommandActivity: matched not-started TC deleted - jobId={matched.JobId}, State={matched.State}, oldSource={matched.Source}, oldDest={matched.Dest}");
                            transferManager.DeleteTransportCommand(matched.JobId);
                        }
                    }
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
                    Description = descriptionValue,
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

        // anchorLoc/anchorPort 의 zone 과 동일 zone 에 속한 LocationEx 중 Location.Type 이 지정값인 후보의 LocationId 반환.
        //   LOAD : anchor=Dest(EQP),   locationType=OUTPUT → source 후보
        //   UNLOAD: anchor=Source(EQP), locationType=INPUT  → dest 후보
        // 단계별 사유는 logger.Warn 으로만 출력 (MES NACK 는 호출부에서 단일 SOURCEMACHINENOTFOUND).
        // LocationId 오름차순 첫 번째 후보를 ':' 로 split 한 [0] 을 반환. 후보 없으면 null.
        private static string ResolveZoneMatchedBuffer(
            AutofacContainerAccessor accessor, string anchorLoc, string anchorPort, string locationType)
        {
            var tag = $"ResolveZoneMatchedBuffer({locationType})";

            if (string.IsNullOrWhiteSpace(anchorLoc))
            {
                logger.Warn($"{tag}: anchorLoc empty");
                return null;
            }

            var cache = accessor.Resolve<ICacheManagerEx>();
            var resource = accessor.Resolve<IResourceManagerEx>();
            if (cache == null || resource == null)
            {
                logger.Warn($"{tag}: DI resolve failed (cache={(cache != null)},resource={(resource != null)})");
                return null;
            }

            // 1. anchorLoc(+anchorPort) → Station → LinkZone → ZoneId (기준 zone)
            var anchorKey = string.IsNullOrEmpty(anchorPort) ? anchorLoc : $"{anchorLoc}:{anchorPort}";
            var anchorLocation = cache.GetLocationByLocationId(anchorKey);
            if (anchorLocation == null)
            {
                logger.Warn($"{tag}: NA_R_LOCATION miss '{anchorKey}'");
                return null;
            }
            logger.Info($"{tag}: anchorLocation={anchorLocation.LocationId}, StationId={anchorLocation.StationId}, Type={anchorLocation.Type}");

            var anchorStation = cache.GetStationById(anchorLocation.StationId);
            if (anchorStation == null)
            {
                logger.Warn($"{tag}: NA_R_STATION miss '{anchorLocation.StationId}'");
                return null;
            }
            if (string.IsNullOrEmpty(anchorStation.LinkId))
            {
                logger.Warn($"{tag}: Station '{anchorLocation.StationId}' has no LinkId");
                return null;
            }
            logger.Info($"{tag}: anchorStation={anchorStation.Id}, Type={anchorStation.Type}, LinkId={anchorStation.LinkId}");

            var anchorLinkZones = resource.GetLinkZonesByLinkId(anchorStation.LinkId);
            if (anchorLinkZones == null || anchorLinkZones.Count == 0)
            {
                logger.Warn($"{tag}: NA_R_LINK_ZONE miss for LinkId='{anchorStation.LinkId}'");
                return null;
            }
            var anchorZoneId = ((LinkZoneEx)anchorLinkZones[0]).ZoneId;
            if (string.IsNullOrEmpty(anchorZoneId))
            {
                logger.Warn($"{tag}: LinkZone has empty ZoneId (LinkId='{anchorStation.LinkId}')");
                return null;
            }
            logger.Info($"{tag}: anchorZoneId={anchorZoneId} (from LinkId={anchorStation.LinkId})");

            // 2. Location.Type==locationType + 동일 zone 인 LocationEx 후보 수집
            var allLocations = resource.GetLocations();
            if (allLocations == null)
            {
                logger.Warn($"{tag}: GetLocations returned null");
                return null;
            }

            int typeMatchCount = 0, zoneMatchCount = 0;
            var candidates = new List<string>();
            foreach (LocationEx loc in allLocations)
            {
                if (loc == null) continue;
                if (!string.Equals(loc.Type, locationType, StringComparison.OrdinalIgnoreCase)) continue;
                typeMatchCount++;

                var st = cache.GetStationById(loc.StationId);
                if (st == null || string.IsNullOrEmpty(st.LinkId)) continue;

                var lzList = resource.GetLinkZonesByLinkId(st.LinkId);
                if (lzList == null) continue;

                bool zoneMatch = false;
                foreach (LinkZoneEx lz in lzList)
                {
                    if (lz != null && string.Equals(lz.ZoneId, anchorZoneId, StringComparison.OrdinalIgnoreCase))
                    {
                        zoneMatch = true;
                        break;
                    }
                }
                if (zoneMatch)
                {
                    zoneMatchCount++;
                    if (!string.IsNullOrEmpty(loc.LocationId))
                        candidates.Add(loc.LocationId);
                }
            }

            logger.Info($"{tag}: scan result - {locationType}={typeMatchCount}, zoneMatch={zoneMatchCount}, candidates={candidates.Count}");

            if (candidates.Count == 0)
            {
                logger.Warn($"{tag}: no candidate in zone='{anchorZoneId}' ({locationType}={typeMatchCount},zoneMatch={zoneMatchCount})");
                return null;
            }

            // 3. LocationId 오름차순 첫 번째, ':' 로 split 한 [0]
            candidates.Sort(StringComparer.OrdinalIgnoreCase);
            var first = candidates[0];
            var parts = first.Split(':');
            var result = parts.Length > 0 ? parts[0] : first;
            logger.Info($"{tag}: chosen first candidate '{first}' → '{result}'");
            return result;
        }

        // LocationId 가 '<loc>:<port>' 포맷인 후보 중 사전순 첫 행의 port 부분을 반환.
        // 후보 없거나 단독 키만 있는 경우 null. SourcePort/DestPort 누락 시 fallback 용.
        private static string ResolveMissingPortByLocPrefix(
            AutofacContainerAccessor accessor, string loc)
        {
            if (string.IsNullOrWhiteSpace(loc)) return null;

            var resource = accessor.Resolve<IResourceManagerEx>();
            if (resource == null)
            {
                logger.Warn($"ResolveMissingPortByLocPrefix: IResourceManagerEx not available - loc={loc}");
                return null;
            }

            var allLocations = resource.GetLocations();
            if (allLocations == null) return null;

            var prefix = loc + ":";
            var candidates = new List<string>();
            foreach (LocationEx l in allLocations)
            {
                if (l == null || string.IsNullOrEmpty(l.LocationId)) continue;
                if (l.LocationId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    candidates.Add(l.LocationId);
            }
            if (candidates.Count == 0)
            {
                logger.Info($"ResolveMissingPortByLocPrefix: no '<loc>:<port>' candidate for loc='{loc}'");
                return null;
            }

            candidates.Sort(StringComparer.OrdinalIgnoreCase);
            var firstId = candidates[0];
            var idx = firstId.IndexOf(':');
            var port = idx >= 0 && idx + 1 < firstId.Length ? firstId.Substring(idx + 1) : null;
            logger.Info($"ResolveMissingPortByLocPrefix: chosen '{firstId}' → port='{port}' (candidates={candidates.Count})");
            return port;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  JOBREPORT 수신 → 검증 → MES 전달 Activities
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 워크플로우 Input(Arguments)에서 JOBREPORT JSON 을 파싱해 JobReportData 로 추출하는 Activity.
    /// 입력 형식: {"header":{"messageName":"JOBREPORT","transactionId":"..."}, "data":{...}}
    /// </summary>
    [Activity("ACS.Host", "Extract JobReport JSON",
        "워크플로우 입력에서 JOBREPORT JSON 을 파싱하여 JobReportData 로 추출합니다.")]
    public class ExtractJobReportFromInput : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "추출된 JOBREPORT 데이터")]
        public Output<JobReportData> OutputData { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            string json = null;
            var input = context.WorkflowExecutionContext.Input;
            if (input != null && input.TryGetValue("Arguments", out var args))
            {
                if (args is object[] argsArray && argsArray.Length > 0)
                    json = argsArray[0] as string;
                else if (args is string s)
                    json = s;
            }

            var data = new JobReportData();
            if (string.IsNullOrEmpty(json))
            {
                logger.Warn("ExtractJobReportFromInput: No JOBREPORT JSON found in input, returning empty data");
                context.Set(OutputData, data);
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
                {
                    data.AcsId = ReadString(d, "AcsId");
                    data.Type = ReadString(d, "Type");
                    data.AmrId = ReadString(d, "AmrId");
                    data.ActionType = ReadString(d, "ActionType");
                    data.JobID = ReadString(d, "JobID");
                    data.MaterialType = ReadString(d, "MaterialType");
                    data.UserID = ReadString(d, "UserID");
                    data.ErrorCode = ReadString(d, "ErrorCode");
                    data.ErrorMsg = ReadString(d, "ErrorMsg");
                }

                // header.routedFrom — 비-Host 프로세스 fallback 재발행 시 루프 검출용
                if (root.TryGetProperty("header", out var h) && h.ValueKind == JsonValueKind.Object)
                {
                    data.RoutedFrom = ReadString(h, "routedFrom");
                }

                context.Set(OutputData, data);
                logger.Info($"ExtractJobReportFromInput: JOBREPORT parsed - JobID={data.JobID}, Type={data.Type}, AmrId={data.AmrId}");
            }
            catch (Exception ex)
            {
                logger.Error($"ExtractJobReportFromInput: JSON parse 실패 - {ex.Message}", ex);
                context.Set(OutputData, data);
            }
        }

        private static string ReadString(JsonElement obj, string name)
        {
            if (obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? "";
            return "";
        }
    }

    /// <summary>
    /// JOBREPORT 메시지를 DB의 TransportCommandEx와 대조 검증하는 Activity.
    ///
    /// 검증 항목:
    ///   1. JobID로 TransportCommandEx 존재 여부 확인
    ///   2. JOBREPORT Type vs TC State 정합성 (이미 완료/취소된 건에 대한 중복 보고 차단)
    ///   3. 데이터 일치 확인 (MODEL↔Description, ActionType↔JobType) — 불일치 시 경고 로그
    /// </summary>
    [Activity("ACS.Host", "Validate Job Report",
        "JOBREPORT 데이터를 DB의 TransportCommandEx와 대조 검증합니다.")]
    public class ValidateJobReportActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "JOBREPORT 데이터")]
        public Input<JobReportData> JobReportData { get; set; }

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

                var data = JobReportData?.Get(context);
                if (data == null)
                {
                    logger.Error("ValidateJobReportActivity: JobReportData is required");
                    context.Set(Result, false);
                    context.Set(ValidationError, "JobReportData is null");
                    return;
                }

                string jobId = data.JobID;
                string type = data.Type;
                string model = data.MODEL;
                string actionType = data.ActionType;
                string amrId = data.AmrId;

                if (string.IsNullOrEmpty(jobId))
                {
                    logger.Error("ValidateJobReportActivity: JobID is missing from JOBREPORT");
                    context.Set(Result, false);
                    context.Set(ValidationError, "JobID is missing from JOBREPORT");
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
                // TC.Description 은 "MODEL='<value>'" 포맷. MES 가 JOBREPORT.MODEL 을 보낸 경우에만 비교한다.
                if (!string.IsNullOrEmpty(model))
                {
                    string tcModel = tc.GetModel();
                    if (!string.IsNullOrEmpty(tcModel) &&
                        !string.Equals(model, tcModel, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Warn($"ValidateJobReportActivity: MODEL mismatch - Message={model}, TC.Model={tcModel}");
                    }
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
    }

    /// <summary>
    /// 검증된 JOBREPORT 를 MES TCP 로 전달하는 Activity.
    /// JSON 데이터로부터 IHostMessageService.BuildJobReport 로 MES 용 XML 을 구성한 뒤
    /// IHostMessageService.SendToHost 로 TCP 송신.
    /// </summary>
    [Activity("ACS.Host", "Forward Job Report to MES",
        "검증된 JOBREPORT 를 MES 로 TCP 전달합니다 (JSON → XML 변환 후 송신).")]
    public class ForwardJobReportToMesActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "전달할 JOBREPORT 데이터")]
        public Input<JobReportData> JobReportData { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var data = JobReportData?.Get(context);
                if (data == null)
                {
                    logger.Error("ForwardJobReportToMesActivity: JobReportData is null");
                    context.Set(Result, false);
                    return;
                }

                var hostMessageService = accessor?.ResolveOptional<IHostMessageService>();
                if (hostMessageService != null)
                {
                    // === Host 프로세스: 정상 경로 — MES TCP 송신 ===
                    var xml = hostMessageService.BuildJobReport(
                        reportType: data.Type ?? "",
                        jobId: data.JobID ?? "",
                        amrId: data.AmrId ?? "",
                        actionType: data.ActionType ?? "",
                        materialType: data.MaterialType ?? "",
                        acsId: data.AcsId ?? "",
                        userId: data.UserID ?? "",
                        errCode: data.ErrorCode ?? "",
                        errMsg: data.ErrorMsg ?? "");

                    hostMessageService.SendToHost("JOBREPORT", xml);
                    context.Set(Result, true);
                    logger.Info($"ForwardJobReportToMesActivity: JOBREPORT forwarded to MES - JobID={data.JobID}, Type={data.Type}");
                    return;
                }

                // === 비-Host 프로세스 (예: Trans): 라우팅 누수로 잘못 도달한 메시지를 ===
                // === host 큐로 재발행한다. 루프 방지를 위해 header.routedFrom 이 자기   ===
                // === 자신과 같으면 (=이미 한 번 재발행한 메시지) 더 재발행하지 않는다.  ===
                string currentProcess = "";
                try { currentProcess = accessor?.Resolve<IConfiguration>()?["Acs:Process:Name"] ?? ""; }
                catch { /* IConfiguration 미등록 — currentProcess 빈 문자열 유지 */ }

                bool alreadyReRouted =
                    !string.IsNullOrEmpty(data.RoutedFrom) &&
                    !string.IsNullOrEmpty(currentProcess) &&
                    data.RoutedFrom.Equals(currentProcess, StringComparison.OrdinalIgnoreCase);

                IMessageAgent hostAgent = null;
                try { hostAgent = accessor?.ResolveNamed<IMessageAgent>("HostAgentSender"); }
                catch { /* 미등록 — 아래에서 null 처리 */ }

                if (hostAgent == null || alreadyReRouted)
                {
                    logger.Error($"ForwardJobReportToMesActivity: IHostMessageService 미등록 — 재발행 불가. " +
                                 $"hostAgent={hostAgent != null}, alreadyReRouted={alreadyReRouted}, " +
                                 $"currentProcess={currentProcess}, routedFrom={data.RoutedFrom}, " +
                                 $"JobID={data.JobID}, Type={data.Type}. 메시지 유실 — 라우팅 누수 (Trans 가 host 큐 메시지를 가로채는 빈-destination 리스너) 점검 필요.");
                    context.Set(Result, false);
                    return;
                }

                // host 큐로 JSON 재발행 — routedFrom 을 현재 프로세스명으로 갱신해 다음 hop 에서 루프 차단.
                string rerouted = JsonSerializer.Serialize(new
                {
                    header = new
                    {
                        messageName = "JOBREPORT",
                        transactionId = Guid.NewGuid().ToString("N"),
                        destSubject = "",
                        replySubject = "",
                        routedFrom = currentProcess
                    },
                    data = new
                    {
                        AcsId = data.AcsId ?? "",
                        Type = data.Type ?? "",
                        AmrId = data.AmrId ?? "",
                        ActionType = data.ActionType ?? "",
                        JobID = data.JobID ?? "",
                        MaterialType = data.MaterialType ?? "",
                        UserID = data.UserID ?? "",
                        ErrorCode = data.ErrorCode ?? "",
                        ErrorMsg = data.ErrorMsg ?? ""
                    }
                });
                hostAgent.Send((object)rerouted);
                context.Set(Result, true);
                logger.Warn($"ForwardJobReportToMesActivity: 비-Host 프로세스({currentProcess})에서 실행됨 — JOBREPORT 를 host 큐로 재발행. " +
                            $"JobID={data.JobID}, Type={data.Type}. 라우팅 누수 점검 필요.");
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

        [Input(Description = "JOBREPORT 데이터")]
        public Input<JobReportData> JobReportData { get; set; }

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

                var data = JobReportData?.Get(context);
                if (data == null)
                {
                    logger.Error("UpdateTransportCommandStateActivity: JobReportData is null");
                    context.Set(Result, false);
                    return;
                }

                string jobId = data.JobID;
                string type = data.Type;
                string amrId = data.AmrId;

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
                    case "RECEIVE":
                        // RECEIVE는 상태 변경 없음
                        break;
                    case "START":
                        tc.State = TransportCommandEx.STATE_ASSIGNED;
                        tc.StartedTime = DateTime.Now;
                        break;
                    case "ARRIVED":
                        tc.State = TransportCommandEx.STATE_ARRIVED_SOURCE;
                        tc.LoadArrivedTime = DateTime.Now;
                        break;
                    case "ACTION":
                        tc.State = TransportCommandEx.STATE_TRANSFERRING_SOURCE;
                        break;
                    case "COMPLETE":
                        tc.State = TransportCommandEx.STATE_COMPLETED;
                        tc.CompletedTime = DateTime.Now;
                        break;
                    case "CANCEL":
                        tc.State = TransportCommandEx.STATE_CANCELED;
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
    }

    // ═══════════════════════════════════════════════════════════════
    //  MOVECANCEL → TransportCommand 취소 Activity
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// MOVECANCEL XML에서 JobId를 추출하여 해당 TransportCommand를 취소하는 Activity.
    ///
    /// 처리 흐름:
    ///   1. MOVECANCEL XML에서 JobID 추출
    ///   2. DB에서 TransportCommand 조회
    ///   3. 취소 가능 상태이면 STATE_CANCELED로 변경
    ///   4. 결과를 ErrorCode/ErrorMsg로 출력
    /// </summary>
    [Activity("ACS.Host", "Cancel Transport Command",
        "MOVECANCEL에서 TransportCommand를 취소합니다.")]
    public class CancelTransportCommandActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        /// <summary>수신한 MOVECANCEL XML</summary>
        [Input(Description = "수신한 MOVECANCEL XmlDocument")]
        public Input<XmlDocument> MoveCancelXml { get; set; }

        /// <summary>취소된 TransportCommand의 JobId</summary>
        [Output(Description = "취소된 TransportCommand JobId")]
        public Output<string> JobId { get; set; }

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
                    logger.Error("CancelTransportCommandActivity: ITransferManagerEx not available");
                    context.Set(ErrCode, "03");
                    context.Set(ErrMsg, "ITransferManagerEx not available");
                    context.Set(Result, false);
                    return;
                }

                var xml = MoveCancelXml?.Get(context);
                if (xml == null)
                {
                    logger.Error("CancelTransportCommandActivity: MoveCancelXml is required");
                    context.Set(ErrCode, "03");
                    context.Set(ErrMsg, "MoveCancelXml is required");
                    context.Set(Result, false);
                    return;
                }

                // MOVECANCEL XML에서 JobID 추출
                string jobId = ExtractValue(xml, "//DataLayer/JobID")
                            ?? ExtractValue(xml, "//DataLayer/JobId")
                            ?? ExtractValue(xml, "//JobID");

                if (string.IsNullOrEmpty(jobId))
                {
                    logger.Error("CancelTransportCommandActivity: JobID is missing from MOVECANCEL XML");
                    context.Set(ErrCode, "03");
                    context.Set(ErrMsg, "JobID is missing");
                    context.Set(Result, false);
                    return;
                }

                context.Set(JobId, jobId);

                // DB에서 TransportCommand 조회
                var tc = transferManager.GetTransportCommand(jobId);
                if (tc == null)
                {
                    logger.Warn($"CancelTransportCommandActivity: TransportCommand not found - JobID={jobId}");
                    context.Set(ErrCode, "01");
                    context.Set(ErrMsg, $"TransportCommand not found: JobID={jobId}");
                    context.Set(Result, false);
                    return;
                }

                // 이미 종료 상태인지 확인
                string stateUpper = tc.State?.ToUpperInvariant() ?? "";
                if (stateUpper == TransportCommandEx.STATE_COMPLETED.ToUpperInvariant() ||
                    stateUpper == TransportCommandEx.STATE_CANCELED.ToUpperInvariant() ||
                    stateUpper == TransportCommandEx.STATE_ABORTED.ToUpperInvariant())
                {
                    logger.Warn($"CancelTransportCommandActivity: TC already in terminal state - JobID={jobId}, State={tc.State}");
                    context.Set(ErrCode, "02");
                    context.Set(ErrMsg, $"TC already in terminal state: {tc.State}");
                    context.Set(Result, false);
                    return;
                }

                // 취소 처리
                string previousState = tc.State;
                tc.State = TransportCommandEx.STATE_CANCELED;
                transferManager.UpdateTransportCommand(tc);

                context.Set(ErrCode, "0");
                context.Set(ErrMsg, "");
                context.Set(Result, true);

                logger.Info($"CancelTransportCommandActivity: TC canceled - JobID={jobId}, State={previousState}→{tc.State}");
            }
            catch (Exception ex)
            {
                logger.Error($"CancelTransportCommandActivity: {ex.Message}", ex);
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

    /// <summary>
    /// ACTIONCMD XML을 JSON으로 변환하여 RabbitMQ(HostAgentSender)를 통해 Trans 프로세스로 전송.
    /// </summary>
    [Activity("ACS.Host", "Send ActionCmd JSON to Trans",
        "ACTIONCMD XML → JSON 변환 후 RabbitMQ로 Trans에 전송")]
    public class SendActionCmdJsonToTransActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "수신한 ACTIONCMD XmlDocument")]
        public Input<XmlDocument> ActionCmdXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var actionCmdXml = ActionCmdXml?.Get(context);
                if (actionCmdXml == null)
                {
                    logger.Error("SendActionCmdJsonToTransActivity: ActionCmdXml is null");
                    context.Set(Result, false);
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("SendActionCmdJsonToTransActivity: AutofacContainerAccessor not available");
                    context.Set(Result, false);
                    return;
                }

                var hostAgentSender = accessor.ResolveNamed<ACS.Communication.Msb.IMessageAgent>("HostAgentSender");
                if (hostAgentSender == null)
                {
                    logger.Error("SendActionCmdJsonToTransActivity: HostAgentSender not available");
                    context.Set(Result, false);
                    return;
                }

                string acsId = ExtractValue(actionCmdXml, "//DataLayer/AcsId") ?? ExtractValue(actionCmdXml, "//AcsId") ?? "";
                string targetLoc = ExtractValue(actionCmdXml, "//DataLayer/TargetLoc") ?? ExtractValue(actionCmdXml, "//TargetLoc") ?? "";
                string targetPort = ExtractValue(actionCmdXml, "//DataLayer/TargetPort") ?? ExtractValue(actionCmdXml, "//TargetPort") ?? "";
                string jobId = ExtractValue(actionCmdXml, "//DataLayer/JobID") ?? ExtractValue(actionCmdXml, "//JobID") ?? "";
                string materialType = ExtractValue(actionCmdXml, "//DataLayer/MaterialType") ?? ExtractValue(actionCmdXml, "//MaterialType") ?? "";
                string model = ExtractValue(actionCmdXml, "//DataLayer/MODEL") ?? ExtractValue(actionCmdXml, "//MODEL") ?? "";
                string actionType = ExtractValue(actionCmdXml, "//DataLayer/ActionType") ?? ExtractValue(actionCmdXml, "//ActionType") ?? "";
                string userId = ExtractValue(actionCmdXml, "//DataLayer/UserID") ?? ExtractValue(actionCmdXml, "//UserID") ?? "";

                var message = new ACS.Communication.Mqtt.Model.ActionCmdMessage
                {
                    Header = new ACS.Communication.Mqtt.Model.ActionCmdHeader
                    {
                        MessageName = "TRANS-ACTIONCMD",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "Host"
                    },
                    Data = new ACS.Communication.Mqtt.Model.ActionCmdData
                    {
                        AcsId = acsId,
                        TargetLoc = targetLoc,
                        TargetPort = targetPort,
                        JobId = jobId,
                        MaterialType = materialType,
                        Model = model,
                        ActionType = actionType,
                        UserId = userId
                    }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(message);
                hostAgentSender.Send((object)json);

                logger.Info($"SendActionCmdJsonToTransActivity: sent ACTIONCMD JSON to Trans - jobId={jobId}, targetLoc={targetLoc}, actionType={actionType}, model={model}");
                context.Set(Result, true);
            }
            catch (Exception ex)
            {
                logger.Error($"SendActionCmdJsonToTransActivity: {ex.Message}", ex);
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
