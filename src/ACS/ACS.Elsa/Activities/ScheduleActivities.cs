using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Autofac;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Path;
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

                // 상태 검증: IDLE + CONNECT + TC 미할당 + TransferState NOTASSIGNED
                // ProcessingState/TransportCommandId/TransferState 세 가지가 모두 정합해야 함.
                // 어느 하나라도 비정합이면 race 또는 외부 상태 오염으로 보고 후보 제외.
                if (vehicle.ProcessingState != VehicleEx.PROCESSINGSTATE_IDLE ||
                    vehicle.ConnectionState != VehicleEx.CONNECTIONSTATE_CONNECT ||
                    !string.IsNullOrEmpty(vehicle.TransportCommandId) ||
                    (vehicle.TransferState != null
                     && vehicle.TransferState != VehicleEx.TRANSFERSTATE_NOTASSIGNED))
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

        // BayId 단위 SemaphoreSlim — 같은 Bay에서 동시에 진입하는 SCHEDULE-QUEUEJOB 워크플로우들의
        // Vehicle 할당 구간을 직렬화하여 고수준 가시성과 디버깅 용이성 확보.
        // DB 원자 UPDATE(TryAssignVehicleAtomic)가 정합성을 보장하므로 본 락은 보조 안전망.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _bayLocks
            = new ConcurrentDictionary<string, SemaphoreSlim>();

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

            var bayKey = string.IsNullOrEmpty(tc.BayId) ? "_default_" : tc.BayId;
            var sema = _bayLocks.GetOrAdd(bayKey, _ => new SemaphoreSlim(1, 1));
            sema.Wait();
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

                // 1) Vehicle 원자적 conditional UPDATE — 전제(IDLE + TC 비어있음) 성립 시에만 잡힘
                bool won = resourceManager.TryAssignVehicleAtomic(vehicle.VehicleId, tc.JobId, "ASSIGN");
                if (!won)
                {
                    logger.Warn($"AssignVehicleToTransportCommandActivity: race detected — Vehicle {vehicle.VehicleId} no longer assignable for TC {tc.JobId}");
                    context.Set(Success, false);
                    return;
                }

                // 2) TC 업데이트 — Vehicle을 잡았으므로 자유롭게 TC 측 상태 갱신
                tc.VehicleId = vehicle.VehicleId;
                tc.State = TransportCommandEx.STATE_ASSIGNED;
                tc.AssignedTime = DateTime.Now;
                transferManager.UpdateTransportCommand(tc);

                // 3) 메모리 객체에도 동기화 — 후속 Activity가 이 vehicle 객체를 그대로 사용
                vehicle.TransportCommandId = tc.JobId;
                vehicle.TransferState = VehicleEx.TRANSFERSTATE_ASSIGNED;
                vehicle.ProcessingState = VehicleEx.PROCESSINGSTATE_RUN;

                logger.Info($"AssignVehicleToTransportCommandActivity: TC {tc.JobId} → Vehicle {vehicle.VehicleId} (atomic)");
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"AssignVehicleToTransportCommandActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
            finally
            {
                sema.Release();
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
    /// destPortId / destNodeId / portType(EQP/MAT)을 채운다.
    /// </summary>
    internal static class CarrierTransferJsonBuilder
    {
        public static string Build(TransportCommandEx tc, string vehicleId, string jobType,
            bool useSource, IResourceManagerEx resourceManager, Logger logger)
        {
            try
            {
                string src = useSource ? tc.Source : tc.Dest;

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

                // nodeId 조회
                string nodeId = "";
                try
                {
                    var location = resourceManager?.GetLocationByLocationId(portId);
                    if (location != null)
                        nodeId = location.StationId ?? "";
                }
                catch (Exception ex)
                {
                    logger?.Error($"CarrierTransferJsonBuilder: location 조회 실패 portId={portId} - {ex.Message}");
                }

                // portType (EQP / MAT) 조회
                string portType = "";
                try
                {
                    if (!string.IsNullOrEmpty(unitName) && !string.IsNullOrEmpty(machineName))
                    {
                        var unit = resourceManager?.GetUnitByName(unitName, machineName);
                        if (unit is ACS.Core.Resource.Model.Factory.Unit.Port port)
                        {
                            portType = port.PortType ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.Error($"CarrierTransferJsonBuilder: Port 조회 실패 machine={machineName}, unit={unitName} - {ex.Message}");
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
                        Port = tc.PortId ?? "",
                        JobType = string.IsNullOrEmpty(jobType) ? (tc.JobType ?? "") : jobType,
                        PortType = portType,
                        ResultCode = ""
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

                // TC 롤백: QUEUED 상태로 복원
                tc.State = TransportCommandEx.STATE_QUEUED;
                tc.VehicleId = null;
                tc.AssignedTime = null;
                transferManager.UpdateTransportCommand(tc);

                // Vehicle 롤백: NOTASSIGNED + IDLE 상태로 복원
                resourceManager.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED);
                resourceManager.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_IDLE);
                resourceManager.UpdateVehicleTransportCommandId(vehicle, "");

                logger.Info($"RollbackVehicleAssignmentActivity: 롤백 완료 - TC {tc.JobId} → QUEUED, Vehicle {vehicle.VehicleId} → NOTASSIGNED/IDLE");
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
                DateTime currentTime = DateTime.Now;

                foreach (VehicleEx vehicle in allVehicles)
                {
                    // PARK/CHARGE 상태는 검사 대상에서 제외
                    if ("PARK".Equals(vehicle.ProcessingState) || "CHARGE".Equals(vehicle.ProcessingState))
                        continue;

                    // EventTime이 null이면 disconnect 대상
                    if (vehicle.EventTime == default(DateTime))
                    {
                        logger.Info($"CheckVehiclesEventTimeActivity: EventTime is default, need disconnect - Vehicle [{vehicle.VehicleId}]");
                        staleList.Add(vehicle);
                        continue;
                    }

                    // 60초 이상 갱신되지 않으면 disconnect 대상
                    TimeSpan elapsed = currentTime - vehicle.EventTime;
                    if (elapsed.TotalSeconds > 60)
                    {
                        logger.Info($"CheckVehiclesEventTimeActivity: EventTime expired ({elapsed.TotalSeconds:F0}s), need disconnect - Vehicle [{vehicle.VehicleId}]");
                        staleList.Add(vehicle);
                    }
                }

                context.Set(StaleVehicles, (ICollection<VehicleEx>)staleList);
                context.Set(StaleCount, staleList.Count);

                if (staleList.Count > 0)
                    logger.Info($"CheckVehiclesEventTimeActivity: {staleList.Count} stale vehicle(s) found");
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

                        logger.Info($"DisconnectVehiclesActivity: Vehicle [{vehicle.VehicleId}] (CommType={vehicle.CommType}) disconnected");
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
}
