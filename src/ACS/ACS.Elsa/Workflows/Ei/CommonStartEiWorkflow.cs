using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// COMMON-START-EI 워크플로우.
    ///
    /// EI 서버 시작 시 ApplicationInitializer에서 호출.
    /// MQTT 설정을 DB에서 로드하고 브로커 연결을 시작한다.
    ///
    /// 레거시 COMMON_START_EI가 NIO 목록을 순회하며 COMMON-START-NIO를 실행했던 것과
    /// 동일한 역할을 MQTT 기반으로 수행한다.
    /// </summary>
    public class CommonStartEiWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-START-EI";
            builder.Name = "COMMON-START-EI";
            builder.Description = "EI 서버 시작: MQTT 설정 로드 및 브로커 연결";

            var mqttResult = new Variable<bool> { Name = "MqttResult" };
            builder.WithVariable(mqttResult);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // MQTT 설정 로드 및 브로커 연결 시작
                    new LoadAndStartMqttActivity
                    {
                        Result = new(mqttResult)
                    },

                    // 결과에 따라 로그
                    new If
                    {
                        Condition = new(mqttResult),
                        Then = new WriteLine("COMMON-START-EI: MQTT 브로커 연결 시작 완료"),
                        Else = new WriteLine("COMMON-START-EI: MQTT 설정 없음 또는 연결 실패")
                    },

                    new WriteLine("COMMON-START-EI workflow completed.")
                }
            };
        }
    }
}
