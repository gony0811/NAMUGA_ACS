using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// SCHEDULE-CHARGEJOB 워크플로우.
    ///
    /// Daemon 의 AwakeChargeTransportJob 이 20초마다 Bay 단위로 트리거.
    /// 같은 Bay 안에서 빈 충전 슬롯(Location.Type=CHARGE) 이 있고
    /// JOB 이 끝난 IDLE 후보 vehicle 이 있으면 배터리 가장 낮은 1대를 충전소로 dispatch.
    ///
    /// 후보 조건(모두 만족):
    ///   ProcessingState=IDLE, RunState=STOP, TransferState=NOTASSIGNED,
    ///   Installed=T, TransportCommandId 비어있음,
    ///   ConnectionState != DISCONNECT, AlarmState = NOALARM
    /// </summary>
    public class ScheduleChargeJobWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "SCHEDULE-CHARGEJOB";
            builder.Name = "SCHEDULE-CHARGEJOB";
            builder.Description = "Bay 단위 충전 디스패치: 빈 충전 슬롯이 있고 IDLE 후보 vehicle 이 있으면 배터리 최저 1대 → 충전소";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new DispatchChargeJobActivity()
                }
            };
        }
    }
}
