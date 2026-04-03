using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// VEHICLE-STATUS 워크플로우.
    ///
    /// MQTT status 토픽으로 AMR 상태 메시지가 수신되면 MqttInterfaceManager에서 호출.
    /// AmrStatusMessage의 모든 상태+위치를 RAIL-VEHICLEUPDATE JSON 메시지로 만들어
    /// Trans 프로세스에 전송한다. DB 업데이트는 Trans에서 수행.
    /// </summary>
    public class VehicleStatusWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "VEHICLE-STATUS";
            builder.Name = "VEHICLE-STATUS";
            builder.Description = "AMR 상태 메시지 수신 시 RAIL-VEHICLEUPDATE JSON을 Trans에 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new SendAmrVehicleUpdateActivity(),
                }
            };
        }
    }
}
