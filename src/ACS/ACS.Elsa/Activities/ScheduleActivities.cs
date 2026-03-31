using System;
using System.Collections;
using System.Xml;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Msb;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Path;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Bridge;
using Microsoft.Extensions.Configuration;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  Schedule Activities
    //  Category: ACS.Schedule
    //
    //  Daemon 서버의 스케줄링 관련 Activity.
    //  Queued TC를 idle AMR에 할당하고 Host에 보고.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Queued 상태 TransportCommand를 Dest 기준 가장 가까운 idle AMR에 할당.
    ///
    /// 처리 흐름:
    ///   1. GetQueuedTransportCommands() → queued TC 목록
    ///   2. foreach TC:
    ///      a. Dest에서 portId 추출 → GetLocationByLocationId → dest Location
    ///      b. SearchSuitableVehicle(destLocation) → 가장 가까운 vehicle
    ///      c. vehicle null/not IDLE/not CONNECT → skip
    ///      d. 이미 할당된 vehicle → skip
    ///      e. TC에 vehicle 할당 (ASSIGNED) + vehicle에 TC 할당
    ///      f. JOBREPORT(START) 전송
    ///   3. Output: 할당된 TC 수
    /// </summary>
    [Activity("ACS.Schedule", "Schedule Queue Job",
        "Queued TC를 idle AMR에 할당하고 JOBREPORT(START) 전송")]
    public class ScheduleQueueJobActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "할당된 TC 수")]
        public Output<int> AssignedCount { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            int assigned = 0;

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("ScheduleQueueJobActivity: AutofacContainerAccessor not available");
                    context.Set(AssignedCount, 0);
                    return;
                }

                var transferManager = accessor.Resolve<ITransferManagerEx>();
                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                var cacheManager = accessor.Resolve<ICacheManagerEx>();
                var pathManager = accessor.Resolve<IPathManagerEx>();
                var configuration = accessor.ResolveOptional<IConfiguration>();
                // HostAgentSender: Trans → Host RabbitMQ sender (named registration)
                var hostSender = accessor.ResolveNamed<IMessageAgent>("HostAgentSender");

                if (transferManager == null || resourceManager == null ||
                    cacheManager == null || pathManager == null || hostSender == null)
                {
                    logger.Error("ScheduleQueueJobActivity: Required services not available " +
                        $"(transfer={transferManager != null}, resource={resourceManager != null}, " +
                        $"cache={cacheManager != null}, path={pathManager != null}, hostSender={hostSender != null})");
                    context.Set(AssignedCount, 0);
                    return;
                }

                // 1. Queued TC 목록 조회
                IList queuedList = transferManager.GetQueuedTransportCommands();
                if (queuedList == null || queuedList.Count == 0)
                {
                    context.Set(AssignedCount, 0);
                    return;
                }

                logger.Info($"ScheduleQueueJobActivity: {queuedList.Count} queued TC(s) found");

                // 2. 각 TC에 대해 idle AMR 할당 시도
                foreach (TransportCommandEx tc in queuedList)
                {
                    try
                    {
                        assigned += TryAssignVehicle(tc, transferManager, resourceManager,
                            cacheManager, pathManager, hostSender, configuration);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"ScheduleQueueJobActivity: Failed to assign TC {tc.JobId} - {ex.Message}", ex);
                    }
                }

                if (assigned > 0)
                    logger.Info($"ScheduleQueueJobActivity: {assigned} job(s) assigned");
            }
            catch (Exception ex)
            {
                logger.Error($"ScheduleQueueJobActivity: {ex.Message}", ex);
            }

            context.Set(AssignedCount, assigned);
        }

        /// <summary>
        /// 단일 TC에 대해 vehicle 할당을 시도. 성공 시 1, 실패 시 0 반환.
        /// </summary>
        private int TryAssignVehicle(
            TransportCommandEx tc,
            ITransferManagerEx transferManager,
            IResourceManagerEx resourceManager,
            ICacheManagerEx cacheManager,
            IPathManagerEx pathManager,
            IMessageAgent hostSender,
            IConfiguration configuration)
        {
            // a. Dest(eqpId:portId)로 Location 검색
            if (string.IsNullOrEmpty(tc.Dest))
            {
                logger.Warn($"ScheduleQueueJobActivity: TC {tc.JobId} has empty Dest, skipping");
                return 0;
            }

            // b. Dest 전체(eqpId:portId)가 LocationId와 일치
            LocationEx destLocation = cacheManager.GetLocationByLocationId(tc.Dest);
            if (destLocation == null)
            {
                logger.Warn($"ScheduleQueueJobActivity: TC {tc.JobId} - Location not found for dest={tc.Dest}");
                return 0;
            }

            // c. Dest Location 기준 가장 가까운 vehicle 검색
            VehicleEx vehicle = pathManager.SearchSuitableVehicle(destLocation, tc.BayId);
            if (vehicle == null)
            {
                return 0;
            }

            // d. vehicle 상태 확인: IDLE or CHARGE + CONNECT 여야 함
            if (vehicle.ProcessingState != VehicleEx.PROCESSINGSTATE_IDLE)
            {
                return 0;
            }
            if (vehicle.ConnectionState != VehicleEx.CONNECTIONSTATE_CONNECT)
            {
                return 0;
            }

            // e. 이미 다른 TC에 할당된 vehicle인지 확인
            var existingTc = transferManager.GetTransportCommandByVehicleId(vehicle.VehicleId);
            if (existingTc != null)
            {
                return 0;
            }

            // f. TC에 vehicle 할당
            tc.VehicleId = vehicle.VehicleId;
            tc.State = TransportCommandEx.STATE_ASSIGNED;
            tc.AssignedTime = DateTime.Now;
            transferManager.UpdateTransportCommand(tc);

            // g. Vehicle에 TC 할당
            resourceManager.UpdateVehicleTransportCommandId(vehicle, tc.JobId);
            resourceManager.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_ASSIGNED);

            // h. TRSJOBREPORT(START) 전송 via RabbitMQ → Host process
            var jobReport = BuildJobReportXml(
                "START", tc.JobId, vehicle.VehicleId,
                tc.JobType ?? "", tc.Description ?? "", tc.EqpId ?? "",
                configuration);
            hostSender.Send(jobReport, true, "TRSJOBREPORT");

            logger.Info($"ScheduleQueueJobActivity: TC {tc.JobId} assigned to vehicle {vehicle.VehicleId}, JOBREPORT(START) sent");

            return 1;
        }

        /// <summary>
        /// Dest 문자열에서 portId 추출.
        /// 형식: "DestLoc:DestPort" → DestPort 반환, 콜론 없으면 전체 반환.
        /// </summary>
        private static string ExtractPortId(string dest)
        {
            if (string.IsNullOrEmpty(dest))
                return null;

            int colonIndex = dest.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < dest.Length - 1)
                return dest.Substring(colonIndex + 1);

            return dest;
        }

        /// <summary>
        /// TRSJOBREPORT XML 빌드 (Host process로 RabbitMQ 전송용).
        /// </summary>
        private static XmlDocument BuildJobReportXml(
            string reportType, string jobId, string amrId,
            string actionType, string materialType, string acsId,
            IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(acsId))
                acsId = configuration?["Acs:Process:Name"] ?? "ACS01";
            string destSubject = configuration?["Acs:Host:DestSubject"] ?? "/HQ/MES01";
            string replySubject = configuration?["Acs:Host:ReplySubject"] ?? "/HQ/ACS01";

            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            var msg = doc.CreateElement("Msg");
            doc.AppendChild(msg);

            var cmdElem = doc.CreateElement("Command");
            cmdElem.InnerText = "JOBREPORT";
            msg.AppendChild(cmdElem);

            var header = doc.CreateElement("Header");
            msg.AppendChild(header);
            var destElem = doc.CreateElement("DestSubject");
            destElem.InnerText = destSubject;
            header.AppendChild(destElem);
            var replyElem = doc.CreateElement("ReplySubject");
            replyElem.InnerText = replySubject;
            header.AppendChild(replyElem);

            var dataLayer = doc.CreateElement("DataLayer");
            msg.AppendChild(dataLayer);
            foreach (var (name, value) in new[]
            {
                ("AcsId", acsId), ("Type", reportType), ("AmrId", amrId ?? ""),
                ("ActionType", actionType ?? ""), ("JobID", jobId),
                ("MaterialType", materialType ?? ""), ("UserID", "")
            })
            {
                var elem = doc.CreateElement(name);
                elem.InnerText = value;
                dataLayer.AppendChild(elem);
            }

            return doc;
        }
    }
}
