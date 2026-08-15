using System.Xml;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// JOBCANCEL 워크플로우 (Host 프로세스).
    ///
    /// MES 로부터 JOBCANCEL(공통 취소 — EXCHANGE·MOVECMD, JobID 기준)을 수신하면
    /// 판정·실행 주체인 Trans 프로세스로 TRANS-JOBCANCEL JSON 을 릴레이한다.
    /// 취소 가부 판정(C1~C4)과 JOBREPORT(CANCEL) 회신은 Trans 가 수행한다
    /// (시나리오 사양서 "JOBCANCEL 요청"/"취소·오류" 시트).
    /// 사양상 접수 즉시응답은 없다 — 처리보고(JOBREPORT CANCEL) 수신까지 MES 가 대기.
    /// </summary>
    public class HostJobCancelWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "JOBCANCEL";
            builder.Name = "JOBCANCEL";
            builder.Description = "MES JOBCANCEL 수신 → TRANS-JOBCANCEL 릴레이 (판정·회신은 Trans)";

            var jobCancelXml = new Variable<XmlDocument> { Name = "JobCancelXml" };
            builder.WithVariable(jobCancelXml);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ExtractMoveCmdFromInput
                    {
                        OutputXml = new(jobCancelXml)
                    },

                    new SendJobCancelJsonToTransActivity
                    {
                        JobCancelXml = new(jobCancelXml)
                    }
                }
            };
        }
    }
}
