using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ACS.Elsa.Workflows.Host
{
    /// <summary>
    /// COMMON-STOP-HOST 워크플로우.
    ///
    /// Host 서버 종료 시 호출.
    /// </summary>
    public class CommonStopHostWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-STOP-HOST";
            builder.Name = "COMMON-STOP-HOST";
            builder.Description = "Host 서버 종료 처리";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("COMMON-STOP-HOST: Host bridge process stopped.")
                }
            };
        }
    }
}
