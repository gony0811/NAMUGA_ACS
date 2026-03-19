using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// SCHEDULE-QUEUEJOB 워크플로우.
    ///
    /// Daemon 서버의 AwakeQueueTransportJob이 20초마다 트리거.
    /// Queued 상태의 TransportCommand를 Dest 기준 가장 가까운 idle AMR에 할당하고
    /// Host에 JOBREPORT(START)를 전송.
    /// </summary>
    public class ScheduleQueueJobWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "SCHEDULE-QUEUEJOB";
            builder.Name = "SCHEDULE-QUEUEJOB";
            builder.Description = "Queued TC 스케줄링: Dest 기준 idle AMR 할당 + JOBREPORT(START) 전송";

            var assignedCount = new Variable<int> { Name = "AssignedCount" };
            builder.WithVariable(assignedCount);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ScheduleQueueJobActivity { AssignedCount = new(assignedCount) },
                    new WriteLine(ctx => $"SCHEDULE-QUEUEJOB: {assignedCount.Get(ctx)} jobs assigned")
                }
            };
        }
    }
}
