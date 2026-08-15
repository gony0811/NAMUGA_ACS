using System;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.History;
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
    /// RAIL-VEHICLEJOBFAILED 워크플로우 (EXCHANGE — Abnormal, "취소·오류" 시트 §2).
    ///
    /// EI 가 AMR reply(status=FAILED) 중 EXCHANGE origin 픽업 실패를 보고할 때 수신.
    /// 사양: 매거진 부재 → JOBREPORT(COMPLETE, ErrorCode=MAGAZINE_NOT_FOUND) 후 Job 즉시 종결.
    /// ACS 재시도 없음 — 재교체는 MES 가 새 EXCHANGECMD 로 재요청.
    ///
    /// Arguments: [string jsonMessage]
    /// </summary>
    public class RailVehicleJobfailedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEJOBFAILED";
            builder.Name = "RAIL-VEHICLEJOBFAILED";
            builder.Description = "EXCHANGE 픽업 실패 수신 → COMPLETE(MAGAZINE_NOT_FOUND) 보고 + Job 즉시 종결";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleVehicleJobFailedActivity()
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEJOBFAILED JSON 수신 처리 — MAGAZINE_NOT_FOUND 즉시 종결.
    /// </summary>
    [Activity("ACS.Trans", "Handle Vehicle Job Failed",
        "EXCHANGE 픽업 실패 → COMPLETE(MAGAZINE_NOT_FOUND) 보고 + TC 종결 + 차량 초기화")]
    public class HandleVehicleJobFailedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleVehicleJobFailedActivity));

        private const string MsgName = "RAIL-VEHICLEJOBFAILED";
        private const string ErrMsgMagazineNotFound = "No magazine found at LoadSourceLoc candidates";

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleVehicleJobFailedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleVehicleJobFailedActivity: JSON 메시지가 null입니다.");
                    return;
                }

                string commandId = null;
                string vehicleId = null;
                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("vehicleId", out var vid))
                            vehicleId = vid.GetString();
                    }
                }

                if (string.IsNullOrEmpty(commandId))
                {
                    logger.Error("HandleVehicleJobFailedActivity: commandId가 없습니다.");
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var tm = accessor?.Resolve<ITransferManagerEx>();
                var rm = accessor?.Resolve<IResourceManagerEx>();
                var mm = accessor?.Resolve<IMessageManagerEx>();
                var hm = accessor?.Resolve<IHistoryManagerEx>();
                var sm = accessor?.ResolveOptional<ISlotManagerEx>();

                if (tm == null || rm == null || mm == null || hm == null)
                {
                    logger.Error("HandleVehicleJobFailedActivity: 필수 서비스 해결 실패");
                    return;
                }

                TransportCommandEx tc = tm.GetTransportCommand(commandId);
                if (tc == null)
                {
                    logger.Warn($"HandleVehicleJobFailedActivity: TC 없음 commandId={commandId} — 생략");
                    return;
                }

                if (!TransportCommandEx.STATE_EXCHANGE_ASSIGNED.Equals(tc.State, StringComparison.OrdinalIgnoreCase)
                    || ExchangeSteps.GetStep(tc.AdditionalInfo) != ExchangeSteps.STEP_PICKUP_NEW)
                {
                    logger.Warn($"HandleVehicleJobFailedActivity: 대상 아님 — state={tc.State}, " +
                                $"step={ExchangeSteps.GetStep(tc.AdditionalInfo)}, tc={tc.JobId}. 생략 (정지+운영자 개입).");
                    return;
                }

                string effectiveVehicleId = !string.IsNullOrEmpty(vehicleId) ? vehicleId : (tc.VehicleId ?? "");

                // ① 보고: COMPLETE(MAGAZINE_NOT_FOUND) — Step=10 기준 (사양 "취소·오류" 시트 §2)
                mm.SendExchangeJobReportToHost(
                    "COMPLETE", tc.JobId, effectiveVehicleId,
                    ExchangeSteps.STEP_PICKUP_NEW.ToString(), ExchangeSteps.StepName(ExchangeSteps.STEP_PICKUP_NEW),
                    "", TransportCommandEx.JOBTYPE_EXCHANGE, tc.GetMaterialType() ?? "",
                    JobCancelJudge.ERR_MAGAZINE_NOT_FOUND, ErrMsgMagazineNotFound);

                // ② TC 즉시 종결: COMPLETEFAILED 기록 → 히스토리 이관 → 삭제 → 슬롯 예약 해제
                tc.State = TransportCommandEx.STATE_COMPLETEFAILED;
                tc.VehicleEvent = MsgName;
                tm.UpdateTransportCommand(tc);
                hm.CreateTransportCommandHistory(tc, "", JobCancelJudge.ERR_MAGAZINE_NOT_FOUND);
                tm.DeleteTransportCommand(tc);
                sm?.ReleaseAllByJobId(tc.JobId);

                // ③ 차량 초기화 (재배차 가능 상태로 복귀 — 실물 미적재)
                VehicleEx vehicle = string.IsNullOrEmpty(effectiveVehicleId) ? null : rm.GetVehicle(effectiveVehicleId);
                if (vehicle != null)
                {
                    rm.UpdateVehicleTransportCommandId(vehicle, "", MsgName);
                    rm.UpdateVehicle(vehicle, "Path", "");
                    rm.UpdateVehicleAcsDestNodeId(vehicle, "", MsgName);
                    rm.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED, MsgName);
                    rm.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_IDLE, MsgName);
                    sm?.ReleaseAllByVehicleId(effectiveVehicleId);
                }

                logger.Info($"[EXCHANGE] MAGAZINE_NOT_FOUND 즉시 종결 — tc={tc.JobId}, vehicleId={effectiveVehicleId} " +
                            "(재교체는 MES 가 새 EXCHANGECMD 로 재요청)");
            }
            catch (Exception ex)
            {
                logger.Error($"HandleVehicleJobFailedActivity 오류: {ex.Message}", ex);
            }
        }
    }
}
