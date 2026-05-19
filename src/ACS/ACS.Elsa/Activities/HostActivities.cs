using System;
using System.Collections.Generic;
using System.Xml;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Core.Base;
using ACS.Core.Cache;
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
                if ((isLoad || isUnload)
                    && string.IsNullOrWhiteSpace(sourceLoc)
                    && string.IsNullOrWhiteSpace(sourcePort))
                {
                    if (isLoad)
                    {
                        var resolved = ResolveZoneMatchedBuffer(accessor, destLoc, destPort, StationExs.TYPE_ACQUIRE);
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
                        logger.Info($"CreateTransportCommandActivity: LOAD source auto-resolved - SourceLoc={sourceLoc}, SourcePort={sourcePort}, Dest={destLoc}");
                    }
                    else // UNLOAD
                    {
                        // 1) 원래 dest(=EQP) 를 source 로 이동
                        sourceLoc = destLoc;
                        sourcePort = destPort;

                        // 2) 동일 zone 의 DEPOSIT BUFFER 로 dest 자동 해석
                        var resolved = ResolveZoneMatchedBuffer(accessor, sourceLoc, sourcePort, StationExs.TYPE_DEPOSITE);
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
                        logger.Info($"CreateTransportCommandActivity: UNLOAD dest auto-resolved - Source={sourceLoc}:{sourcePort}, DestLoc={destLoc}, DestPort={destPort}");
                    }
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

        // anchorLoc/anchorPort 의 zone 과 동일 zone 에 속한 BUFFER + 지정 stationType 후보의 LocationId 반환.
        //   LOAD : anchor=Dest(EQP),   stationType=ACQUIRE → source 후보
        //   UNLOAD: anchor=Source(EQP), stationType=DEPOSIT → dest 후보
        // 단계별 사유는 logger.Warn 으로만 출력 (MES NACK 는 호출부에서 단일 SOURCEMACHINENOTFOUND).
        // LocationId 오름차순 첫 번째 후보를 ':' 로 split 한 [0] 을 반환. 후보 없으면 null.
        private static string ResolveZoneMatchedBuffer(
            AutofacContainerAccessor accessor, string anchorLoc, string anchorPort, string stationType)
        {
            var tag = $"ResolveZoneMatchedBuffer({stationType})";

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

            // 2. BUFFER + (stationType) + 동일 zone 인 LocationEx 후보 수집
            var allLocations = resource.GetLocations();
            if (allLocations == null)
            {
                logger.Warn($"{tag}: GetLocations returned null");
                return null;
            }

            int bufferCount = 0, typeMatchCount = 0, zoneMatchCount = 0;
            var candidates = new List<string>();
            foreach (LocationEx loc in allLocations)
            {
                if (loc == null) continue;
                if (!string.Equals(loc.Type, "BUFFER", StringComparison.OrdinalIgnoreCase)) continue;
                bufferCount++;

                var st = cache.GetStationById(loc.StationId);
                if (st == null) continue;
                if (!string.Equals(st.Type, stationType, StringComparison.OrdinalIgnoreCase)) continue;
                typeMatchCount++;
                if (string.IsNullOrEmpty(st.LinkId)) continue;

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

            logger.Info($"{tag}: scan result - BUFFER={bufferCount}, {stationType}={typeMatchCount}, zoneMatch={zoneMatchCount}, candidates={candidates.Count}");

            if (candidates.Count == 0)
            {
                logger.Warn($"{tag}: no candidate in zone='{anchorZoneId}' (BUFFER={bufferCount},{stationType}={typeMatchCount},zoneMatch={zoneMatchCount})");
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
}
