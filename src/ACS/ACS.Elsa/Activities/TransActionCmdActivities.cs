using System;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  TRANS-ACTIONCMD 수신 → Vehicle 라우팅 → RAIL-ACTIONCMD 전달
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 워크플로우 Input(Arguments) 에서 TRANS-ACTIONCMD JSON 을 파싱해 ActionCmdMessage 로 추출.
    /// 입력 형식: { "header": { "messageName": "TRANS-ACTIONCMD", ... }, "data": { ... } }
    /// </summary>
    [Activity("ACS.Trans", "Extract ActionCmd JSON",
        "워크플로우 입력에서 TRANS-ACTIONCMD JSON 을 파싱하여 ActionCmdMessage 로 추출합니다.")]
    public class ExtractActionCmdFromInput : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "추출된 ACTIONCMD 메시지")]
        public Output<ActionCmdMessage> OutputData { get; set; }

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

            if (string.IsNullOrEmpty(json))
            {
                logger.Warn("ExtractActionCmdFromInput: No TRANS-ACTIONCMD JSON found in input");
                context.Set(OutputData, new ActionCmdMessage());
                return;
            }

            try
            {
                var msg = JsonSerializer.Deserialize<ActionCmdMessage>(json) ?? new ActionCmdMessage();
                context.Set(OutputData, msg);
                logger.Info($"ExtractActionCmdFromInput: parsed - JobId={msg.Data?.JobId}, TargetLoc={msg.Data?.TargetLoc}, ActionType={msg.Data?.ActionType}");
            }
            catch (Exception ex)
            {
                logger.Error($"ExtractActionCmdFromInput: JSON parse 실패 - {ex.Message}", ex);
                context.Set(OutputData, new ActionCmdMessage());
            }
        }
    }

    /// <summary>
    /// ACTIONCMD 데이터의 jobId 로 TC 를 조회해 할당된 vehicleId 를 찾고,
    /// RAIL-ACTIONCMD JSON 을 빌드하여 해당 vehicle 의 EI destination 으로 송신.
    /// </summary>
    [Activity("ACS.Trans", "Route ActionCmd To Vehicle",
        "TC → Vehicle → RAIL-ACTIONCMD 라우팅 후 EI 로 송신")]
    public class RouteActionCmdToVehicleActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "ACTIONCMD 메시지")]
        public Input<ActionCmdMessage> ActionCmd { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var msg = ActionCmd?.Get(context);
                if (msg?.Data == null)
                {
                    logger.Error("RouteActionCmdToVehicleActivity: ActionCmd 가 비어있음");
                    context.Set(Result, false);
                    return;
                }

                string jobId = msg.Data.JobId ?? "";
                if (string.IsNullOrEmpty(jobId))
                {
                    logger.Error("RouteActionCmdToVehicleActivity: JobId 가 없음");
                    context.Set(Result, false);
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();

                if (transferManager == null || messageManager == null)
                {
                    logger.Error("RouteActionCmdToVehicleActivity: Manager 가 resolve 되지 않음");
                    context.Set(Result, false);
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommand(jobId);
                if (tc == null)
                {
                    logger.Error($"RouteActionCmdToVehicleActivity: TransportCommand not found - JobId={jobId}");
                    context.Set(Result, false);
                    return;
                }

                string vehicleId = tc.VehicleId;
                if (string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error($"RouteActionCmdToVehicleActivity: VehicleId not assigned - JobId={jobId}, State={tc.State}");
                    context.Set(Result, false);
                    return;
                }

                // EXCHANGE TC: 사양서 게이트 — ACTIONCMD(UNLOAD)는 STEP=20, ACTIONCMD(LOAD)는 STEP=30
                // 상태에서만 수용한다. 통과 시 진행 중 액션(ACT)과 대상 슬롯을 확정한다.
                int amrSlot = 1;
                if (TransportCommandEx.JOBTYPE_EXCHANGE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                {
                    if (!GateExchangeActionCmd(tc, msg.Data.ActionType, transferManager, out amrSlot))
                    {
                        context.Set(Result, false);
                        return;
                    }
                }

                string targetLoc = msg.Data.TargetLoc ?? "";
                string targetPort = msg.Data.TargetPort ?? "";
                string portId = string.IsNullOrEmpty(targetLoc) ? "" : targetLoc + ":" + targetPort;

                string nodeId = "";
                string portType = "";
                if (resourceManager != null && !string.IsNullOrEmpty(portId))
                {
                    try
                    {
                        var location = resourceManager.GetLocationByLocationId(portId);
                        if (location != null)
                        {
                            nodeId = location.StationId ?? "";
                            portType = location.Type ?? "";
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"RouteActionCmdToVehicleActivity: location 조회 실패 portId={portId} - {ex.Message}");
                    }
                }

                var railMsg = new RailActionCmdMessage
                {
                    Header = new RailActionCmdHeader
                    {
                        MessageName = "RAIL-ACTIONCMD",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "Trans"
                    },
                    Data = new RailActionCmdData
                    {
                        CommandId = jobId,
                        VehicleId = vehicleId,
                        NodeId = nodeId,
                        Port = targetPort,
                        ActionType = msg.Data.ActionType ?? "",
                        JobType = tc.JobType ?? msg.Data.ActionType ?? "",
                        PortType = portType,
                        // ACTIONCMD 가 명시한 MODEL 을 우선 사용. 비어있을 때만 TC.Description 의 MODEL 로 폴백.
                        Model = !string.IsNullOrEmpty(msg.Data.Model) ? msg.Data.Model : (tc.GetModel() ?? ""),
                        AmrSlot = amrSlot
                    }
                };

                string json = JsonSerializer.Serialize(railMsg);
                messageManager.SendActionCmdJson(json, vehicleId);

                logger.Info($"RouteActionCmdToVehicleActivity: RAIL-ACTIONCMD dispatched - JobId={jobId}, vehicleId={vehicleId}, nodeId={nodeId}, port={targetPort}, actionType={msg.Data.ActionType}");
                context.Set(Result, true);
            }
            catch (Exception ex)
            {
                logger.Error($"RouteActionCmdToVehicleActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }

        /// <summary>
        /// EXCHANGE TC 의 ACTIONCMD 수용 게이트 (시나리오 사양서 Scenario 시트):
        ///  - UNLOAD: STEP=20 에서만 수용 → ACT=UNLOAD 기록, 슬롯=UNLOADSLOT(회수)
        ///  - LOAD:   STEP=30 에서만 수용 → ACT=LOAD 기록, 슬롯=LOADSLOT(투입)
        /// 불일치 시 차량으로 중계하지 않는다 (false 반환).
        /// </summary>
        private static bool GateExchangeActionCmd(TransportCommandEx tc, string actionType,
            ITransferManagerEx transferManager, out int amrSlot)
        {
            amrSlot = 1;
            int step = ExchangeSteps.GetStep(tc.AdditionalInfo);

            string act;
            int expectedStep;
            string slotKey;
            if (ExchangeInfo.ACT_UNLOAD.Equals(actionType, StringComparison.OrdinalIgnoreCase))
            {
                act = ExchangeInfo.ACT_UNLOAD;
                expectedStep = ExchangeSteps.STEP_MOVE_TO_EQUIP;
                slotKey = ExchangeInfo.KEY_UNLOADSLOT;
            }
            else if (ExchangeInfo.ACT_LOAD.Equals(actionType, StringComparison.OrdinalIgnoreCase))
            {
                act = ExchangeInfo.ACT_LOAD;
                expectedStep = ExchangeSteps.STEP_UNLOAD_OLD;
                slotKey = ExchangeInfo.KEY_LOADSLOT;
            }
            else
            {
                logger.Warn($"RouteActionCmdToVehicleActivity: [EXCHANGE] 알 수 없는 ActionType={actionType} — 중계 생략. tc={tc.JobId}");
                return false;
            }

            if (step != expectedStep)
            {
                logger.Warn($"RouteActionCmdToVehicleActivity: [EXCHANGE] ACTIONCMD({act}) 수용 불가 — " +
                            $"STEP={step} (기대 {expectedStep}). 중계 생략. tc={tc.JobId}");
                return false;
            }

            string slot = ExchangeInfo.Get(tc.AdditionalInfo, slotKey);
            if (!int.TryParse(slot, out amrSlot) || amrSlot < 1)
            {
                logger.Warn($"RouteActionCmdToVehicleActivity: [EXCHANGE] {slotKey} 비정상('{slot}') — 기본 1 사용. tc={tc.JobId}");
                amrSlot = 1;
            }

            tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo, ExchangeInfo.KEY_ACT, act);
            transferManager.UpdateTransportCommand(tc);
            logger.Info($"RouteActionCmdToVehicleActivity: [EXCHANGE] ACT={act} 기록, amrSlot={amrSlot}, tc={tc.JobId}");
            return true;
        }
    }
}
