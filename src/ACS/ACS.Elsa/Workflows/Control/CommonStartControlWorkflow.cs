using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ACS.Elsa.Workflows.Control
{
    /// <summary>
    /// COMMON-START-CONTROL 워크플로우.
    ///
    /// Control 서버 시작 시 ApplicationInitializer에서 호출.
    /// HeartBeat 스케줄링은 ApplicationInitializer.ScheduleHeartBeat()에서 별도 처리하므로
    /// 이 워크플로우에서는 시작 로그만 출력한다.
    /// </summary>
    public class CommonStartControlWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-START-CONTROL";
            builder.Name = "COMMON-START-CONTROL";
            builder.Description = "Control 서버 시작 초기화";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("COMMON-START-CONTROL: Control 서버 초기화 완료")
                }
            };
        }
    }
}
