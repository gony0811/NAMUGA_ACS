using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// VEHICLE-HEARTBEAT 워크플로우.
    ///
    /// MQTT heartbeat 토픽으로 AMR heartbeat 메시지가 수신되면 MqttInterfaceManager에서 호출.
    /// 현재는 빈 워크플로우이며, 필요 시 Activity를 추가하여 확장 가능.
    /// </summary>
    public class VehicleHeartbeatWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "VEHICLE-HEARTBEAT";
            builder.Name = "VEHICLE-HEARTBEAT";
            builder.Description = "AMR heartbeat 메시지 수신 시 처리";

            builder.Root = new Sequence
            {
                Activities = { }
            };
        }
    }
}
