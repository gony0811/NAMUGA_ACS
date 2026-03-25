using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// COMMON-STOP-EI 워크플로우.
    ///
    /// EI 서버 종료 시 호출.
    /// MQTT 브로커 연결을 종료하고 heartbeat 모니터링 타이머를 정지한다.
    ///
    /// 레거시 COMMON_STOP_EI가 NIO 목록을 순회하며 COMMON_STOP_NIO를 실행했던 것과
    /// 동일한 역할을 MQTT 기반으로 수행한다.
    /// </summary>
    public class CommonStopEiWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-STOP-EI";
            builder.Name = "COMMON-STOP-EI";
            builder.Description = "EI 서버 종료: MQTT 브로커 연결 종료 및 리소스 정리";

            var mqttResult = new Variable<bool> { Name = "MqttStopResult" };
            builder.WithVariable(mqttResult);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // MQTT 브로커 연결 종료
                    new StopMqttActivity
                    {
                        Result = new(mqttResult)
                    },

                    // 결과에 따라 로그
                    new If
                    {
                        Condition = new(mqttResult),
                        Then = new WriteLine("COMMON-STOP-EI: MQTT 브로커 연결 종료 완료"),
                        Else = new WriteLine("COMMON-STOP-EI: MQTT 정지 실패")
                    },

                    new WriteLine("COMMON-STOP-EI workflow completed.")
                }
            };
        }
    }
}
