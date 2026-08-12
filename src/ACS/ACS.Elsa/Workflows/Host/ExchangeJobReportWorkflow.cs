using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// EXCHANGE-JOBREPORT 워크플로우 (EXCHANGE v2 — S4).
    ///
    /// Trans 프로세스가 발행한 EXCHANGE JOBREPORT JSON(Step/StepName/CarrierSlot 포함)을
    /// host 큐로 수신하면 실행: 파싱 → MES XML 변환·TCP 송신.
    /// 기존 HostJobReportWorkflow(JOBREPORT)와 병렬 신규 경로 (D4).
    ///
    /// TC.State 천이는 trans 측 워크플로우가 단독 책임 — 본 워크플로우는 전달만 수행.
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "EXCHANGE-JOBREPORT" (ElsaWorkflowManagerBridge가 설정)
    ///   - Arguments: object[] { string } (EXCHANGE-JOBREPORT JSON payload)
    /// </summary>
    public class ExchangeJobReportWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "EXCHANGE-JOBREPORT";
            builder.Name = "EXCHANGE-JOBREPORT";
            builder.Description = "Trans EXCHANGE JOBREPORT JSON 수신 → MES XML 전달 (Step/StepName/CarrierSlot 포함)";

            var jobReportData = new Variable<ExchangeJobReportData> { Name = "ExchangeJobReportData" };
            builder.WithVariable(jobReportData);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ExtractExchangeJobReportFromInput
                    {
                        OutputData = new(jobReportData)
                    },

                    new ForwardExchangeJobReportToMesActivity
                    {
                        JobReportData = new(jobReportData)
                    },

                    new WriteLine("EXCHANGE-JOBREPORT workflow completed: forwarded to MES")
                }
            };
        }
    }
}
