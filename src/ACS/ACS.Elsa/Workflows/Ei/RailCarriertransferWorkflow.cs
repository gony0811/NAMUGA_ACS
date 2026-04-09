using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// RAIL-CARRIERTRANSFER 워크플로우.
    ///
    /// Trans 프로세스에서 RabbitMQ로 전송된 RAIL-CARRIERTRANSFER JSON을 수신하여
    /// vehicleId로 Vehicle 조회 → CommType=MQTT인 경우 MqttInterfaceManager를 통해
    /// 해당 Vehicle의 MQTT 브로커로 destNodeId 이동 명령을 전송한다.
    /// </summary>
    public class RailCarriertransferWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-CARRIERTRANSFER";
            builder.Name = "RAIL-CARRIERTRANSFER";
            builder.Description = "RAIL-CARRIERTRANSFER 수신 → MQTT로 Vehicle에 이동 명령 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleCarrierTransferActivity()
                }
            };
        }
    }
}
