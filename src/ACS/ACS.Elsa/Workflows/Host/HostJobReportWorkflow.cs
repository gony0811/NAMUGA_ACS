using System;
using System.Xml;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// HOST_JOBREPORT 워크플로우.
    ///
    /// Trans 서버로부터 JOBREPORT를 수신하면 실행되는 워크플로우.
    ///
    /// 처리 흐름:
    ///   1. JOBREPORT XML 수신 (Input으로 전달됨)
    ///   2. JobID로 DB(TransportCommandEx) 조회 및 정합성 검증
    ///   3. 검증 성공 시 MES로 JOBREPORT TCP 전달
    ///   4. TransportCommand 상태 업데이트
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "JOBREPORT" (ElsaWorkflowManagerBridge가 설정)
    ///   - Arguments: object[] { XmlDocument } (JOBREPORT XML)
    /// </summary>
    public class HostJobReportWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "JOBREPORT";
            builder.Name = "JOBREPORT";
            builder.Description = "Trans JOBREPORT 수신 → DB 검증 → MES 전달 → TC 상태 업데이트";

            var jobReportXml = new Variable<XmlDocument> { Name = "JobReportXml" };
            var isValid = new Variable<bool> { Name = "IsValid" };
            var validationError = new Variable<string> { Name = "ValidationError" };
            var jobReportType = new Variable<string> { Name = "JobReportType" };
            builder.WithVariable(jobReportXml);
            builder.WithVariable(isValid);
            builder.WithVariable(validationError);
            builder.WithVariable(jobReportType);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input에서 JOBREPORT XmlDocument 추출
                    new ExtractJobReportFromInput
                    {
                        OutputXml = new(jobReportXml)
                    },

                    // Step 2: DB 검증 (JobID로 TransportCommandEx 조회 + 정합성 확인)
                    new ValidateJobReportActivity
                    {
                        JobReportXml = new(jobReportXml),
                        Result = new(isValid),
                        ValidationError = new(validationError),
                        JobReportType = new(jobReportType)
                    },

                    // Step 3: 검증 결과에 따라 분기
                    new If
                    {
                        Condition = new(ctx => isValid.Get(ctx)),
                        Then = new Sequence
                        {
                            Activities =
                            {
                                // Step 3a: MES로 JOBREPORT 전달
                                new ForwardJobReportToMesActivity
                                {
                                    JobReportXml = new(jobReportXml)
                                },

                                // Step 3b: TC 상태 업데이트 — COMPLETE 는 Trans 가 TC 를 삭제했으므로 skip
                                new If
                                {
                                    Condition = new(ctx => !string.Equals(jobReportType.Get(ctx), "COMPLETE", StringComparison.OrdinalIgnoreCase)),
                                    Then = new UpdateTransportCommandStateActivity
                                    {
                                        JobReportXml = new(jobReportXml)
                                    }
                                },

                                new WriteLine("JOBREPORT workflow completed: validated and forwarded to MES")
                            }
                        },
                        Else = new Sequence
                        {
                            Activities =
                            {
                                new WriteLine(ctx => $"JOBREPORT validation failed: {validationError.Get(ctx)}")
                            }
                        }
                    }
                }
            };
        }
    }
}
