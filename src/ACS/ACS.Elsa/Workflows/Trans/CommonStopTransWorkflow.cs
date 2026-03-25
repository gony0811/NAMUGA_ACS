using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// COMMON-STOP-TRANS 워크플로우.
    ///
    /// Trans 서버 종료 시 호출.
    /// MQTT 브로커 연결을 종료하고 heartbeat 모니터링 타이머를 정지한다.
    /// </summary>
    public class CommonStopTransWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-STOP-TRANS";
            builder.Name = "COMMON-STOP-TRANS";
            builder.Description = "Trans 서버 종료: MQTT 브로커 연결 종료 및 리소스 정리";

            var mqttResult = new Variable<bool> { Name = "MqttStopResult" };
            builder.WithVariable(mqttResult);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new StopMqttActivity
                    {
                        Result = new(mqttResult)
                    },

                    new If
                    {
                        Condition = new(mqttResult),
                        Then = new WriteLine("COMMON-STOP-TRANS: MQTT 브로커 연결 종료 완료"),
                        Else = new WriteLine("COMMON-STOP-TRANS: MQTT 정지 실패")
                    },

                    new WriteLine("COMMON-STOP-TRANS workflow completed.")
                }
            };
        }
    }
}
