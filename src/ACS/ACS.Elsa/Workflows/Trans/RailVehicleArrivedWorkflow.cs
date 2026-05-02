using System;
using System.Collections.Generic;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEARRIVED 워크플로우.
    ///
    /// EI(AMR 컨트롤러)가 AMR이 moveCmd 목적지 노드에 도달한 시점(LOAD/UNLOAD 시작 직전)을
    /// Trans에 보고할 때 수신.
    /// jobType에 따라 source / dest 도착으로 분기:
    ///   UNLOAD → SOURCE_ARRIVED  (TC.State, Vehicle.TransferState=ASSIGNED_ACQUIRING)
    ///   LOAD   → DEST_ARRIVED    (TC.State, Vehicle.TransferState=ASSIGNED_DEPOSITING)
    /// 그 후 도착 노드의 Location.Type에 따라 Host로 JOBREPORT(ARRIVED)를 송신:
    ///   EQP    → 송신
    ///   BUFFER → 자재 포트이므로 송신 생략
    ///
    /// Arguments: [string jsonMessage]
    /// </summary>
    public class RailVehicleArrivedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEARRIVED";
            builder.Name = "RAIL-VEHICLEARRIVED";
            builder.Description = "AMR 도착 보고 수신 → jobType별 source/dest 상태 전이 → EQP 도착 시 Host JOBREPORT(ARRIVED)";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleVehicleArrivedActivity()
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEARRIVED JSON 수신 후 상태 전이 + JobReport 송신.
    /// ACS.Service 계층은 호출하지 않고 Manager 계층 primitive 만 직접 사용한다.
    /// </summary>
    [Activity("ACS.Trans", "Handle Vehicle Arrived",
        "AMR 도착 수신 → TC/Vehicle 상태 전이 → EQP 노드 도착 시 Host JOBREPORT(ARRIVED) 전송")]
    public class HandleVehicleArrivedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleVehicleArrivedActivity));

        private const string MsgName = "RAIL-VEHICLEARRIVED";

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleVehicleArrivedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleVehicleArrivedActivity: JSON 메시지가 null입니다.");
                    return;
                }

                string commandId = null;
                string vehicleId = null;
                string jobType = null;
                string nodeId = null;
                string resultCode = null;
                string errorCode = null;
                string errorMessage = null;

                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("vehicleId", out var vid))
                            vehicleId = vid.GetString();
                        // EI 측에서 PascalCase("JobType")로 직렬화하므로 두 형태 모두 허용.
                        if (dataEl.TryGetProperty("JobType", out var jtP) || dataEl.TryGetProperty("jobType", out jtP))
                            jobType = jtP.GetString();
                        if (dataEl.TryGetProperty("nodeId", out var nid))
                            nodeId = nid.GetString();
                        if (dataEl.TryGetProperty("resultCode", out var rc))
                            resultCode = rc.GetString();
                        if (dataEl.TryGetProperty("errorCode", out var ec))
                            errorCode = ec.ValueKind == JsonValueKind.String ? ec.GetString() : ec.ToString();
                        if (dataEl.TryGetProperty("errorMessage", out var em))
                            errorMessage = em.GetString();
                    }
                }

                logger.Info($"HandleVehicleArrivedActivity: commandId={commandId}, vehicleId={vehicleId}, jobType={jobType}, nodeId={nodeId}, resultCode={resultCode}");

                if (string.IsNullOrEmpty(commandId))
                {
                    logger.Error("HandleVehicleArrivedActivity: commandId가 없습니다.");
                    return;
                }

                if (!"OK".Equals(resultCode, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleVehicleArrivedActivity: 도착 실패 보고 - commandId={commandId}, resultCode={resultCode}, errorCode={errorCode}, errorMessage={errorMessage}. 후속 처리 생략.");
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();

                if (resourceManager == null || transferManager == null || messageManager == null)
                {
                    logger.Error("HandleVehicleArrivedActivity: 필수 서비스 해결 실패 " +
                        $"(resourceManager={resourceManager != null}, transferManager={transferManager != null}, messageManager={messageManager != null})");
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommand(commandId);
                if (tc == null)
                {
                    logger.Error($"HandleVehicleArrivedActivity: TC를 찾을 수 없음. commandId={commandId}");
                    return;
                }

                string effectiveVehicleId = !string.IsNullOrEmpty(vehicleId) ? vehicleId : (tc.VehicleId ?? "");
                if (string.IsNullOrEmpty(effectiveVehicleId))
                {
                    logger.Error($"HandleVehicleArrivedActivity: VehicleId 없음. commandId={commandId}");
                    return;
                }

                // jobType이 reply에 없으면 TC에서 보완.
                string effectiveJobType = !string.IsNullOrEmpty(jobType) ? jobType : (tc.JobType ?? "");
                bool isSourceArrival = "UNLOAD".Equals(effectiveJobType, StringComparison.OrdinalIgnoreCase);
                bool isDestArrival = "LOAD".Equals(effectiveJobType, StringComparison.OrdinalIgnoreCase);
                if (!isSourceArrival && !isDestArrival)
                {
                    logger.Warn($"HandleVehicleArrivedActivity: jobType={effectiveJobType} 은 라우팅 대상 아님. commandId={commandId}");
                    return;
                }

                // Step 1: Vehicle 존재 확인
                VehicleEx vehicle = CheckVehicle(resourceManager, effectiveVehicleId);
                if (vehicle == null) return;

                // Step 2: TC.VehicleEvent = RAIL-VEHICLEARRIVED
                UpdateTransportCommandVehicleEvent(transferManager, tc);

                // Step 3: TC.State 업데이트 (ARRIVED_SOURCE / ARRIVED_DEST) + 도착 시각 기록
                UpdateTransportCommandArrivedState(transferManager, tc, isSourceArrival);

                // Step 4: Vehicle.TransferState 업데이트 (ASSIGNED_ACQUIRING / ASSIGNED_DEPOSITING)
                UpdateVehicleTransferState(resourceManager, vehicle, isSourceArrival);

                // Step 5: 도착 노드 Location 조회 후 Type=EQP 일 때만 Host JOBREPORT(ARRIVED) 송신
                ReportArrivedIfEqp(messageManager, resourceManager, tc, effectiveVehicleId, nodeId);
            }
            catch (Exception ex)
            {
                logger.Error($"HandleVehicleArrivedActivity 오류: {ex.Message}", ex);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Step 1: NA_R_VEHICLE 존재 확인
        // ─────────────────────────────────────────────────────────────
        private static VehicleEx CheckVehicle(IResourceManagerEx rm, string vehicleId)
        {
            VehicleEx v = rm.GetVehicle(vehicleId);
            if (v == null)
                logger.Error($"[Step1] CheckVehicle: Vehicle 없음 vehicleId={vehicleId}");
            else
                logger.Info($"[Step1] CheckVehicle OK vehicleId={vehicleId}");
            return v;
        }

        // ─────────────────────────────────────────────────────────────
        // Step 2: NA_T_TRANSPORTCMD.VehicleEvent = RAIL-VEHICLEARRIVED
        // ─────────────────────────────────────────────────────────────
        private static void UpdateTransportCommandVehicleEvent(ITransferManagerEx tm, TransportCommandEx tc)
        {
            tc.VehicleEvent = MsgName;
            var set = new Dictionary<string, object> { ["VehicleEvent"] = MsgName };
            tm.UpdateTransportCommand(tc, set);
            logger.Info($"[Step2] UpdateTransportCommandVehicleEvent tc={tc.JobId}, vehicleEvent={MsgName}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 3: TC.State = ARRIVED_SOURCE / ARRIVED_DEST + Load/UnloadArrivedTime
        // ─────────────────────────────────────────────────────────────
        private static void UpdateTransportCommandArrivedState(ITransferManagerEx tm, TransportCommandEx tc, bool isSourceArrival)
        {
            DateTime now = DateTime.Now;
            var set = new Dictionary<string, object>();

            if (isSourceArrival)
            {
                tc.State = TransportCommandEx.STATE_ARRIVED_SOURCE;
                tc.UnloadArrivedTime = now;
                set["State"] = tc.State;
                set["UnloadArrivedTime"] = tc.UnloadArrivedTime;
            }
            else
            {
                tc.State = TransportCommandEx.STATE_ARRIVED_DEST;
                tc.LoadArrivedTime = now;
                set["State"] = tc.State;
                set["LoadArrivedTime"] = tc.LoadArrivedTime;
            }

            tm.UpdateTransportCommand(tc, set);
            logger.Info($"[Step3] UpdateTransportCommandArrivedState tc={tc.JobId}, state={tc.State}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 4: NA_R_VEHICLE.TransferState = ASSIGNED_ACQUIRING / ASSIGNED_DEPOSITING
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleTransferState(IResourceManagerEx rm, VehicleEx v, bool isSourceArrival)
        {
            string newState = isSourceArrival
                ? VehicleEx.TRANSFERSTATE_ASSIGNED_ACQUIRING
                : VehicleEx.TRANSFERSTATE_ASSIGNED_DEPOSITING;
            rm.UpdateVehicleTransferState(v, newState, MsgName);
            logger.Info($"[Step4] UpdateVehicleTransferState vehicleId={v.VehicleId}, transferState={newState}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 5: 도착 노드 Location.Type 분기 → EQP 만 Host JOBREPORT(ARRIVED) 송신
        //         BUFFER(자재 포트) 는 MES 보고 대상 아니므로 송신 생략.
        // ─────────────────────────────────────────────────────────────
        private static void ReportArrivedIfEqp(IMessageManagerEx mm, IResourceManagerEx rm,
            TransportCommandEx tc, string vehicleId, string arrivedNodeId)
        {
            if (string.IsNullOrEmpty(arrivedNodeId))
            {
                logger.Warn($"[Step5] ReportArrivedIfEqp: 도착 nodeId 없음, JOBREPORT 송신 생략. tc={tc.JobId}");
                return;
            }

            // nodeId == Location.StationId 매핑.
            LocationEx loc = rm.GetLocationByStationId(arrivedNodeId);
            if (loc == null || string.IsNullOrEmpty(loc.Type))
            {
                logger.Warn($"[Step5] ReportArrivedIfEqp: NA_R_LOCATION 조회 실패 또는 Type 없음. nodeId={arrivedNodeId}, tc={tc.JobId}");
                return;
            }

            if (LocationExs.LOCATION_TYPE_BUFFER.Equals(loc.Type, StringComparison.OrdinalIgnoreCase))
            {
                logger.Info($"[Step5] ReportArrivedIfEqp: BUFFER(자재 포트) 도착 - JOBREPORT 송신 생략. nodeId={arrivedNodeId}, tc={tc.JobId}");
                return;
            }

            if (!LocationExs.LOCATION_TYPE_EQP.Equals(loc.Type, StringComparison.OrdinalIgnoreCase))
            {
                logger.Info($"[Step5] ReportArrivedIfEqp: Location.Type={loc.Type} 은 EQP/BUFFER 가 아니므로 JOBREPORT 송신 생략. nodeId={arrivedNodeId}, tc={tc.JobId}");
                return;
            }

            mm.SendJobReportToHost(
                "ARRIVED",
                tc.JobId,
                vehicleId,
                tc.JobType ?? "",
                tc.Description ?? "");
            logger.Info($"[Step5] ReportArrivedIfEqp: JOBREPORT(ARRIVED) 전송 tc={tc.JobId}, vehicleId={vehicleId}, nodeId={arrivedNodeId}, type=EQP");
        }
    }
}
