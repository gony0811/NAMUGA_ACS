using System.Xml;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// HOST EXCHANGECMD 워크플로우 (EXCHANGE v2 — S3 슬라이스).
    ///
    /// Host(MES)로부터 EXCHANGECMD를 수신하면 실행:
    ///   1. EXCHANGECMD XML 추출
    ///   2. 파싱 + 검증 + 1-TC 3-waypoint 생성 (EXCHANGE_QUEUED) — 실패 시 ErrCode 세팅, TC 미생성
    ///   3. JOBREPORT(RECEIVE, Step=10, StepName=PICKUP_NEW) 회신 — 검증 실패 시 ErrorCode 포함 NACK
    ///
    /// DefinitionId = "EXCHANGECMD" (HostBridgeService 가 <Command> 값으로 워크플로우를 찾음).
    /// 기존 HostMoveCmdWorkflow 와 동일 골격 — 병렬 신규 경로 (D4).
    /// 참조: ACS_EXCHANGE_구현사양서.md §4.4
    /// </summary>
    public class HostExchangeCmdWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "EXCHANGECMD";
            builder.Name = "EXCHANGECMD";
            builder.Description = "Host EXCHANGECMD 수신 → 검증/TC 생성(EXCHANGE_QUEUED) → JOBREPORT(RECEIVE, Step=10)";

            var exchangeCmdXml = new Variable<XmlDocument> { Name = "ExchangeCmdXml" };
            var jobReportXml = new Variable<XmlDocument> { Name = "JobReportXml" };
            var transportCommandId = new Variable<string> { Name = "TransportCommandId" };
            var errCode = new Variable<string> { Name = "ErrCode" };
            var errMsg = new Variable<string> { Name = "ErrMsg" };
            builder.WithVariable(exchangeCmdXml);
            builder.WithVariable(jobReportXml);
            builder.WithVariable(transportCommandId);
            builder.WithVariable(errCode);
            builder.WithVariable(errMsg);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ExtractExchangeCmdFromInput
                    {
                        OutputXml = new(exchangeCmdXml)
                    },

                    new CreateExchangeTransportCommandActivity
                    {
                        ExchangeCmdXml = new(exchangeCmdXml),
                        TransportCommandId = new(transportCommandId),
                        ErrCode = new(errCode),
                        ErrMsg = new(errMsg)
                    },

                    new SendExchangeJobReportActivity
                    {
                        ExchangeCmdXml = new(exchangeCmdXml),
                        ReportType = new("RECEIVE"),
                        Step = new("10"),
                        StepName = new("PICKUP_NEW"),
                        ActionType = new("EXCHANGE"),
                        ErrCode = new(errCode),
                        ErrMsg = new(errMsg),
                        JobReportXml = new(jobReportXml)
                    },

                    new WriteLine("EXCHANGECMD workflow completed: TC(EXCHANGE_QUEUED) processed, JOBREPORT(RECEIVE, Step=10) sent")
                }
            };
        }
    }
}
