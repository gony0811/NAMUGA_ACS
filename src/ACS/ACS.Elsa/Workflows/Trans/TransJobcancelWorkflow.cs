using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Communication.Mqtt.Model;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// TRANS-JOBCANCEL 워크플로우 (Trans 프로세스).
    ///
    /// Host 가 릴레이한 JOBCANCEL(공통 취소)을 수신하여 C1~C4 를 판정·실행하고
    /// JOBREPORT(Type=CANCEL, ErrorCode) 를 회신한다 (시나리오 사양서 "취소·오류" 시트):
    ///  - C1 배차 전: 즉시 취소 (이력 이관)
    ///  - C2 픽업 전: cancelCmd + 즉시 취소 + 차량 IDLE
    ///  - C3 적재 후: 승인 보고 + cancelCmd + Job 삭제 + 충전소 복귀 + 차량 ALARM (작업자 실물 회수)
    ///  - C4 종료 상태: 거부 (CANCEL_REJECTED)
    /// </summary>
    public class TransJobcancelWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "TRANS-JOBCANCEL";
            builder.Name = "TRANS-JOBCANCEL";
            builder.Description = "JOBCANCEL 판정(C1~C4)·실행 → JOBREPORT(CANCEL) 회신";

            var jobCancel = new Variable<ActionCmdMessage> { Name = "JobCancel" };
            builder.WithVariable(jobCancel);

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ExtractActionCmdFromInput
                    {
                        OutputData = new(jobCancel)
                    },
                    new JudgeAndExecuteJobCancelActivity
                    {
                        JobCancel = new(jobCancel)
                    }
                }
            };
        }
    }
}
