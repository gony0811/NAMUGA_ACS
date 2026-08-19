using System;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Logging;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Workflow;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEARRIVED 워크플로우 (AMR reply status=ARRIVED, ACS-AMR 사양 v0.3).
    ///
    /// EI 가 AMR 의 명시적 도착 보고를 중계하면, pose 기반 도착 판정(RailVehicleUpdateWorkflow →
    /// DispatchDestArrivedIfNeeded)과 같은 진입점인 RAIL-VEHICLEDESTARRIVED(vehicleId) 로 수렴시킨다.
    /// 두 경로가 같은 도착에 대해 이중 발화해도 RailVehicleDestArrivedActivity/ExchangeTransHandlers.OnDestArrived
    /// 의 ARRIVED 마커(TC AdditionalInfo) 가드가 중복 보고를 막는다.
    ///
    /// Arguments: [string jsonMessage]
    /// </summary>
    public class RailVehicleArrivedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEARRIVED";
            builder.Name = "RAIL-VEHICLEARRIVED";
            builder.Description = "AMR ARRIVED reply 수신 → RAIL-VEHICLEDESTARRIVED 진입점으로 수렴";

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
    /// RAIL-VEHICLEARRIVED JSON 수신 → vehicleId 해석 → RAIL-VEHICLEDESTARRIVED 실행.
    /// 도착 판정 본체(TC/Location 매칭, EQP 판정, EXCHANGE STEP 판정)는 기존 워크플로우가 담당한다.
    /// </summary>
    [Activity("ACS.Trans", "Handle Vehicle Arrived",
        "AMR ARRIVED reply → RAIL-VEHICLEDESTARRIVED dispatch")]
    public class HandleVehicleArrivedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleVehicleArrivedActivity));

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
                int? step = null;
                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("vehicleId", out var vid))
                            vehicleId = vid.GetString();
                        if (dataEl.TryGetProperty("step", out var st) && st.ValueKind == JsonValueKind.Number)
                            step = st.GetInt32();
                    }
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var rm = accessor?.Resolve<IResourceManagerEx>();
                var workflowManager = accessor?.Resolve<IWorkflowManager>();
                if (rm == null || workflowManager == null)
                {
                    logger.Error("HandleVehicleArrivedActivity: 필수 서비스 해결 실패");
                    return;
                }

                // vehicleId 가 비면 TC 로부터 보완
                if (string.IsNullOrEmpty(vehicleId) && !string.IsNullOrEmpty(commandId))
                {
                    var tm = accessor.Resolve<ACS.Core.Transfer.ITransferManagerEx>();
                    var tc = tm?.GetTransportCommand(commandId);
                    vehicleId = tc?.VehicleId;
                }

                if (string.IsNullOrEmpty(vehicleId))
                {
                    logger.Warn($"HandleVehicleArrivedActivity: vehicleId 해석 불가 — 생략. commandId={commandId}");
                    return;
                }

                VehicleEx vehicle = rm.GetVehicle(vehicleId);
                if (vehicle == null)
                {
                    logger.Warn($"HandleVehicleArrivedActivity: Vehicle 미존재 vehicleId={vehicleId} — 생략");
                    return;
                }

                logger.Info($"HandleVehicleArrivedActivity: AMR ARRIVED reply → RAIL-VEHICLEDESTARRIVED dispatch. " +
                            $"vehicleId={vehicleId}, commandId={commandId}, step={step}, currentNode={vehicle.CurrentNodeId}");
                workflowManager.Execute("RAIL-VEHICLEDESTARRIVED", (object)vehicleId);
            }
            catch (Exception ex)
            {
                logger.Error($"HandleVehicleArrivedActivity 오류: {ex.Message}", ex);
            }
        }
    }
}
