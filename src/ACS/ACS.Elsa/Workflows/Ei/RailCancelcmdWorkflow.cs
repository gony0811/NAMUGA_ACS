using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// RAIL-CANCELCMD 워크플로우.
    ///
    /// Trans 프로세스에서 JOBCANCEL 판정(C2/C3) 시 전송한 RAIL-CANCELCMD JSON 을 수신하여
    /// vehicleId 로 Vehicle 조회 → CommType=MQTT 인 경우 MqttInterfaceManager 를 통해
    /// 해당 Vehicle 의 MQTT 브로커로 cancelCmd 를 발행한다 (docs/mqtt_interface.md §cancelCmd).
    /// </summary>
    public class RailCancelcmdWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-CANCELCMD";
            builder.Name = "RAIL-CANCELCMD";
            builder.Description = "RAIL-CANCELCMD 수신 → MQTT 로 Vehicle 에 cancelCmd 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleCancelCmdActivity()
                }
            };
        }
    }
}
