using System;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Activities;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-CARRIERTRANSFER-LOAD 워크플로우.
    ///
    /// TransferServiceEx.ChangeTransportCommandStateToTransferingDest가 TC를
    /// TRANSFERRING_DEST 상태로 전이시킬 때 트리거된다.
    /// Dest 포트(EQP/MAT)에 매거진을 투입하라는 LOAD 명령을 AMR(EI)로 전송.
    ///
    /// Arguments: [TransportCommandEx tc, string vehicleId]
    /// </summary>
    public class RailCarrierTransferLoadWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-CARRIERTRANSFER-LOAD";
            builder.Name = "RAIL-CARRIERTRANSFER-LOAD";
            builder.Description = "Dest 단계 LOAD 명령 전송 (Reply 대기, 3회 재시도)";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new SendCarrierTransferLoadActivity()
                }
            };
        }
    }

    /// <summary>
    /// Workflow Arguments에서 TC와 VehicleId를 추출해 LOAD 메시지를 전송하고
    /// Reply를 대기한다. 5초 타임아웃, 최대 3회 재시도.
    /// </summary>
    [Activity("ACS.Trans", "Send Carrier Transfer LOAD",
        "Dest 포트에 매거진 투입 명령 (RAIL-CARRIERTRANSFER, JobType=LOAD)")]
    public class SendCarrierTransferLoadActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        private const int MaxAttempts = 3;
        private const int TimeoutMs = 5000;

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 2)
                {
                    logger.Error("SendCarrierTransferLoadActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var tc = args[0] as TransportCommandEx;
                var vehicleId = args[1] as string;

                if (tc == null || string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("SendCarrierTransferLoadActivity: TC 또는 VehicleId가 null입니다.");
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();

                if (messageManager == null)
                {
                    logger.Error("SendCarrierTransferLoadActivity: IMessageManagerEx not resolved");
                    return;
                }

                string json = CarrierTransferJsonBuilder.Build(
                    tc, vehicleId, TransportCommandEx.JOBTYPE_LOAD,
                    useSource: false, resourceManager, logger);

                if (string.IsNullOrEmpty(json))
                {
                    logger.Error($"SendCarrierTransferLoadActivity: JSON 빌드 실패, TC={tc.JobId}");
                    return;
                }

                string commandId = tc.JobId;

                for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    logger.Info($"SendCarrierTransferLoadActivity: LOAD 시도 {attempt}/{MaxAttempts} - TC {commandId}");

                    CarrierTransferReplyWaiter.RegisterWait(commandId);
                    messageManager.SendCarrierTransferJson(json);

                    var (replied, resultCode) = CarrierTransferReplyWaiter.WaitForReply(commandId, TimeoutMs);

                    if (replied && "OK".Equals(resultCode, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Info($"SendCarrierTransferLoadActivity: LOAD Reply OK - TC {commandId}, attempt={attempt}");
                        return;
                    }

                    if (replied)
                    {
                        logger.Warn($"SendCarrierTransferLoadActivity: Reply 수신했으나 실패 - TC {commandId}, resultCode={resultCode}, attempt={attempt}");
                    }
                    else
                    {
                        logger.Warn($"SendCarrierTransferLoadActivity: Reply 타임아웃 ({TimeoutMs}ms) - TC {commandId}, attempt={attempt}");
                    }
                }

                logger.Error($"SendCarrierTransferLoadActivity: {MaxAttempts}회 시도 모두 실패 - TC {commandId}");
            }
            catch (Exception ex)
            {
                logger.Error($"SendCarrierTransferLoadActivity 오류: {ex.Message}", ex);
            }
        }
    }
}
