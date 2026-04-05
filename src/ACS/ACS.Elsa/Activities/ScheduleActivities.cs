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
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Core.Message.Model;
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
                var interfaceServiceType = System.Type.GetType("ACS.Service.InterfaceServiceEx, ACS.Service");
                if (interfaceServiceType == null)
                {
                    logger.Error("SendJobReportStartActivity: InterfaceServiceEx type not found");
                    context.Set(Success, false);
                    return;
                }

                var scope = accessor?.Resolve<ILifetimeScope>();
                var interfaceService = scope?.Resolve(interfaceServiceType);
                if (interfaceService == null)
                {
                    logger.Error("SendJobReportStartActivity: InterfaceServiceEx not resolved");
                    context.Set(Success, false);
                    return;
                }

                var method = interfaceServiceType.GetMethod("SendJobReportToHost");
                method?.Invoke(interfaceService, new object[]
                {
                    "START", tc.JobId, vehicleId, tc.JobType ?? "", tc.Description ?? ""
                });

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
                var interfaceServiceType = System.Type.GetType("ACS.Service.InterfaceServiceEx, ACS.Service");
                if (interfaceServiceType == null)
                {
                    logger.Error("SendCarrierTransferActivity: InterfaceServiceEx type not found");
                    context.Set(Success, false);
                    return;
                }

                var scope = accessor?.Resolve<ILifetimeScope>();
                var interfaceService = scope?.Resolve(interfaceServiceType);
                if (interfaceService == null)
                {
                    logger.Error("SendCarrierTransferActivity: InterfaceServiceEx not resolved");
                    context.Set(Success, false);
                    return;
                }

                // TransferMessageEx 빌드
                var transferMessage = new TransferMessageEx();
                transferMessage.TransportCommandId = tc.JobId;
                transferMessage.VehicleId = vehicleId;
                transferMessage.Priority = tc.Priority;
                transferMessage.CarrierType = tc.CarrierId ?? "";

                if (!string.IsNullOrEmpty(tc.Dest))
                {
                    int colonIdx = tc.Dest.IndexOf(':');
                    if (colonIdx >= 0)
                    {
                        transferMessage.DestMachine = tc.Dest.Substring(0, colonIdx);
                        transferMessage.DestUnit = tc.Dest.Substring(colonIdx + 1);
                    }
                    else
                    {
                        transferMessage.DestMachine = tc.Dest;
                        transferMessage.DestUnit = "";
                    }
                }

                var method = interfaceServiceType.GetMethod("SendCarrierTransferToEi");
                method?.Invoke(interfaceService, new object[] { transferMessage });

                logger.Info($"SendCarrierTransferActivity: RAIL-CARRIERTRANSFER sent for TC {tc.JobId}");
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"SendCarrierTransferActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }
    }
}
