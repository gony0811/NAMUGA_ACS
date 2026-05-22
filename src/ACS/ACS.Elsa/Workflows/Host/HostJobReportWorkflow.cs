using System;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using ACS.Communication.Host.Models;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// HOST_JOBREPORT 워크플로우.
    ///
    /// Trans 프로세스에서 JOBREPORT JSON 을 host 큐로 수신하면 실행되는 워크플로우.
    ///
    /// 처리 흐름:
    ///   1. JOBREPORT JSON 수신 → JobReportData 로 파싱
    ///   2. JobID 로 DB(TransportCommandEx) 조회 및 정합성 검증
    ///   3. 검증 성공 시 IHostMessageService 가 JSON→MES XML 을 구성하여 TCP 송신
    ///
    /// TC.State 천이는 trans 측 워크플로우(ScheduleQueueJob/AcquireCompleted/DepositCompleted)가
    /// 단독으로 책임지므로 본 워크플로우에서 추가 갱신하지 않는다 (이중-write 방지).
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "JOBREPORT" (ElsaWorkflowManagerBridge가 설정)
    ///   - Arguments: object[] { string } (JOBREPORT JSON payload)
    /// </summary>
    public class HostJobReportWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "JOBREPORT";
            builder.Name = "JOBREPORT";
            builder.Description = "Trans JOBREPORT JSON 수신 → MES XML 전달 (DB 재검증 없음)";

            var jobReportData = new Variable<JobReportData> { Name = "JobReportData" };
            builder.WithVariable(jobReportData);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input 에서 JOBREPORT JSON 추출 → JobReportData
                    new ExtractJobReportFromInput
                    {
                        OutputData = new(jobReportData)
                    },

                    // Step 2: MES 로 JOBREPORT 전달 (JSON→XML 변환 내부 수행).
                    // DB 재검증은 의도적으로 생략 — Trans 의 송신 자체가 신호이고,
                    // COMPLETE/CANCEL 시점에는 TC 가 종료/삭제 상태라 검증이 정상 흐름을 차단함.
                    new ForwardJobReportToMesActivity
                    {
                        JobReportData = new(jobReportData)
                    },

                    new WriteLine("JOBREPORT workflow completed: forwarded to MES")
                }
            };
        }
    }
}
