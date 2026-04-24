using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// AMR-REPLY-RECEIVED 워크플로우.
    ///
    /// MqttInterfaceManager가 amr/{id}/reply 토픽에서 수신한 reply 메시지를
    /// Trans 프로세스로 라우팅한다.
    /// status=COMPLETED 이고 jobType=UNLOAD이면 RAIL-VEHICLEACQUIRECOMPLETED,
    /// jobType=LOAD이면 RAIL-VEHICLEDEPOSITCOMPLETED를 Trans로 전송.
    /// </summary>
    public class AmrReplyReceivedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "AMR-REPLY-RECEIVED";
            builder.Name = "AMR-REPLY-RECEIVED";
            builder.Description = "AMR reply 수신 → COMPLETED+jobType 분기로 Trans에 ACQUIRE/DEPOSIT 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleAmrReplyActivity()
                }
            };
        }
    }
}
