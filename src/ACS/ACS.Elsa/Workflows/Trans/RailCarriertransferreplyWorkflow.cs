using System;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Logging;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-CARRIERTRANSFERREPLY 워크플로우.
    ///
    /// EI 프로세스가 RAIL-CARRIERTRANSFER 처리 후 회신한 Reply 메시지를 수신하여
    /// CarrierTransferReplyWaiter에 시그널을 전달한다.
    /// Trans 프로세스의 EsListener가 수신하여 이 워크플로우를 실행한다.
    /// </summary>
    public class RailCarriertransferreplyWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-CARRIERTRANSFERREPLY";
            builder.Name = "RAIL-CARRIERTRANSFERREPLY";
            builder.Description = "RAIL-CARRIERTRANSFERREPLY 수신 시 ReplyWaiter에 시그널 전달";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleCarrierTransferReplyActivity()
                }
            };
        }
    }

    /// <summary>
    /// RAIL-CARRIERTRANSFERREPLY JSON에서 commandId와 resultCode를 추출하여
    /// CarrierTransferReplyWaiter.SignalReply()를 호출.
    /// </summary>
    [Activity("ACS.Trans", "Handle Carrier Transfer Reply",
        "RAIL-CARRIERTRANSFERREPLY 수신 → ReplyWaiter 시그널")]
    public class HandleCarrierTransferReplyActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleCarrierTransferReplyActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleCarrierTransferReplyActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleCarrierTransferReplyActivity: JSON 메시지가 null입니다.");
                    return;
                }

                string commandId = null;
                string resultCode = null;

                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("resultCode", out var rc))
                            resultCode = rc.GetString();
                    }
                }

                if (string.IsNullOrEmpty(commandId))
                {
                    logger.Error("HandleCarrierTransferReplyActivity: commandId가 없습니다.");
                    return;
                }

                logger.Info($"HandleCarrierTransferReplyActivity: commandId={commandId}, resultCode={resultCode}");
                CarrierTransferReplyWaiter.SignalReply(commandId, resultCode ?? "");
            }
            catch (Exception ex)
            {
                logger.Error($"HandleCarrierTransferReplyActivity 오류: {ex.Message}", ex);
            }
        }
    }
}
