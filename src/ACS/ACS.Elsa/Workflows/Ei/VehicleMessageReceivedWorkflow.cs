using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// VEHICLE-MESSAGERECEIVED 워크플로우.
    ///
    /// MQTT status 토픽으로 AMR 상태 메시지가 수신되면 MqttInterfaceManager에서 호출.
    /// AmrStatusMessage를 파싱하여 Vehicle 상태(RunState, AlarmState, BatteryRate,
    /// ProcessingState 등)를 업데이트한다.
    /// </summary>
    public class VehicleMessageReceivedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "VEHICLE-MESSAGERECEIVED";
            builder.Name = "VEHICLE-MESSAGERECEIVED";
            builder.Description = "AMR 상태 메시지 수신 시 Vehicle 상태 업데이트";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ProcessAmrStatusActivity(),
                    new ProcessAmrLocationChangeActivity(),
                }
            };
        }
    }
}
