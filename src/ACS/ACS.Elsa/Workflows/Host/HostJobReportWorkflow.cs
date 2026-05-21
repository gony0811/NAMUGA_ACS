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
            builder.Description = "Trans JOBREPORT JSON 수신 → DB 검증 → MES XML 전달 → TC 상태 업데이트";

            var jobReportData = new Variable<JobReportData> { Name = "JobReportData" };
            var isValid = new Variable<bool> { Name = "IsValid" };
            var validationError = new Variable<string> { Name = "ValidationError" };
            builder.WithVariable(jobReportData);
            builder.WithVariable(isValid);
            builder.WithVariable(validationError);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input 에서 JOBREPORT JSON 추출 → JobReportData
                    new ExtractJobReportFromInput
                    {
                        OutputData = new(jobReportData)
                    },

                    // Step 2: DB 검증 (JobID 로 TransportCommandEx 조회 + 정합성 확인)
                    new ValidateJobReportActivity
                    {
                        JobReportData = new(jobReportData),
                        Result = new(isValid),
                        ValidationError = new(validationError)
                    },

                    // Step 3: 검증 결과에 따라 분기
                    new If
                    {
                        Condition = new(ctx => isValid.Get(ctx)),
                        Then = new Sequence
                        {
                            Activities =
                            {
                                // Step 3a: MES 로 JOBREPORT 전달 (JSON→XML 변환 내부 수행)
                                new ForwardJobReportToMesActivity
                                {
                                    JobReportData = new(jobReportData)
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
