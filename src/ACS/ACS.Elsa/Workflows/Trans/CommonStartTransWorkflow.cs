using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// COMMON-START-TRANS 워크플로우.
    ///
    /// Trans 서버 시작 시 ApplicationInitializer에서 호출.
    /// MQTT 설정을 DB에서 로드하고 브로커 연결을 시작한다.
    ///
    /// 레거시 COMMON_START_TRANS는 서비스 resolve만 수행했으며,
    /// 신규 구현에서는 MQTT 기반 AMR 통신 초기화를 추가한다.
    /// </summary>
    public class CommonStartTransWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-START-TRANS";
            builder.Name = "COMMON-START-TRANS";
            builder.Description = "Trans 서버 시작: MQTT 설정 로드 및 브로커 연결";

            var mqttResult = new Variable<bool> { Name = "MqttResult" };
            builder.WithVariable(mqttResult);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("COMMON-START-TRANS: Trans 서버 초기화 시작"),
                    
                    

                    new WriteLine("COMMON-START-TRANS workflow completed.")
                }
            };
        }
    }
}
