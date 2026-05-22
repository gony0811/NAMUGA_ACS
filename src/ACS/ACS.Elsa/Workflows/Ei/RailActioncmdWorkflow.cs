using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// RAIL-ACTIONCMD 워크플로우.
    ///
    /// Trans 프로세스에서 RabbitMQ로 전송된 RAIL-ACTIONCMD JSON을 수신하여
    /// vehicleId로 Vehicle 조회 → CommType=MQTT 인 경우 MqttInterfaceManager 를 통해
    /// 해당 Vehicle 의 MQTT 브로커로 actionCmd 를 발행한다 (docs/mqtt_interface.md).
    /// </summary>
    public class RailActioncmdWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-ACTIONCMD";
            builder.Name = "RAIL-ACTIONCMD";
            builder.Description = "RAIL-ACTIONCMD 수신 → MQTT 로 Vehicle 에 actionCmd 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleActionCmdActivity()
                }
            };
        }
    }
}
