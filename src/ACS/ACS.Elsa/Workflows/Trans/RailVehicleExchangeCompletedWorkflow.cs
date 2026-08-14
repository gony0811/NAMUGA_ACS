using System;
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
using ACS.Elsa.Activities;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEEXCHANGECOMPLETED 워크플로우 (EXCHANGE v2 S5).
    ///
    /// EI가 AMR의 설비(mid) 교체 작업 완료(구자재 회수 UNLOAD_OLD + 신자재 투입 LOAD_NEW,
    /// AMR reply jobType=EXCHANGE)를 Trans에 보고할 때 수신.
    /// 슬롯 전이(loadSlot 하치 + unloadSlot 적재) → STEP=50 → 반납(dest)행
    /// RAIL-CARRIERTRANSFER(LOAD) 전송 → EXCHANGE-JOBREPORT STEP_COMPLETE(30, 40) 발행.
    /// RailVehicleAcquireCompletedWorkflow 미러 구조 — EXCHANGE 전용 병렬 경로 (D4/D5).
    ///
    /// Arguments: [string jsonMessage]
    /// </summary>
    public class RailVehicleExchangeCompletedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEEXCHANGECOMPLETED";
            builder.Name = "RAIL-VEHICLEEXCHANGECOMPLETED";
            builder.Description = "AMR 설비 교체 완료 보고 수신 → 슬롯 전이 + STEP=50 + 반납행 RAIL-CARRIERTRANSFER(LOAD) 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleVehicleExchangeCompletedActivity()
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEEXCHANGECOMPLETED JSON 수신 처리.
    /// 공통 전처리(차량 확인/이벤트 기록) 후 본체는 ExchangeTransHandlers.OnExchangeCompleted 가 수행.
    /// </summary>
    [Activity("ACS.Trans", "Handle Vehicle Exchange Completed",
        "AMR 설비 교체 완료 수신 → 슬롯 전이 → RAIL-CARRIERTRANSFER(LOAD) 전송")]
    public class HandleVehicleExchangeCompletedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleVehicleExchangeCompletedActivity));

        private const string MsgName = "RAIL-VEHICLEEXCHANGECOMPLETED";

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleVehicleExchangeCompletedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleVehicleExchangeCompletedActivity: JSON 메시지가 null입니다.");
                    return;
                }

                string commandId = null;
                string vehicleId = null;
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
                        if (dataEl.TryGetProperty("resultCode", out var rc))
                            resultCode = rc.GetString();
                        if (dataEl.TryGetProperty("errorCode", out var ec))
                            errorCode = ec.ValueKind == JsonValueKind.String ? ec.GetString() : ec.ToString();
                        if (dataEl.TryGetProperty("errorMessage", out var em))
                            errorMessage = em.GetString();
                    }
                }

                logger.Info($"HandleVehicleExchangeCompletedActivity: commandId={commandId}, vehicleId={vehicleId}, resultCode={resultCode}, errorCode={errorCode}");

                if (string.IsNullOrEmpty(commandId))
                {
                    logger.Error("HandleVehicleExchangeCompletedActivity: commandId가 없습니다.");
                    return;
                }

                if (!"OK".Equals(resultCode, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleVehicleExchangeCompletedActivity: 교체 실패 - commandId={commandId}, resultCode={resultCode}, errorCode={errorCode}, errorMessage={errorMessage}. 후속 처리 생략.");
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var slotManager = accessor?.Resolve<ISlotManagerEx>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();

                if (resourceManager == null || transferManager == null || slotManager == null || messageManager == null)
                {
                    logger.Error("HandleVehicleExchangeCompletedActivity: 필수 서비스 해결 실패 " +
                        $"(resourceManager={resourceManager != null}, transferManager={transferManager != null}, " +
                        $"slotManager={slotManager != null}, messageManager={messageManager != null})");
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommand(commandId);
                if (tc == null)
                {
                    logger.Error($"HandleVehicleExchangeCompletedActivity: TC를 찾을 수 없음. commandId={commandId}");
                    return;
                }

                if (!TransportCommandEx.JOBTYPE_EXCHANGE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleVehicleExchangeCompletedActivity: EXCHANGE TC 아님 — jobType={tc.JobType}, tc={tc.JobId}. 생략.");
                    return;
                }

                string effectiveVehicleId = !string.IsNullOrEmpty(vehicleId) ? vehicleId : (tc.VehicleId ?? "");
                VehicleEx vehicle = !string.IsNullOrEmpty(effectiveVehicleId)
                    ? resourceManager.GetVehicle(effectiveVehicleId) : null;
                if (vehicle == null)
                {
                    logger.Error($"HandleVehicleExchangeCompletedActivity: Vehicle 없음 vehicleId={effectiveVehicleId}, tc={tc.JobId}");
                    return;
                }

                // 공통 전처리 (Acquire/Deposit 관례 미러): 이벤트 기록 + 연결 상태 + 이벤트 시각
                tc.VehicleEvent = MsgName;
                transferManager.UpdateTransportCommand(tc);
                resourceManager.UpdateVehicleConnectionState(vehicle, VehicleEx.CONNECTIONSTATE_CONNECT, MsgName);
                resourceManager.UpdateVehicleEventTime(vehicle);

                // 본체: 슬롯 전이 + STEP=50 + dest행 LOAD 전송 + STEP_COMPLETE(30, 40)
                ExchangeTransHandlers.OnExchangeCompleted(tc, vehicle,
                    transferManager, resourceManager, slotManager, messageManager);
            }
            catch (Exception ex)
            {
                logger.Error($"HandleVehicleExchangeCompletedActivity 오류: {ex.Message}", ex);
            }
        }
    }
}
