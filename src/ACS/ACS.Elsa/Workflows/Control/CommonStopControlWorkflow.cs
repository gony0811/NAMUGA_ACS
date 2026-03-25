using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ACS.Elsa.Workflows.Control
{
    /// <summary>
    /// COMMON-STOP-CONTROL 워크플로우.
    ///
    /// Control 서버 종료 시 호출.
    /// </summary>
    public class CommonStopControlWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-STOP-CONTROL";
            builder.Name = "COMMON-STOP-CONTROL";
            builder.Description = "Control 서버 종료 처리";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("COMMON-STOP-CONTROL: Control 서버 종료 완료")
                }
            };
        }
    }
}
