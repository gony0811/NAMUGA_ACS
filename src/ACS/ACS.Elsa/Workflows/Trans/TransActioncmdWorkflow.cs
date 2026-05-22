using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Communication.Mqtt.Model;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// TRANS-ACTIONCMD 워크플로우.
    ///
    /// Host 프로세스가 MES ACTIONCMD 를 받고 JSON 으로 변환해 Trans 큐로 forward 하면 이 워크플로우가 실행된다.
    ///
    /// 처리 흐름:
    ///   1. ACTIONCMD JSON → ActionCmdMessage 파싱
    ///   2. JobId 로 TransportCommand 조회 → 할당된 VehicleId 획득
    ///   3. Vehicle.CommId → NA_C_MQTT → ApplicationName → destination 해석 후
    ///      RAIL-ACTIONCMD JSON 을 EI 큐로 송신
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "TRANS-ACTIONCMD"
    ///   - Arguments: object[] { string } (ACTIONCMD JSON payload)
    /// </summary>
    public class TransActioncmdWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "TRANS-ACTIONCMD";
            builder.Name = "TRANS-ACTIONCMD";
            builder.Description = "Host → Trans ACTIONCMD JSON 수신 → TC 조회 → RAIL-ACTIONCMD EI 송신";

            var actionCmd = new Variable<ActionCmdMessage> { Name = "ActionCmd" };
            builder.WithVariable(actionCmd);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ExtractActionCmdFromInput
                    {
                        OutputData = new(actionCmd)
                    },

                    new RouteActionCmdToVehicleActivity
                    {
                        ActionCmd = new(actionCmd)
                    },

                    new WriteLine("TRANS-ACTIONCMD workflow completed")
                }
            };
        }
    }
}
