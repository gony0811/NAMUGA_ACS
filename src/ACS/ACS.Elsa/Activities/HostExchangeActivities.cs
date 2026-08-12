using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Msb;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Core.Base;
using ACS.Core.Path;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  Host EXCHANGE Activities (EXCHANGE v2 — S3 슬라이스)
    //  Category: ACS.Host
    //
    //  MES EXCHANGECMD 수신 경로: 파싱(ExchangeCmdModel) → 검증 →
    //  1-TC 3-waypoint(Origin→Mid→Dest) 생성(EXCHANGE_QUEUED) →
    //  JOBREPORT(RECEIVE, Step=10) 회신.
    //  기존 MOVECMD 경로(HostActivities.cs)는 무수정 — 병렬 신규 경로 (D4).
    //  참조: ACS_EXCHANGE_구현사양서.md §4.1~4.5
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 워크플로우 Input(Arguments)에서 EXCHANGECMD XmlDocument 추출.
    /// ExtractMoveCmdFromInput 과 동일 골격 (EXCHANGE 전용 fallback XML).
    /// </summary>
    [Activity("ACS.Host", "Extract ExchangeCmd XML",
        "워크플로우 입력에서 EXCHANGECMD XmlDocument를 추출합니다.")]
    public class ExtractExchangeCmdFromInput : CodeActivity
    {
        [Output(Description = "추출된 EXCHANGECMD XmlDocument")]
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
                result.LoadXml("<Msg><Command>EXCHANGECMD</Command><Header/><DataLayer/></Msg>");
            }

            context.Set(OutputXml, result);
        }
    }

    /// <summary>
    /// EXCHANGECMD 파싱 + 검증 + 1-TC 3-waypoint 생성 (구현사양서 §4.1~4.3).
    ///
    /// 검증 순서 (실패 시 ErrCode/ErrMsg 설정, TC 미생성 — NACK 은 후속 SendExchangeJobReportActivity 가 회신):
    ///   1. ActionType == EXCHANGE (라우팅 방어)          → 39 CANNOTEXECUTE
    ///   2. JobID 비어있지 않음                           → 107 TRANSPORTCOMMANDIDISEMPTY
    ///   3. JobID 중복                                    → 102 COMMANDALREADYREQUESTED
    ///   4. LoadCarrierSlot∈{1,2}·UnloadCarrierSlot∈{3,4} 또는 공백(자동배정, D10) → 106 INVALIDCARRIERSLOT
    ///   5. LoadSourceLoc Location 존재                   → 25 SOURCEMACHINENOTFOUND
    ///   6. EquipID:Port Location 존재                    → 21 DESTMACHINENOTFOUND
    ///   7. UnloadDestLoc Location 존재                   → 21 DESTMACHINENOTFOUND
    ///   8. Origin/Mid/Dest 위치 중복                     → 106 SOURCEDESTMACHINEDUPLICATE
    ///   9. 세 위치 공통 Bay                              → 22 NOTSAMEBAY
    /// </summary>
    [Activity("ACS.Host", "Create Exchange TransportCommand",
        "EXCHANGECMD를 검증하고 1-TC 3-waypoint TransportCommand(EXCHANGE_QUEUED)를 생성합니다.")]
    public class CreateExchangeTransportCommandActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "수신한 EXCHANGECMD XmlDocument")]
        public Input<XmlDocument> ExchangeCmdXml { get; set; }

        [Output(Description = "생성된 TransportCommand JobID")]
        public Output<string> TransportCommandId { get; set; }

        [Output(Description = "에러 코드 (성공 시 '0')")]
        public Output<string> ErrCode { get; set; }

        [Output(Description = "에러 메시지 (성공 시 빈 문자열)")]
        public Output<string> ErrMsg { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var pathManager = accessor?.Resolve<IPathManagerEx>();

                if (transferManager == null || pathManager == null)
                {
                    Fail(context, "03", "ITransferManagerEx/IPathManagerEx not available");
                    return;
                }

                var xml = ExchangeCmdXml?.Get(context);
                if (xml == null)
                {
                    Fail(context, "03", "ExchangeCmdXml is required");
                    return;
                }

                var cmd = ExchangeCmdModel.Parse(xml);
                context.Set(TransportCommandId, cmd.JobId);

                // 1. ActionType 방어
                if (!string.Equals(cmd.ActionType, "EXCHANGE", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: ActionType mismatch - ActionType={cmd.ActionType}, job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_CANNOTEXECUTE.Item1,
                        AbstractManager.ID_RESULT_CANNOTEXECUTE.Item2);
                    return;
                }

                // 2. JobID 필수
                if (string.IsNullOrWhiteSpace(cmd.JobId))
                {
                    Fail(context, AbstractManager.ID_RESULT_TRANSPORTCOMMAND_IDEMPTY.Item1,
                        AbstractManager.ID_RESULT_TRANSPORTCOMMAND_IDEMPTY.Item2);
                    return;
                }

                // 3. JobID 중복
                if (transferManager.ExistTransportCommand(cmd.JobId))
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: duplicated JobID - job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1,
                        AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2);
                    return;
                }

                // 4. 슬롯 역할 검증 (공백=자동배정 허용, D10)
                if (!IsValidSlot(cmd.LoadCarrierSlot, 1, 2) || !IsValidSlot(cmd.UnloadCarrierSlot, 3, 4))
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: invalid CarrierSlot - load={cmd.LoadCarrierSlot}, unload={cmd.UnloadCarrierSlot}, job={cmd.JobId}");
                    Fail(context, "106", "INVALIDCARRIERSLOT");
                    return;
                }

                // 위치 해석 — 포트 누락 시 '<Loc>:<Port>' 후보 첫 항목으로 보정, 최종 없으면 LEFT
                var resource = accessor.Resolve<IResourceManagerEx>();
                string origin = CombineLocPort(cmd.LoadSourceLoc, ResolvePort(resource, cmd.LoadSourceLoc, ""));
                string mid = CombineLocPort(cmd.EquipId, ResolvePort(resource, cmd.EquipId, cmd.Port));
                string dest = CombineLocPort(cmd.UnloadDestLoc, ResolvePort(resource, cmd.UnloadDestLoc, ""));

                // 5~7. 위치 존재 검증
                var originLocation = pathManager.GetLocationByLocationId(origin);
                if (originLocation == null)
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: LoadSourceLoc not found - {origin}, job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item1,
                        AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item2);
                    return;
                }

                var midLocation = pathManager.GetLocationByLocationId(mid);
                if (midLocation == null)
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: EquipID location not found - {mid}, job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1,
                        AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2);
                    return;
                }

                var destLocation = pathManager.GetLocationByLocationId(dest);
                if (destLocation == null)
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: UnloadDestLoc not found - {dest}, job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1,
                        AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2);
                    return;
                }

                // 8. 위치 중복 차단
                if (string.Equals(origin, mid, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mid, dest, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(origin, dest, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: waypoint duplicated - origin={origin}, mid={mid}, dest={dest}, job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_SOURCEDESTMACHINE_DUPLICATE.Item1,
                        AbstractManager.ID_RESULT_SOURCEDESTMACHINE_DUPLICATE.Item2);
                    return;
                }

                // 9. 공통 Bay (Origin~Mid, Mid~Dest 둘 다 성립해야 함)
                string bayOriginMid = pathManager.GetCommonUseBayIdBySourceDest(
                    originLocation.StationId, midLocation.StationId, "Y");
                string bayMidDest = pathManager.GetCommonUseBayIdBySourceDest(
                    midLocation.StationId, destLocation.StationId, "Y");
                if (bayOriginMid == null || bayMidDest == null)
                {
                    logger.Warn($"CreateExchangeTransportCommandActivity: no common bay - origin={origin}, mid={mid}, dest={dest}, bayOM={bayOriginMid}, bayMD={bayMidDest}, job={cmd.JobId}");
                    Fail(context, AbstractManager.ID_RESULT_NOTSAMEBAY.Item1,
                        AbstractManager.ID_RESULT_NOTSAMEBAY.Item2);
                    return;
                }

                // TC 생성 — 구현사양서 §2.1 스냅샷
                var tc = new TransportCommandEx
                {
                    JobId = cmd.JobId,
                    State = TransportCommandEx.STATE_EXCHANGE_QUEUED,   // D5: 기존 스케줄러(QUEUED 조회) 자연 배제
                    JobType = TransportCommandEx.JOBTYPE_EXCHANGE,
                    Source = origin,
                    OriginLoc = origin,
                    MidLoc = cmd.EquipId,
                    MidPortId = ResolvePort(resource, cmd.EquipId, cmd.Port),
                    Dest = dest,
                    EqpId = cmd.AcsId,                                   // D7: eqpId=AcsId, portId=NULL
                    PortId = null,
                    BayId = bayOriginMid,
                    Priority = TransportCommandEx.DEFAULT_PRIORITY,
                    Description = $"MODEL='{cmd.Model}';{cmd.MaterialType}",
                    AdditionalInfo = ExchangeInfo.BuildInitial(cmd.LoadEquipJobId, cmd.UnloadEquipJobId),
                    CreateTime = DateTime.Now,
                    QueuedTime = DateTime.Now,
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

                transferManager.CreateTransportCommand(tc);

                context.Set(TransportCommandId, cmd.JobId);
                context.Set(ErrCode, "0");
                context.Set(ErrMsg, "");
                context.Set(Result, true);

                logger.Info($"CreateExchangeTransportCommandActivity: EXCHANGE TC created - job={cmd.JobId}, origin={origin}, mid={mid}, dest={dest}, bay={bayOriginMid}, state={tc.State}");
            }
            catch (Exception ex)
            {
                logger.Error($"CreateExchangeTransportCommandActivity: {ex.Message}", ex);
                Fail(context, "03", ex.Message);
            }
        }

        private void Fail(ActivityExecutionContext context, string code, string msg)
        {
            context.Set(ErrCode, code);
            context.Set(ErrMsg, msg);
            context.Set(Result, false);
        }

        /// <summary>슬롯 값 검증: 공백=자동배정 허용(D10), 값이 있으면 허용 범위 내 정수여야 함</summary>
        internal static bool IsValidSlot(string value, int allowedA, int allowedB)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (!int.TryParse(value.Trim(), out int n)) return false;
            return n == allowedA || n == allowedB;
        }

        private static string CombineLocPort(string loc, string port)
        {
            if (string.IsNullOrWhiteSpace(loc)) return "";
            return string.IsNullOrWhiteSpace(port) ? loc : $"{loc}:{port}";
        }

        /// <summary>
        /// 포트 해석: 명시 포트 우선 → NA_R_LOCATION 의 '<Loc>:<Port>' 첫 후보 → 기본 LEFT.
        /// (기존 ResolveMissingPortByLocPrefix + LEFT 기본값 관례 준수)
        /// </summary>
        private static string ResolvePort(IResourceManagerEx resource, string loc, string explicitPort)
        {
            if (!string.IsNullOrWhiteSpace(explicitPort)) return explicitPort.Trim();
            if (string.IsNullOrWhiteSpace(loc) || resource == null) return "LEFT";

            var all = resource.GetLocations();
            if (all != null)
            {
                var prefix = loc + ":";
                var candidates = new List<string>();
                foreach (LocationEx l in all)
                {
                    if (l == null || string.IsNullOrEmpty(l.LocationId)) continue;
                    if (l.LocationId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        candidates.Add(l.LocationId);
                }
                if (candidates.Count > 0)
                {
                    candidates.Sort(StringComparer.OrdinalIgnoreCase);
                    var first = candidates[0];
                    int idx = first.IndexOf(':');
                    if (idx >= 0 && idx < first.Length - 1) return first.Substring(idx + 1);
                }
            }
            return "LEFT";
        }
    }

    /// <summary>
    /// EXCHANGE 전용 JOBREPORT 빌더+전송 (구현사양서 §4.5).
    /// Step/StepName/CarrierSlot 을 포함하며, 기존 SendJobReportActivity(MOVECMD)는 무수정 (D4).
    /// DestSubject = EXCHANGECMD Header 의 ReplySubject (없으면 설정 기본값).
    /// </summary>
    [Activity("ACS.Host", "Send Exchange Job Report",
        "EXCHANGE JOBREPORT(Step/StepName/CarrierSlot 포함)를 Host(MES)에 전송합니다.")]
    public class SendExchangeJobReportActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "수신한 EXCHANGECMD XmlDocument (필드 추출용)")]
        public Input<XmlDocument> ExchangeCmdXml { get; set; }

        [Input(Description = "리포트 타입 (RECEIVE, START, ARRIVED, STEP_COMPLETE, COMPLETE, CANCEL)")]
        public Input<string> ReportType { get; set; } = new("RECEIVE");

        [Input(Description = "Step (10/20/30/40/50/60)")]
        public Input<string> Step { get; set; } = new("10");

        [Input(Description = "StepName (PICKUP_NEW/MOVE_TO_EQUIP/UNLOAD_OLD/LOAD_NEW/RETURN_OLD/DONE)")]
        public Input<string> StepName { get; set; } = new("PICKUP_NEW");

        [Input(Description = "ActionType (EXCHANGE/UNLOAD/LOAD/MOVE)")]
        public Input<string> ActionType { get; set; } = new("EXCHANGE");

        [Input(Description = "CarrierSlot (1~4, 해당 시에만)")]
        public Input<string> CarrierSlot { get; set; }

        [Input(Description = "AMR ID (배차 전 공백)")]
        public Input<string> AmrId { get; set; }

        [Input(Description = "에러 코드 (0=정상)")]
        public Input<string> ErrCode { get; set; }

        [Input(Description = "에러 메시지")]
        public Input<string> ErrMsg { get; set; }

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
                    logger.Error("SendExchangeJobReportActivity: IHostMessageService not available");
                    context.Set(Result, false);
                    return;
                }

                var cmdXml = ExchangeCmdXml?.Get(context);
                var cmd = ExchangeCmdModel.Parse(cmdXml);

                string reportType = ReportType?.Get(context) ?? "RECEIVE";
                string errCode = ErrCode?.Get(context);
                if (string.IsNullOrEmpty(errCode)) errCode = "0";
                string errMsg = ErrMsg?.Get(context) ?? "";

                string acsId = cmd.AcsId;
                var configuration = accessor.Resolve<Microsoft.Extensions.Configuration.IConfiguration>();
                if (string.IsNullOrEmpty(acsId))
                    acsId = configuration?["Acs:Process:Name"] ?? "ACS01";

                // 라우팅: 요청의 ReplySubject 로 회신 (없으면 설정 기본값)
                string destSubject = cmd.ReplySubject;
                if (string.IsNullOrEmpty(destSubject))
                    destSubject = configuration?["Acs:Host:DestSubject"] ?? "/HQ/MES01";
                string replySubject = configuration?["Acs:Host:ReplySubject"] ?? $"/HQ/{acsId}";

                var doc = new XmlDocument();
                var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(decl);
                var msg = doc.CreateElement("Msg");
                doc.AppendChild(msg);
                Append(doc, msg, "Command", "JOBREPORT");

                var header = doc.CreateElement("Header");
                msg.AppendChild(header);
                Append(doc, header, "DestSubject", destSubject);
                Append(doc, header, "ReplySubject", replySubject);

                var dataLayer = doc.CreateElement("DataLayer");
                msg.AppendChild(dataLayer);
                Append(doc, dataLayer, "AcsId", acsId);
                Append(doc, dataLayer, "Type", reportType);
                Append(doc, dataLayer, "Step", Step?.Get(context) ?? "");
                Append(doc, dataLayer, "StepName", StepName?.Get(context) ?? "");
                Append(doc, dataLayer, "AmrId", AmrId?.Get(context) ?? "");
                Append(doc, dataLayer, "ActionType", ActionType?.Get(context) ?? "EXCHANGE");
                Append(doc, dataLayer, "JobID", cmd.JobId);
                Append(doc, dataLayer, "CarrierSlot", CarrierSlot?.Get(context) ?? "");
                Append(doc, dataLayer, "MaterialType", cmd.MaterialType);
                Append(doc, dataLayer, "UserID", cmd.UserId);
                Append(doc, dataLayer, "ErrorCode", errCode);
                Append(doc, dataLayer, "ErrorMsg", errMsg);

                hostMessageService.SendToHost("JOBREPORT", doc);

                context.Set(JobReportXml, doc);
                context.Set(Result, true);
                logger.Info($"SendExchangeJobReportActivity: JOBREPORT sent - type={reportType}, step={Step?.Get(context)}, job={cmd.JobId}, errCode={errCode}");
            }
            catch (Exception ex)
            {
                logger.Error($"SendExchangeJobReportActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }

        private static void Append(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var el = doc.CreateElement(name);
            el.InnerText = value ?? "";
            parent.AppendChild(el);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  EXCHANGE-JOBREPORT 릴레이 수신 (S4) — TS 발신 JSON → MES XML 전달
    //  기존 JOBREPORT 릴레이(JobReportData/ExtractJobReportFromInput/
    //  ForwardJobReportToMesActivity)에는 Step/StepName/CarrierSlot 필드가
    //  없어 무수정 유지(D4), messageName="EXCHANGE-JOBREPORT" 병렬 경로 신설.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>EXCHANGE JOBREPORT 릴레이 데이터 (기존 JobReportData + Step/StepName/CarrierSlot).</summary>
    public class ExchangeJobReportData
    {
        public string AcsId { get; set; } = "";
        public string Type { get; set; } = "";
        public string Step { get; set; } = "";
        public string StepName { get; set; } = "";
        public string AmrId { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string JobID { get; set; } = "";
        public string CarrierSlot { get; set; } = "";
        public string MaterialType { get; set; } = "";
        public string UserID { get; set; } = "";
        public string ErrorCode { get; set; } = "";
        public string ErrorMsg { get; set; } = "";
        public string RoutedFrom { get; set; } = "";
    }

    /// <summary>
    /// 워크플로우 Input(Arguments)에서 EXCHANGE-JOBREPORT JSON 을 파싱해 ExchangeJobReportData 로 추출.
    /// ExtractJobReportFromInput 미러 + Step/StepName/CarrierSlot 필드 추가.
    /// </summary>
    [Activity("ACS.Host", "Extract Exchange JobReport JSON",
        "워크플로우 입력에서 EXCHANGE-JOBREPORT JSON 을 파싱하여 추출합니다.")]
    public class ExtractExchangeJobReportFromInput : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "추출된 EXCHANGE JOBREPORT 데이터")]
        public Output<ExchangeJobReportData> OutputData { get; set; }

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

            var data = new ExchangeJobReportData();
            if (string.IsNullOrEmpty(json))
            {
                logger.Warn("ExtractExchangeJobReportFromInput: No JSON found in input, returning empty data");
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
                    data.Step = ReadString(d, "Step");
                    data.StepName = ReadString(d, "StepName");
                    data.AmrId = ReadString(d, "AmrId");
                    data.ActionType = ReadString(d, "ActionType");
                    data.JobID = ReadString(d, "JobID");
                    data.CarrierSlot = ReadString(d, "CarrierSlot");
                    data.MaterialType = ReadString(d, "MaterialType");
                    data.UserID = ReadString(d, "UserID");
                    data.ErrorCode = ReadString(d, "ErrorCode");
                    data.ErrorMsg = ReadString(d, "ErrorMsg");
                }

                if (root.TryGetProperty("header", out var h) && h.ValueKind == JsonValueKind.Object)
                {
                    data.RoutedFrom = ReadString(h, "routedFrom");
                }

                context.Set(OutputData, data);
                logger.Info($"ExtractExchangeJobReportFromInput: parsed - JobID={data.JobID}, Type={data.Type}, Step={data.Step}, AmrId={data.AmrId}");
            }
            catch (Exception ex)
            {
                logger.Error($"ExtractExchangeJobReportFromInput: JSON parse 실패 - {ex.Message}", ex);
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
    /// EXCHANGE JOBREPORT 를 MES TCP 로 전달.
    /// MES XML DataLayer 레이아웃은 SendExchangeJobReportActivity 와 동일
    /// (AcsId/Type/Step/StepName/AmrId/ActionType/JobID/CarrierSlot/MaterialType/UserID/ErrorCode/ErrorMsg).
    /// 비-Host 프로세스로 라우팅 누수 시 ForwardJobReportToMesActivity 와 동일하게
    /// routedFrom 루프 차단 후 host 큐 재발행.
    /// </summary>
    [Activity("ACS.Host", "Forward Exchange JobReport to MES",
        "EXCHANGE JOBREPORT 를 MES 로 TCP 전달합니다 (JSON → XML 변환 후 송신).")]
    public class ForwardExchangeJobReportToMesActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "전달할 EXCHANGE JOBREPORT 데이터")]
        public Input<ExchangeJobReportData> JobReportData { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var data = JobReportData?.Get(context);
                if (data == null)
                {
                    logger.Error("ForwardExchangeJobReportToMesActivity: JobReportData is null");
                    context.Set(Result, false);
                    return;
                }

                var hostMessageService = accessor?.ResolveOptional<IHostMessageService>();
                if (hostMessageService != null)
                {
                    // === Host 프로세스: 정상 경로 — MES TCP 송신 ===
                    var configuration = accessor.ResolveOptional<IConfiguration>();
                    string acsId = data.AcsId;
                    if (string.IsNullOrEmpty(acsId))
                        acsId = configuration?["Acs:Process:Name"] ?? "ACS01";
                    string destSubject = configuration?["Acs:Host:DestSubject"] ?? "/HQ/MES01";
                    string replySubject = configuration?["Acs:Host:ReplySubject"] ?? $"/HQ/{acsId}";

                    var doc = new XmlDocument();
                    var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                    doc.AppendChild(decl);
                    var msg = doc.CreateElement("Msg");
                    doc.AppendChild(msg);
                    Append(doc, msg, "Command", "JOBREPORT");

                    var header = doc.CreateElement("Header");
                    msg.AppendChild(header);
                    Append(doc, header, "DestSubject", destSubject);
                    Append(doc, header, "ReplySubject", replySubject);

                    var dataLayer = doc.CreateElement("DataLayer");
                    msg.AppendChild(dataLayer);
                    Append(doc, dataLayer, "AcsId", acsId);
                    Append(doc, dataLayer, "Type", data.Type);
                    Append(doc, dataLayer, "Step", data.Step);
                    Append(doc, dataLayer, "StepName", data.StepName);
                    Append(doc, dataLayer, "AmrId", data.AmrId);
                    Append(doc, dataLayer, "ActionType", data.ActionType);
                    Append(doc, dataLayer, "JobID", data.JobID);
                    Append(doc, dataLayer, "CarrierSlot", data.CarrierSlot);
                    Append(doc, dataLayer, "MaterialType", data.MaterialType);
                    Append(doc, dataLayer, "UserID", data.UserID);
                    Append(doc, dataLayer, "ErrorCode", data.ErrorCode);
                    Append(doc, dataLayer, "ErrorMsg", data.ErrorMsg);

                    hostMessageService.SendToHost("JOBREPORT", doc);
                    context.Set(Result, true);
                    logger.Info($"ForwardExchangeJobReportToMesActivity: forwarded to MES - JobID={data.JobID}, Type={data.Type}, Step={data.Step}");
                    return;
                }

                // === 비-Host 프로세스: 라우팅 누수 — host 큐 재발행 (routedFrom 루프 차단) ===
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
                    logger.Error($"ForwardExchangeJobReportToMesActivity: IHostMessageService 미등록 — 재발행 불가. " +
                                 $"hostAgent={hostAgent != null}, alreadyReRouted={alreadyReRouted}, " +
                                 $"currentProcess={currentProcess}, routedFrom={data.RoutedFrom}, " +
                                 $"JobID={data.JobID}, Type={data.Type}. 메시지 유실 — 라우팅 누수 점검 필요.");
                    context.Set(Result, false);
                    return;
                }

                string rerouted = JsonSerializer.Serialize(new
                {
                    header = new
                    {
                        messageName = "EXCHANGE-JOBREPORT",
                        transactionId = Guid.NewGuid().ToString("N"),
                        destSubject = "",
                        replySubject = "",
                        routedFrom = currentProcess
                    },
                    data = new
                    {
                        AcsId = data.AcsId ?? "",
                        Type = data.Type ?? "",
                        Step = data.Step ?? "",
                        StepName = data.StepName ?? "",
                        AmrId = data.AmrId ?? "",
                        ActionType = data.ActionType ?? "",
                        JobID = data.JobID ?? "",
                        CarrierSlot = data.CarrierSlot ?? "",
                        MaterialType = data.MaterialType ?? "",
                        UserID = data.UserID ?? "",
                        ErrorCode = data.ErrorCode ?? "",
                        ErrorMsg = data.ErrorMsg ?? ""
                    }
                });
                hostAgent.Send((object)rerouted);
                context.Set(Result, true);
                logger.Warn($"ForwardExchangeJobReportToMesActivity: 비-Host 프로세스({currentProcess})에서 실행됨 — host 큐로 재발행. " +
                            $"JobID={data.JobID}, Type={data.Type}. 라우팅 누수 점검 필요.");
            }
            catch (Exception ex)
            {
                logger.Error($"ForwardExchangeJobReportToMesActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }

        private static void Append(XmlDocument doc, XmlElement parent, string name, string value)
        {
            var el = doc.CreateElement(name);
            el.InnerText = value ?? "";
            parent.AppendChild(el);
        }
    }
}
