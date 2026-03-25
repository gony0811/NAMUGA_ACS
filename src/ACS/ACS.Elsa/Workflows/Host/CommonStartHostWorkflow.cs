using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ACS.Elsa.Workflows.Host
{
    /// <summary>
    /// COMMON-START-HOST 워크플로우.
    ///
    /// Host 서버 시작 시 ApplicationInitializer에서 호출.
    /// 레거시 COMMON_START_HOST와 동일하게 시작 로그를 출력한다.
    /// </summary>
    public class CommonStartHostWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "COMMON-START-HOST";
            builder.Name = "COMMON-START-HOST";
            builder.Description = "Host 서버 시작 초기화";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("COMMON-START-HOST: Host bridge process initialized.")
                }
            };
        }
    }
}
