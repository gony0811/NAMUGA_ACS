using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Core.Resource.Model;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// DISCONNECTED 워크플로우.
    ///
    /// MqttInterfaceManager.CheckAmrHeartbeats()에서 AMR 연결 끊김이 감지되면 호출.
    /// Vehicle의 ConnectionState를 DISCONNECT로 업데이트한다.
    /// </summary>
    public class DisconnectedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "DISCONNECTED";
            builder.Name = "DISCONNECTED";
            builder.Description = "AMR 연결 끊김 감지 시 Vehicle ConnectionState를 DISCONNECT로 업데이트";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new UpdateAmrConnectionStateActivity
                    {
                        ConnectionState = new(VehicleEx.CONNECTIONSTATE_DISCONNECT)
                    },
                }
            };
        }
    }
}
