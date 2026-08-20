using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Autofac;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Core.Message;
using ACS.Core.Message.Model;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  Schedule Activities
    //  Category: ACS.Schedule
    //
    //  Queued TC 스케줄링 워크플로우용 Input/Output Activity들.
    //  ForEach + If 조합으로 워크플로우에서 데이터를 전달하며 조합.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Queued 상태의 TransportCommand 목록을 조회.
    /// </summary>
    [Activity("ACS.Schedule", "Get Queued TCs",
        "Queued 상태 TransportCommand 목록 조회 (BayId 필터 지원)")]
    public class GetQueuedTransportCommandsActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "Queued TC 목록")]
        public Output<ICollection<TransportCommandEx>> QueuedCommands { get; set; }

        [Output(Description = "Queued TC 수")]
        public Output<int> Count { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                if (transferManager == null)
                {
                    logger.Error("GetQueuedTransportCommandsActivity: ITransferManagerEx not available");
                    context.Set(QueuedCommands, (ICollection<TransportCommandEx>)new List<TransportCommandEx>());
                    context.Set(Count, 0);
                    return;
                }

                // 워크플로우 input Arguments[0] JSON에서 bayId 추출
                string bayId = ExtractBayIdFromInput(context);

                IList rawList;
                if (!string.IsNullOrEmpty(bayId))
                    rawList = transferManager.GetQueuedTransportCommandsByBayId(bayId);
                else
                    rawList = transferManager.GetQueuedTransportCommands();

                var list = rawList?.Cast<TransportCommandEx>().ToList() ?? new List<TransportCommandEx>();
                context.Set(QueuedCommands, (ICollection<TransportCommandEx>)list);
                context.Set(Count, list.Count);

                if (list.Count > 0)
                    logger.Info($"GetQueuedTransportCommandsActivity: {list.Count} queued TC(s) found (bayId={bayId})");
            }
            catch (Exception ex)
            {
                logger.Error($"GetQueuedTransportCommandsActivity: {ex.Message}", ex);
                context.Set(QueuedCommands, (ICollection<TransportCommandEx>)new List<TransportCommandEx>());
                context.Set(Count, 0);
            }
        }

        /// <summary>
        /// 워크플로우 input의 Arguments[0] JSON 문자열에서 data.bayId를 추출.
        /// </summary>
        private string ExtractBayIdFromInput(ActivityExecutionContext context)
        {
            try
            {
                if (!context.WorkflowExecutionContext.Input.TryGetValue("Arguments", out var argsObj))
                    return "";

                var args = argsObj as object[];
                if (args == null || args.Length == 0)
                    return "";

                string jsonStr = args[0] as string;
                if (string.IsNullOrEmpty(jsonStr))
                    return "";

                var msg = JsonSerializer.Deserialize<DaemonScheduleMessage>(jsonStr);
                return msg?.Data?.BayId ?? "";
            }
            catch (Exception ex)
            {
                logger.Warn($"GetQueuedTransportCommandsActivity: Failed to extract bayId from input: {ex.Message}");
                return "";
            }
        }
    }

    /// <summary>
    /// TC의 Dest 기준으로 가장 가까운 idle + connect 상태 vehicle을 검색.
    /// vehicle 상태 검증(IDLE+CONNECT) 및 기존 할당 여부 확인 포함.
    /// </summary>
    [Activity("ACS.Schedule", "Find Suitable Vehicle",
        "TC Dest 기준 가장 가까운 idle vehicle 검색")]
    public class FindSuitableVehicleActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "대상 TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Output(Description = "검색된 Vehicle (없으면 null)")]
        public Output<VehicleEx> Vehicle { get; set; }

        [Output(Description = "적합한 Vehicle을 찾았는지 여부")]
        public Output<bool> Found { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            if (tc == null)
            {
                context.Set(Found, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var cacheManager = accessor?.Resolve<ICacheManagerEx>();
                var pathManager = accessor?.Resolve<IPathManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();

                if (cacheManager == null || pathManager == null || transferManager == null)
                {
                    logger.Error("FindSuitableVehicleActivity: Required services not available");
                    context.Set(Found, false);
                    return;
                }

                // Dest Location 조회
                if (string.IsNullOrEmpty(tc.Dest))
                {
                    logger.Warn($"FindSuitableVehicleActivity: TC {tc.JobId} has empty Dest");
                    context.Set(Found, false);
                    return;
                }

                LocationEx sourceLocation = cacheManager.GetLocationByLocationId(tc.Source);
                if (sourceLocation == null)
                {
                    logger.Warn($"FindSuitableVehicleActivity: Location not found for source={tc.Source}");
                    context.Set(Found, false);
                    return;
                }

                // 가장 가까운 vehicle 검색
                VehicleEx vehicle = pathManager.SearchSuitableVehicle(sourceLocation, tc.BayId);
                if (vehicle == null)
                {
                    context.Set(Found, false);
                    return;
                }

                // 상태 검증: IDLE + CONNECT
                if (vehicle.ProcessingState != VehicleEx.PROCESSINGSTATE_IDLE ||
                    vehicle.ConnectionState != VehicleEx.CONNECTIONSTATE_CONNECT)
                {
                    context.Set(Found, false);
                    return;
                }

                // 기존 할당 확인
                var existingTc = transferManager.GetTransportCommandByVehicleId(vehicle.VehicleId);
                if (existingTc != null)
                {
                    context.Set(Found, false);
                    return;
                }

                context.Set(Vehicle, vehicle);
                context.Set(Found, true);
            }
            catch (Exception ex)
            {
                logger.Error($"FindSuitableVehicleActivity: {ex.Message}", ex);
                context.Set(Found, false);
            }
        }
    }

    /// <summary>
    /// TC에 Vehicle을 할당하고 양쪽 상태를 ASSIGNED로 변경.
    /// </summary>
    [Activity("ACS.Schedule", "Assign Vehicle To TC",
        "TC에 Vehicle 할당 + 상태 ASSIGNED로 변경")]
    public class AssignVehicleToTransportCommandActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "할당 대상 TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "할당할 Vehicle")]
        public Input<VehicleEx> Vehicle { get; set; }

        [Output(Description = "할당 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicle = Vehicle?.Get(context);

            if (tc == null || vehicle == null)
            {
                logger.Error("AssignVehicleToTransportCommandActivity: TC or Vehicle is null");
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();

                if (transferManager == null || resourceManager == null)
                {
                    logger.Error("AssignVehicleToTransportCommandActivity: Required services not available");
                    context.Set(Success, false);
                    return;
                }

                // TC에 vehicle 할당
                tc.VehicleId = vehicle.VehicleId;
                tc.State = TransportCommandEx.STATE_ASSIGNED;
                tc.AssignedTime = DateTime.Now;
                transferManager.UpdateTransportCommand(tc);

                // Vehicle에 TC 할당
                resourceManager.UpdateVehicleTransportCommandId(vehicle, tc.JobId);
                resourceManager.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_ASSIGNED);
                resourceManager.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_RUN);

                // AcsDestNodeId 를 source 의 StationId 로 세팅 — RailVehicleDestArrivedWorkflow 가
                // source 도착을 검출하려면 할당 시점에 채워져 있어야 한다. acquire-complete 이후
                // RailVehicleAcquireCompletedWorkflow.UpdateVehicleAcsDestNodeToDest 가 dest 로 덮어쓴다.
                var cacheManager = accessor?.Resolve<ICacheManagerEx>();
                if (cacheManager != null && !string.IsNullOrEmpty(tc.Source))
                {
                    LocationEx sourceLoc = cacheManager.GetLocationByLocationId(tc.Source);
                    if (sourceLoc != null && !string.IsNullOrEmpty(sourceLoc.StationId))
                    {
                        resourceManager.UpdateVehicleAcsDestNodeId(vehicle, sourceLoc.StationId, "SCHEDULE-QUEUEJOB");
                        vehicle.AcsDestNodeId = sourceLoc.StationId;
                        logger.Info($"AssignVehicleToTransportCommandActivity: AcsDestNodeId={sourceLoc.StationId} (source={tc.Source})");
                    }
                    else
                    {
                        logger.Warn($"AssignVehicleToTransportCommandActivity: source Location/StationId 조회 실패 source={tc.Source}, tc={tc.JobId}");
                    }
                }

                logger.Info($"AssignVehicleToTransportCommandActivity: TC {tc.JobId} → Vehicle {vehicle.VehicleId}");
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"AssignVehicleToTransportCommandActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }
    }

    /// <summary>
    /// JOBREPORT(START)를 Host 프로세스로 전송.
    /// </summary>
    [Activity("ACS.Schedule", "Send JobReport START",
        "JOBREPORT(START) Host 프로세스 전송")]
    public class SendJobReportStartActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "할당된 Vehicle ID")]
        public Input<string> VehicleId { get; set; }

        [Output(Description = "전송 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicleId = VehicleId?.Get(context);

            if (tc == null || string.IsNullOrEmpty(vehicleId))
            {
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendJobReportStartActivity: IMessageManagerEx not resolved");
                    context.Set(Success, false);
                    return;
                }

                messageManager.SendJobReportToHost(
                    "START", tc.JobId, vehicleId, tc.JobType ?? "", tc.Description ?? "");

                logger.Info($"SendJobReportStartActivity: JOBREPORT(START) sent for TC {tc.JobId}");
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"SendJobReportStartActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }
    }

    /// <summary>
    /// RAIL-CARRIERTRANSFER JSON을 EI 프로세스로 전송.
    /// </summary>
    [Activity("ACS.Schedule", "Send Carrier Transfer",
        "RAIL-CARRIERTRANSFER JSON EI 프로세스 전송")]
    public class SendCarrierTransferActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "할당된 Vehicle ID")]
        public Input<string> VehicleId { get; set; }

        [Input(Description = "작업 유형 (UNLOAD / LOAD)")]
        public Input<string> JobType { get; set; }

        [Input(Description = "true=Source 기준, false=Dest 기준")]
        public Input<bool> UseSource { get; set; }

        [Output(Description = "전송 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicleId = VehicleId?.Get(context);
            var jobType = JobType?.Get(context);
            var useSource = UseSource?.Get(context) ?? true;

            if (tc == null || string.IsNullOrEmpty(vehicleId))
            {
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendCarrierTransferActivity: IMessageManagerEx not resolved");
                    context.Set(Success, false);
                    return;
                }

                string json = CarrierTransferJsonBuilder.Build(tc, vehicleId, jobType, useSource, resourceManager, logger);
                if (string.IsNullOrEmpty(json))
                {
                    context.Set(Success, false);
                    return;
                }

                messageManager.SendCarrierTransferJson(json);

                logger.Info($"SendCarrierTransferActivity: RAIL-CARRIERTRANSFER sent for TC {tc.JobId}, vehicleId={vehicleId}, jobType={jobType}, useSource={useSource}");
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"SendCarrierTransferActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }
    }

    /// <summary>
    /// RAIL-CARRIERTRANSFER JSON 빌드 공유 헬퍼.
    /// Source 단계(UNLOAD)와 Dest 단계(LOAD) 모두에서 사용.
    /// useSource=true면 tc.Source, false면 tc.Dest를 기준으로
    /// destPortId / destNodeId / portType(LocationEx.Type 값)을 채운다.
    /// </summary>
    internal static class CarrierTransferJsonBuilder
    {
        public static string Build(TransportCommandEx tc, string vehicleId, string jobType,
            bool useSource, IResourceManagerEx resourceManager, Logger logger)
        {
            // 기존 시그니처 유지 — tc.Source/tc.Dest 2택, amrSlot 기본 1 (출력 불변)
            return Build(tc, vehicleId, jobType, useSource ? tc.Source : tc.Dest, 1, resourceManager, logger);
        }

        /// <summary>
        /// EXCHANGE(v2) S5: 임의 목적지(LocationId)와 amrSlot 을 지정하는 확장 빌드.
        /// mid(설비, MidLoc:MidPortId) 등 tc.Source/Dest 이외의 waypoint 전송에 사용.
        /// </summary>
        public static string Build(TransportCommandEx tc, string vehicleId, string jobType,
            string targetLocationId, int amrSlot, IResourceManagerEx resourceManager, Logger logger)
        {
            try
            {
                string src = targetLocationId;

                // portId (machine:unit) 파싱
                string portId = "";
                string machineName = "";
                string unitName = "";
                if (!string.IsNullOrEmpty(src))
                {
                    int colonIdx = src.IndexOf(':');
                    if (colonIdx >= 0)
                    {
                        machineName = src.Substring(0, colonIdx);
                        unitName = src.Substring(colonIdx + 1);
                        portId = machineName + ":" + unitName;
                    }
                    else
                    {
                        machineName = src;
                        portId = src + ":";
                    }
                }

                // nodeId 및 portType (LocationEx.Type: EQP/BUFFER/INPUT/OUTPUT/CHARGE/VBUFFER) 조회
                string nodeId = "";
                string portType = "";
                try
                {
                    var location = resourceManager?.GetLocationByLocationId(portId);
                    if (location != null)
                    {
                        nodeId = location.StationId ?? "";
                        portType = location.Type ?? "";
                    }
                }
                catch (Exception ex)
                {
                    logger?.Error($"CarrierTransferJsonBuilder: location 조회 실패 portId={portId} - {ex.Message}");
                }

                var message = new RailCarrierTransferMessage
                {
                    Header = new RailCarrierTransferHeader
                    {
                        MessageName = "RAIL-CARRIERTRANSFER",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "Trans"
                    },
                    Data = new RailCarrierTransferData
                    {
                        CommandId = tc.JobId,
                        VehicleId = vehicleId,
                        DestPortId = portId,
                        DestNodeId = nodeId,
                        Priority = tc.Priority.ToString(),
                        CarrierType = tc.CarrierId ?? "",
                        Port = unitName ?? "",
                        JobType = string.IsNullOrEmpty(jobType) ? (tc.JobType ?? "") : jobType,
                        PortType = portType,
                        Model = tc.GetModel() ?? "",
                        ResultCode = "",
                        AmrSlot = amrSlot
                    }
                };

                return JsonSerializer.Serialize(message);
            }
            catch (Exception ex)
            {
                logger?.Error($"CarrierTransferJsonBuilder: JSON 빌드 실패 - {ex.Message}", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// RAIL-CARRIERTRANSFER JSON을 EI 프로세스로 전송하고 Reply 응답을 대기.
    /// 5초 타임아웃, 최대 3회 재시도.
    /// </summary>
    [Activity("ACS.Schedule", "Send Carrier Transfer With Retry",
        "RAIL-CARRIERTRANSFER 전송 + 응답 대기 (5초 타임아웃, 최대 3회 재시도)")]
    public class SendCarrierTransferWithRetryActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        private const int MaxAttempts = 3;
        private const int TimeoutMs = 5000;

        [Input(Description = "TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "할당된 Vehicle ID")]
        public Input<string> VehicleId { get; set; }

        [Input(Description = "작업 유형 (UNLOAD / LOAD). 비어있으면 tc.JobType 사용")]
        public Input<string> JobType { get; set; }

        [Input(Description = "true=Source 기준(UNLOAD), false=Dest 기준(LOAD)")]
        public Input<bool> UseSource { get; set; }

        [Output(Description = "전송 및 응답 수신 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicleId = VehicleId?.Get(context);
            var jobType = JobType?.Get(context);
            var useSource = UseSource?.Get(context) ?? true;

            if (tc == null || string.IsNullOrEmpty(vehicleId))
            {
                logger.Error("SendCarrierTransferWithRetryActivity: TC or VehicleId is null");
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendCarrierTransferWithRetryActivity: IMessageManagerEx not resolved");
                    context.Set(Success, false);
                    return;
                }

                // RAIL-CARRIERTRANSFER JSON 빌드 (공유 헬퍼 사용)
                string json = CarrierTransferJsonBuilder.Build(tc, vehicleId, jobType, useSource, resourceManager, logger);
                if (string.IsNullOrEmpty(json))
                {
                    logger.Error("SendCarrierTransferWithRetryActivity: JSON 빌드 실패");
                    context.Set(Success, false);
                    return;
                }

                string commandId = tc.JobId;

                // 최대 3회 재시도
                for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    logger.Info($"SendCarrierTransferWithRetryActivity: 시도 {attempt}/{MaxAttempts} - TC {commandId}");

                    // 응답 대기 등록
                    Bridge.CarrierTransferReplyWaiter.RegisterWait(commandId);

                    // RAIL-CARRIERTRANSFER 전송
                    messageManager.SendCarrierTransferJson(json);

                    // 5초간 응답 대기
                    var (replied, resultCode) = Bridge.CarrierTransferReplyWaiter.WaitForReply(commandId, TimeoutMs);

                    if (replied && "OK".Equals(resultCode, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Info($"SendCarrierTransferWithRetryActivity: 응답 수신 성공 - TC {commandId}, attempt={attempt}");
                        context.Set(Success, true);
                        return;
                    }

                    if (replied)
                    {
                        logger.Warn($"SendCarrierTransferWithRetryActivity: 응답 수신했으나 실패 - TC {commandId}, resultCode={resultCode}, attempt={attempt}");
                    }
                    else
                    {
                        logger.Warn($"SendCarrierTransferWithRetryActivity: 응답 타임아웃 ({TimeoutMs}ms) - TC {commandId}, attempt={attempt}");
                    }
                }

                // 3회 모두 실패
                logger.Error($"SendCarrierTransferWithRetryActivity: {MaxAttempts}회 시도 모두 실패 - TC {commandId}");
                context.Set(Success, false);
            }
            catch (Exception ex)
            {
                logger.Error($"SendCarrierTransferWithRetryActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }

    }

    /// <summary>
    /// CARRIER-TRANSFER 실패 시 TC와 Vehicle 할당을 롤백.
    /// TC: QUEUED 상태로 복원, VehicleId 제거
    /// Vehicle: NOTASSIGNED, IDLE 상태로 복원, TransportCommandId 제거
    /// </summary>
    [Activity("ACS.Schedule", "Rollback Vehicle Assignment",
        "CARRIER-TRANSFER 실패 시 TC/Vehicle 할당 롤백")]
    public class RollbackVehicleAssignmentActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "롤백 대상 TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "롤백 대상 Vehicle")]
        public Input<VehicleEx> Vehicle { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicle = Vehicle?.Get(context);

            if (tc == null || vehicle == null)
            {
                logger.Error("RollbackVehicleAssignmentActivity: TC or Vehicle is null");
                return;
            }

            try
            {
                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();

                if (transferManager == null || resourceManager == null)
                {
                    logger.Error("RollbackVehicleAssignmentActivity: Required services not available");
                    return;
                }

                // 다른 워크플로우의 ChangeTracker 스냅샷 의존성을 끊기 위해 fresh 인스턴스 재조회
                TransportCommandEx freshTc = transferManager.GetTransportCommand(tc.JobId) ?? tc;
                VehicleEx freshVehicle = resourceManager.GetVehicle(vehicle.VehicleId) ?? vehicle;

                // Progress-aware guard: TC가 이미 ASSIGNED 단계를 넘어섰으면 롤백 스킵
                // (carrier reply 만 누락되고 차량은 정상 진행 중인 케이스)
                string tcState = freshTc.State ?? string.Empty;
                if (TransportCommandEx.STATE_TRANSFERRING_SOURCE.Equals(tcState, StringComparison.OrdinalIgnoreCase)
                    || TransportCommandEx.STATE_TRANSFERRING_DEST.Equals(tcState, StringComparison.OrdinalIgnoreCase)
                    || TransportCommandEx.STATE_COMPLETED.Equals(tcState, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"RollbackVehicleAssignmentActivity: 롤백 스킵 - TC 이미 진행 중 tc={freshTc.JobId}, state={tcState}");
                    return;
                }

                string vehicleTransferState = freshVehicle.TransferState ?? string.Empty;
                if (VehicleEx.TRANSFERSTATE_ACQUIRE_COMPLETE.Equals(vehicleTransferState, StringComparison.OrdinalIgnoreCase)
                    || VehicleEx.TRANSFERSTATE_TRANSFERING_DEST.Equals(vehicleTransferState, StringComparison.OrdinalIgnoreCase)
                    || VehicleEx.TRANSFERSTATE_DEPOSIT_COMPLETE.Equals(vehicleTransferState, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"RollbackVehicleAssignmentActivity: 롤백 스킵 - Vehicle 이미 이동 중 vehicleId={freshVehicle.VehicleId}, transferState={vehicleTransferState}, tc={freshTc.JobId}");
                    return;
                }

                // TC 롤백: QUEUED 상태로 복원
                freshTc.State = TransportCommandEx.STATE_QUEUED;
                freshTc.VehicleId = null;
                freshTc.AssignedTime = null;
                transferManager.UpdateTransportCommand(freshTc);

                // Vehicle 롤백: NOTASSIGNED + IDLE 상태로 복원 + 슬롯 동반 초기화
                resourceManager.UpdateVehicleTransferState(freshVehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED);
                resourceManager.UpdateVehicleProcessingState(freshVehicle, VehicleEx.PROCESSINGSTATE_IDLE);
                resourceManager.UpdateVehicleTransportCommandId(freshVehicle, "");
                accessor.ResolveOptional<ISlotManagerEx>()?.ReleaseAllByVehicleId(freshVehicle.VehicleId);

                logger.Info($"RollbackVehicleAssignmentActivity: 롤백 완료 - TC {freshTc.JobId} → QUEUED, Vehicle {freshVehicle.VehicleId} → NOTASSIGNED/IDLE");
            }
            catch (Exception ex)
            {
                logger.Error($"RollbackVehicleAssignmentActivity: {ex.Message}", ex);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CheckVehicles Activities
    //  Category: ACS.Schedule
    //
    //  Vehicle EventTime 검사 + DISCONNECT 처리 워크플로우용 Activity들.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 모든 Vehicle의 EventTime을 검사하여 1분 이상 갱신되지 않은 Vehicle을 필터링.
    /// PARK/CHARGE 상태의 Vehicle은 제외.
    /// </summary>
    [Activity("ACS.Schedule", "Check Vehicles EventTime",
        "Vehicle EventTime 검사: 1분 이상 미갱신 Vehicle 필터링 (PARK/CHARGE 제외)")]
    public class CheckVehiclesEventTimeActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "EventTime이 만료된 Vehicle 목록")]
        public Output<ICollection<VehicleEx>> StaleVehicles { get; set; }

        [Output(Description = "만료 Vehicle 수")]
        public Output<int> StaleCount { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                if (resourceManager == null)
                {
                    logger.Error("CheckVehiclesEventTimeActivity: IResourceManagerEx not available");
                    context.Set(StaleVehicles, (ICollection<VehicleEx>)new List<VehicleEx>());
                    context.Set(StaleCount, 0);
                    return;
                }

                IList allVehicles = resourceManager.GetVehicles();
                if (allVehicles == null || allVehicles.Count == 0)
                {
                    context.Set(StaleVehicles, (ICollection<VehicleEx>)new List<VehicleEx>());
                    context.Set(StaleCount, 0);
                    return;
                }

                var staleList = new List<VehicleEx>();
                // EventTime 은 EfCorePersistentDao.SetPropertyValue 에서 UTC 로 변환 저장됨.
                // DateTime.Now(Local) 와 비교 시 KST 오프셋(9h)만큼 elapsed 가 부풀려져 항상 stale 판정됨.
                DateTime currentTime = DateTime.UtcNow;

                foreach (VehicleEx vehicle in allVehicles)
                {
                    // PARK/CHARGE 상태는 검사 대상에서 제외
                    if ("PARK".Equals(vehicle.ProcessingState) || "CHARGE".Equals(vehicle.ProcessingState))
                        continue;

                    // EventTime이 null이면 disconnect 대상
                    if (vehicle.EventTime == default(DateTime))
                    {
                        if (logger.IsDebugEnabled)
                            logger.Debug($"CheckVehiclesEventTimeActivity: EventTime is default, need disconnect - Vehicle [{vehicle.VehicleId}]");
                        staleList.Add(vehicle);
                        continue;
                    }

                    // 60초 이상 갱신되지 않으면 disconnect 대상
                    TimeSpan elapsed = currentTime - vehicle.EventTime;
                    if (elapsed.TotalSeconds > 60)
                    {
                        if (logger.IsDebugEnabled)
                            logger.Debug($"CheckVehiclesEventTimeActivity: EventTime expired ({elapsed.TotalSeconds:F0}s), need disconnect - Vehicle [{vehicle.VehicleId}]");
                        staleList.Add(vehicle);
                    }
                }

                context.Set(StaleVehicles, (ICollection<VehicleEx>)staleList);
                context.Set(StaleCount, staleList.Count);

                if (staleList.Count > 0 && logger.IsDebugEnabled)
                    logger.Debug($"CheckVehiclesEventTimeActivity: {staleList.Count} stale vehicle(s) found");
            }
            catch (Exception ex)
            {
                logger.Error($"CheckVehiclesEventTimeActivity: {ex.Message}", ex);
                context.Set(StaleVehicles, (ICollection<VehicleEx>)new List<VehicleEx>());
                context.Set(StaleCount, 0);
            }
        }
    }

    /// <summary>
    /// 대상 Vehicle 목록의 ConnectionState를 DISCONNECT로 변경.
    /// CommType(NIO/MQTT)에 관계없이 EventTime이 만료된 Vehicle을 disconnect 처리.
    /// </summary>
    [Activity("ACS.Schedule", "Disconnect Vehicles",
        "Vehicle ConnectionState를 DISCONNECT로 변경")]
    public class DisconnectVehiclesActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "DISCONNECT 대상 Vehicle 목록")]
        public Input<ICollection<VehicleEx>> Vehicles { get; set; }

        [Output(Description = "처리 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var vehicleList = Vehicles?.Get(context);
            if (vehicleList == null || vehicleList.Count == 0)
            {
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();

                if (resourceManager == null)
                {
                    logger.Error("DisconnectVehiclesActivity: IResourceManagerEx not available");
                    context.Set(Success, false);
                    return;
                }

                foreach (var vehicle in vehicleList)
                {
                    if (!"DISCONNECT".Equals(vehicle.ConnectionState, StringComparison.OrdinalIgnoreCase))
                    {
                        resourceManager.UpdateVehicleConnectionState(vehicle.VehicleId,
                            VehicleEx.CONNECTIONSTATE_DISCONNECT,
                            "SCHEDULE-CHECKVEHICLES");

                        if (logger.IsDebugEnabled)
                            logger.Debug($"DisconnectVehiclesActivity: Vehicle [{vehicle.VehicleId}] (CommType={vehicle.CommType}) disconnected");
                    }
                }

                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"DisconnectVehiclesActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }
    }

    /// <summary>
    /// SCHEDULE-CHECKVEHICLES 보조 액티비티.
    ///
    /// ProcessingState=RUN 인데 RunState=STOP 으로 정지해 있는 vehicle 을 찾아
    /// 할당된 TC 와 정합이 맞으면 RAIL-CARRIERTRANSFER 를 재전송한다.
    ///
    /// 발동 조건 (모두 만족해야 재전송):
    ///   - vehicle.ProcessingState == RUN
    ///   - vehicle.RunState == STOP
    ///   - vehicle.AlarmState == NOALARM   (ALARM 중인 vehicle 은 이동 명령 보내지 않음)
    ///   - vehicle.ConnectionState == CONNECT  (DISCONNECT vehicle 은 이동 명령 보내지 않음)
    ///   - !string.IsNullOrEmpty(vehicle.TransportCommandId)
    ///   - tc = GetTransportCommand(vehicle.TransportCommandId) 가 존재
    ///   - tc.VehicleId == vehicle.VehicleId
    ///   - (vehicle.TransferState, tc.State) ∈ 다음 매칭 중 하나
    ///       (ASSIGNED,           ASSIGNED 또는 TRANSFERRING_SOURCE) → useSource=true,  jobType=UNLOAD
    ///       (TRANSFERING_DEST,   TRANSFERRING_DEST)                  → useSource=false, jobType=LOAD
    ///
    /// 메시지 빌드/송신은 SendCarrierTransferActivity 와 동일하게 CarrierTransferJsonBuilder 사용.
    /// 응답 대기/재시도는 하지 않는 단순 송신 (이 vehicle 은 이미 명령 중인 상태이므로 단순 재푸시).
    /// </summary>
    [Activity("ACS.Schedule", "Recover Stuck Vehicles",
        "RUN+STOP 상태로 멈춘 vehicle 에 RAIL-CARRIERTRANSFER 재전송")]
    public class RecoverStuckVehiclesActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();
                if (resourceManager == null || transferManager == null || messageManager == null)
                {
                    logger.Error("RecoverStuckVehiclesActivity: 필수 서비스 해결 실패");
                    return;
                }

                IList allVehicles = resourceManager.GetVehicles();
                if (allVehicles == null || allVehicles.Count == 0) return;

                int recovered = 0;
                foreach (VehicleEx vehicle in allVehicles)
                {
                    if (!VehicleEx.RUNSTATE_STOP.Equals(vehicle.RunState, StringComparison.OrdinalIgnoreCase))
                        continue;
                    // ALARM 상태에서는 이동 명령 재전송하지 않음
                    if (!VehicleEx.ALARMSTATE_NOALARM.Equals(vehicle.AlarmState, StringComparison.OrdinalIgnoreCase))
                        continue;
                    // DISCONNECT 차량에는 재전송하지 않음 (연결 끊긴 vehicle 에 명령 보내지 않음)
                    if (!VehicleEx.CONNECTIONSTATE_CONNECT.Equals(vehicle.ConnectionState, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.IsNullOrEmpty(vehicle.TransportCommandId))
                        continue;

                    // ─── CHARGEMOVE 재전송 분기 ───
                    // ChargeJob 은 dispatch 시 ProcessingState 를 IDLE 그대로 두므로
                    // 일반 RUN+STOP 필터에 안 잡힌다. 별도 분기로 처리.
                    if (VehicleEx.PROCESSINGSTATE_IDLE.Equals(vehicle.ProcessingState, StringComparison.OrdinalIgnoreCase))
                    {
                        TransportCommandEx chargeTc = transferManager.GetTransportCommand(vehicle.TransportCommandId);
                        if (chargeTc != null
                            && TransportCommandEx.JOBTYPE_CHARGEMOVE.Equals(chargeTc.JobType, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(chargeTc.VehicleId, vehicle.VehicleId, StringComparison.OrdinalIgnoreCase))
                        {
                            var pathManager = accessor.Resolve<IPathManagerEx>();
                            var destLoc = pathManager?.GetLocationByLocationId(chargeTc.Dest);
                            string destNodeId = destLoc?.StationId;

                            if (!string.IsNullOrEmpty(destNodeId)
                                && !string.Equals(vehicle.CurrentNodeId, destNodeId, StringComparison.OrdinalIgnoreCase))
                            {
                                string chargeJson = CarrierTransferJsonBuilder.Build(chargeTc, vehicle.VehicleId,
                                    TransportCommandEx.JOBTYPE_CHARGEMOVE, useSource: false, resourceManager, logger);
                                if (!string.IsNullOrEmpty(chargeJson))
                                {
                                    messageManager.SendCarrierTransferJson(chargeJson);
                                    recovered++;
                                    logger.Info($"RecoverStuckVehiclesActivity: CHARGEMOVE 재전송 vehicleId={vehicle.VehicleId}, " +
                                                $"tc={chargeTc.JobId}, currentNode={vehicle.CurrentNodeId}, destNode={destNodeId}");
                                }
                                else
                                {
                                    logger.Error($"RecoverStuckVehiclesActivity: CHARGEMOVE JSON 빌드 실패 vehicleId={vehicle.VehicleId}, tc={chargeTc.JobId}");
                                }
                            }
                        }
                        continue;   // IDLE 분기 처리 후 일반 Job 매칭 단계 진입 방지
                    }

                    if (!VehicleEx.PROCESSINGSTATE_RUN.Equals(vehicle.ProcessingState, StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    if (vehicle.CurrentNodeId.Equals(vehicle.AcsDestNodeId, StringComparison.OrdinalIgnoreCase))
                        continue;   // 이미 도착한 상태면 재전송하지 않음

                    // EXCHANGE(v2) S7 배칭 트립: TransportCommandId="TRIP..." 는 TC 직조회 불가 —
                    // 활성 EXCHANGE TC 들의 STEP 조합에서 현재 진행 leg 를 유도해 그 TC 만 재푸시한다.
                    if (ExchangeTour.IsTripId(vehicle.TransportCommandId))
                    {
                        var tripJobs = new List<TransportCommandEx>();
                        foreach (var item in transferManager.GetActiveExchangeTransportCommandsByVehicleId(vehicle.VehicleId))
                            if (item is TransportCommandEx t)
                                tripJobs.Add(t);

                        var tripSteps = tripJobs.ConvertAll(j => (j.JobId, ExchangeSteps.GetStep(j.AdditionalInfo)));
                        var tourAction = ExchangeTour.NextAfter(tripSteps);
                        var legTc = tourAction.JobId != null
                            ? tripJobs.Find(j => string.Equals(j.JobId, tourAction.JobId, StringComparison.OrdinalIgnoreCase))
                            : null;
                        if (legTc != null)
                        {
                            if (RecoverExchangeVehicle(vehicle, legTc, resourceManager, messageManager))
                                recovered++;
                        }
                        else
                        {
                            logger.Warn($"RecoverStuckVehiclesActivity: 트립 활성 TC 없음(action={tourAction.Kind}) — " +
                                        $"vehicleId={vehicle.VehicleId}, transportCommandId={vehicle.TransportCommandId} (reset 필요 가능)");
                        }
                        continue;
                    }

                    TransportCommandEx tc = transferManager.GetTransportCommand(vehicle.TransportCommandId);
                    if (tc == null)
                    {
                        logger.Warn($"RecoverStuckVehiclesActivity: TC 없음 vehicleId={vehicle.VehicleId}, transportCommandId={vehicle.TransportCommandId}");
                        continue;
                    }

                    // EXCHANGE(v2) 분기 (v0.3 하이브리드): TC 상태는 여정 내내 EXCHANGE_ASSIGNED 라 아래 일반 매칭에 걸리지 않으므로
                    // STEP/ACT 로 현재 이동 구간을 유도해 재푸시한다. 설비 게이트 대기(ACT 설정)·설비 앞 대기(이미 mid)·30/40/60 은 재푸시 안 함.
                    if (TransportCommandEx.JOBTYPE_EXCHANGE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                    {
                        if (RecoverExchangeVehicle(vehicle, tc, resourceManager, messageManager))
                            recovered++;
                        continue;
                    }

                    if (!string.Equals(tc.VehicleId, vehicle.VehicleId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Vehicle 측 (TransportCommandId, TransferState) 을 진실 원천으로 간주하여 TC 재연결.
                        // Rollback 의 잘못된 발동이나 EF silent drop 으로 TC.VehicleId 가 비워진 케이스 자동 복구.
                        bool canHeal = string.Equals(vehicle.TransportCommandId, tc.JobId, StringComparison.OrdinalIgnoreCase)
                            && (VehicleEx.TRANSFERSTATE_TRANSFERING_DEST.Equals(vehicle.TransferState, StringComparison.OrdinalIgnoreCase)
                                || VehicleEx.TRANSFERSTATE_ASSIGNED.Equals(vehicle.TransferState, StringComparison.OrdinalIgnoreCase));

                        if (!canHeal)
                        {
                            logger.Warn($"RecoverStuckVehiclesActivity: TC.VehicleId 불일치 (자동 보정 불가) vehicleId={vehicle.VehicleId}, tc={tc.JobId}, tc.VehicleId={tc.VehicleId}, vehicleTransferState={vehicle.TransferState}");
                            continue;
                        }

                        string oldVehicleId = tc.VehicleId;
                        string oldState = tc.State;
                        tc.VehicleId = vehicle.VehicleId;
                        if (VehicleEx.TRANSFERSTATE_TRANSFERING_DEST.Equals(vehicle.TransferState, StringComparison.OrdinalIgnoreCase)
                            && !TransportCommandEx.STATE_TRANSFERRING_DEST.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
                        {
                            tc.State = TransportCommandEx.STATE_TRANSFERRING_DEST;
                            if (tc.LoadedTime == null) tc.LoadedTime = DateTime.Now;
                        }
                        else if (VehicleEx.TRANSFERSTATE_ASSIGNED.Equals(vehicle.TransferState, StringComparison.OrdinalIgnoreCase)
                            && !TransportCommandEx.STATE_ASSIGNED.Equals(tc.State, StringComparison.OrdinalIgnoreCase)
                            && !TransportCommandEx.STATE_TRANSFERRING_SOURCE.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
                        {
                            tc.State = TransportCommandEx.STATE_ASSIGNED;
                        }

                        transferManager.UpdateTransportCommand(tc);
                        logger.Warn($"RecoverStuckVehiclesActivity: TC 재연결 완료 vehicleId={vehicle.VehicleId}, tc={tc.JobId}, oldVehicleId={oldVehicleId}, oldState={oldState}, newState={tc.State}");
                    }

                    bool useSource;
                    string jobType;
                    if (VehicleEx.TRANSFERSTATE_ASSIGNED.Equals(vehicle.TransferState, StringComparison.OrdinalIgnoreCase)
                        && (TransportCommandEx.STATE_ASSIGNED.Equals(tc.State, StringComparison.OrdinalIgnoreCase)
                            || TransportCommandEx.STATE_TRANSFERRING_SOURCE.Equals(tc.State, StringComparison.OrdinalIgnoreCase)))
                    {
                        useSource = true;
                        jobType = TransportCommandEx.JOBTYPE_UNLOAD;
                    }
                    else if (VehicleEx.TRANSFERSTATE_TRANSFERING_DEST.Equals(vehicle.TransferState, StringComparison.OrdinalIgnoreCase)
                        && TransportCommandEx.STATE_TRANSFERRING_DEST.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
                    {
                        useSource = false;
                        jobType = TransportCommandEx.JOBTYPE_LOAD;
                    }
                    else
                    {
                        // 상태 매칭 실패 — 재전송 대상 아님
                        continue;
                    }

                    string json = CarrierTransferJsonBuilder.Build(tc, vehicle.VehicleId, jobType, useSource, resourceManager, logger);
                    if (string.IsNullOrEmpty(json))
                    {
                        logger.Error($"RecoverStuckVehiclesActivity: JSON 빌드 실패 vehicleId={vehicle.VehicleId}, tc={tc.JobId}");
                        continue;
                    }

                    messageManager.SendCarrierTransferJson(json);
                    recovered++;
                    logger.Info($"RecoverStuckVehiclesActivity: RAIL-CARRIERTRANSFER 재전송 vehicleId={vehicle.VehicleId}, tc={tc.JobId}, " +
                                $"transferState={vehicle.TransferState}, tcState={tc.State}, jobType={jobType}, useSource={useSource}, " +
                                $"acsDestNodeId={vehicle.AcsDestNodeId}");
                }

                if (recovered > 0)
                    logger.Info($"RecoverStuckVehiclesActivity: 총 {recovered}대 재전송 완료");
            }
            catch (Exception ex)
            {
                logger.Error($"RecoverStuckVehiclesActivity: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// EXCHANGE TC stuck 복구: ExchangeSteps.ResolveRecoverySegment 로 구간을 유도해 RAIL-CARRIERTRANSFER 를 재푸시한다.
        ///  - STEP=10 → Origin(tc.Source) UNLOAD, amrSlot=LOADSLOT
        ///  - STEP=20 &amp;&amp; ACT 빈값 &amp;&amp; 현재≠mid → Mid(MidLoc:MidPortId) EXCHANGE, amrSlot=LOADSLOT
        ///  - STEP=50 → Dest(tc.Dest) LOAD, amrSlot=UNLOADSLOT
        /// 재푸시 시 AcsDestNodeId 도 해당 waypoint StationId 로 재설정한다. 재푸시했으면 true.
        /// </summary>
        private static bool RecoverExchangeVehicle(VehicleEx vehicle, TransportCommandEx tc,
            IResourceManagerEx resourceManager, IMessageManagerEx messageManager)
        {
            if (!TransportCommandEx.STATE_EXCHANGE_ASSIGNED.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warn($"RecoverStuckVehiclesActivity[EXCHANGE]: 예상외 TC 상태 — 재푸시 안 함 tc={tc.JobId}, state={tc.State}");
                return false;
            }

            int step = ExchangeSteps.GetStep(tc.AdditionalInfo);
            string act = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_ACT);
            string midLocationId = ExchangeSteps.BuildMidLocationId(tc.MidLoc, tc.MidPortId);
            string midStationId = ExchangeTransHandlers.ResolveStationId(resourceManager, midLocationId);

            var seg = ExchangeSteps.ResolveRecoverySegment(step, act, vehicle.CurrentNodeId, midStationId);
            if (seg == null)
            {
                logger.Info($"RecoverStuckVehiclesActivity[EXCHANGE]: 재푸시 대상 아님 tc={tc.JobId}, step={step}, act='{act}', " +
                            $"currentNode={vehicle.CurrentNodeId}, mid={midStationId}");
                return false;
            }

            string targetLocationId = seg.Target == ExchangeSteps.TARGET_SOURCE ? tc.Source
                                    : seg.Target == ExchangeSteps.TARGET_MID ? midLocationId
                                    : tc.Dest;
            string targetStationId = ExchangeTransHandlers.ResolveStationId(resourceManager, targetLocationId);
            if (string.IsNullOrEmpty(targetStationId))
            {
                logger.Error($"RecoverStuckVehiclesActivity[EXCHANGE]: waypoint StationId 조회 실패 tc={tc.JobId}, target={seg.Target}, loc={targetLocationId}");
                return false;
            }
            string slot = ExchangeInfo.Get(tc.AdditionalInfo, seg.SlotKey);

            resourceManager.UpdateVehicleAcsDestNodeId(vehicle, targetStationId, "SCHEDULE-CHECKVEHICLES");
            vehicle.AcsDestNodeId = targetStationId;
            ExchangeTransHandlers.SendCarrierTransfer(messageManager, resourceManager, tc, vehicle.VehicleId,
                seg.JobType, targetLocationId, slot);

            logger.Info($"RecoverStuckVehiclesActivity[EXCHANGE]: RAIL-CARRIERTRANSFER 재전송 vehicleId={vehicle.VehicleId}, tc={tc.JobId}, " +
                        $"step={step}, target={seg.Target}({targetLocationId}→{targetStationId}), jobType={seg.JobType}, amrSlot={slot}");
            return true;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ChargeJob Activity
    //  Category: ACS.Schedule
    //
    //  SCHEDULE-CHARGEJOB 워크플로우 단일 액티비티.
    //  Bay 단위로 빈 충전 슬롯(Location.Type=CHARGE) + IDLE 후보 vehicle 매칭 →
    //  배터리 가장 낮은 1대에 CHARGEMOVE TC 생성 + RAIL-CARRIERTRANSFER 송신.
    //
    //  Daemon AwakeChargeTransportJob 이 20초/Bay 주기로 트리거.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// SCHEDULE-CHARGEJOB 디스패치.
    ///
    /// 동작:
    ///   1. 메시지에서 BayId 추출
    ///   2. CacheManager.GetChargeLocationViewsByBayId(bayId) 로 충전 슬롯 후보 조회
    ///      - 점유 vehicle 없고 (GetVehiclesByCurrentNode 결과 0)
    ///      - 점유 TC 도 없는 (CheckTransportCommandByDestLocationId == false)
    ///        첫 슬롯 선정
    ///   3. 같은 Bay 의 vehicle 들 중 후보 조건 만족 + 배터리 최저 1대 선정
    ///   4. CHARGEMOVE TC 생성 (CreateRechargeTransportCommand)
    ///   5. vehicle.TransportCommandId 갱신
    ///   6. CarrierTransferJsonBuilder 로 JSON 빌드 후 SendCarrierTransferJson 송신 (단발)
    /// </summary>
    [Activity("ACS.Schedule", "Dispatch Charge Job",
        "Bay 별 빈 충전 슬롯에 배터리 낮은 IDLE vehicle 1대 dispatch (CHARGEMOVE)")]
    public class DispatchChargeJobActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var cacheManager = accessor?.Resolve<ICacheManagerEx>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();

                if (resourceManager == null || transferManager == null
                    || cacheManager == null || messageManager == null)
                {
                    logger.Error("DispatchChargeJobActivity: 필수 서비스 해결 실패");
                    return;
                }

                string bayId = ExtractBayIdFromInput(context);
                if (string.IsNullOrEmpty(bayId))
                {
                    logger.Warn("DispatchChargeJobActivity: BayId 가 메시지에 없음");
                    return;
                }

                // 1. 빈 충전 슬롯 찾기
                List<LocationViewEx> chargeLocations = cacheManager.GetChargeLocationViewsByBayId(bayId);
                if (chargeLocations == null || chargeLocations.Count == 0)
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug($"DispatchChargeJobActivity: bayId={bayId} 에 충전 슬롯 없음");
                    return;
                }

                LocationViewEx availableSlot = null;
                foreach (LocationViewEx loc in chargeLocations)
                {
                    IList occupied = resourceManager.GetVehiclesByCurrentNode(loc.StationId);
                    bool nodeBusy = occupied != null && occupied.Count > 0;
                    bool tcBusy = transferManager.CheckTransportCommandByDestLocationId(loc.LocationId);

                    if (!nodeBusy && !tcBusy)
                    {
                        availableSlot = loc;
                        break;
                    }
                }
                if (availableSlot == null)
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug($"DispatchChargeJobActivity: bayId={bayId} 의 충전 슬롯 모두 사용 중");
                    return;
                }

                // 2. 후보 vehicle 선정 (배터리 최저)
                IList vehiclesInBay = resourceManager.GetVehiclesByBayId(bayId);
                if (vehiclesInBay == null || vehiclesInBay.Count == 0) return;

                VehicleEx candidate = null;
                int lowestBattery = int.MaxValue;
                foreach (VehicleEx v in vehiclesInBay)
                {
                    if (!VehicleEx.PROCESSINGSTATE_IDLE.Equals(v.ProcessingState, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!VehicleEx.RUNSTATE_STOP.Equals(v.RunState, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!VehicleEx.TRANSFERSTATE_NOTASSIGNED.Equals(v.TransferState, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!VehicleEx.INSTALL_INSTALLED.Equals(v.Installed, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(v.TransportCommandId)) continue;
                    if (VehicleEx.CONNECTIONSTATE_DISCONNECT.Equals(v.ConnectionState, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!VehicleEx.ALARMSTATE_NOALARM.Equals(v.AlarmState, StringComparison.OrdinalIgnoreCase)) continue;

                    if (v.BatteryRate < lowestBattery)
                    {
                        lowestBattery = v.BatteryRate;
                        candidate = v;
                    }
                }
                if (candidate == null)
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug($"DispatchChargeJobActivity: bayId={bayId} 에 충전 후보 vehicle 없음");
                    return;
                }

                // 3. CHARGEMOVE TC 생성 (기존 TransferServiceEx.CreateRechargeTransportCommand 패턴과 동일)
                string commandId = "C" + candidate.VehicleId + DateTime.Now.ToString("yyyyMMddHHmmss");
                var tc = new TransportCommandEx
                {
                    JobId = commandId,
                    CarrierId = commandId,
                    State = TransportCommandEx.STATE_CREATED,
                    Dest = availableSlot.LocationId,
                    VehicleId = candidate.VehicleId,
                    BayId = bayId,
                    CreateTime = DateTime.Now,
                    AssignedTime = DateTime.Now,
                    JobType = TransportCommandEx.JOBTYPE_CHARGEMOVE,
                    LoadedTime = null,
                    UnloadArrivedTime = null,
                    UnloadedTime = null,
                    LoadingTime = null,
                    UnloadingTime = null,
                    CompletedTime = null,
                    StartedTime = null
                };

                TransportCommandEx createdTc = transferManager.CreateRechargeTransportCommand(tc);
                if (createdTc == null)
                {
                    logger.Error($"DispatchChargeJobActivity: TC 생성 실패 vehicleId={candidate.VehicleId}, bayId={bayId}");
                    return;
                }

                // 4. vehicle 측 TC 연결 (다음 사이클에서 같은 vehicle 재선정 방지)
                resourceManager.UpdateVehicleTransportCommandId(candidate, tc.JobId);

                // 5. RAIL-CARRIERTRANSFER JSON 빌드 & 송신 (단발, 응답 대기 없음)
                string json = CarrierTransferJsonBuilder.Build(tc, candidate.VehicleId,
                    TransportCommandEx.JOBTYPE_CHARGEMOVE, useSource: false, resourceManager, logger);
                if (string.IsNullOrEmpty(json))
                {
                    logger.Error($"DispatchChargeJobActivity: JSON 빌드 실패 vehicleId={candidate.VehicleId}, tc={tc.JobId}");
                    return;
                }

                messageManager.SendCarrierTransferJson(json);

                logger.Info($"DispatchChargeJobActivity: 충전 이동 dispatched bayId={bayId}, vehicleId={candidate.VehicleId}, " +
                            $"batteryRate={candidate.BatteryRate}, chargeLocationId={availableSlot.LocationId}, " +
                            $"chargeStationId={availableSlot.StationId}, tc={tc.JobId}");
            }
            catch (Exception ex)
            {
                logger.Error($"DispatchChargeJobActivity: {ex.Message}", ex);
            }
        }

        private string ExtractBayIdFromInput(ActivityExecutionContext context)
        {
            try
            {
                if (!context.WorkflowExecutionContext.Input.TryGetValue("Arguments", out var argsObj))
                    return "";
                var args = argsObj as object[];
                if (args == null || args.Length == 0)
                    return "";
                string jsonStr = args[0] as string;
                if (string.IsNullOrEmpty(jsonStr))
                    return "";
                var msg = JsonSerializer.Deserialize<DaemonScheduleMessage>(jsonStr);
                return msg?.Data?.BayId ?? "";
            }
            catch (Exception ex)
            {
                logger.Warn($"DispatchChargeJobActivity: bayId 추출 실패: {ex.Message}");
                return "";
            }
        }
    }
}
