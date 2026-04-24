using System.Collections.Generic;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Core.Resource.Model;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// SCHEDULE-CHECKVEHICLES 워크플로우.
    ///
    /// Daemon 서버의 AwakeCheckVehiclesJob이 10초마다 트리거.
    /// 모든 Vehicle의 EventTime을 검사하여 1분 이상 갱신되지 않은 Vehicle을
    /// DISCONNECT 상태로 변경. PARK/CHARGE 상태의 Vehicle은 제외.
    ///
    /// 워크플로우 구조:
    ///   1. CheckVehiclesEventTime → staleVehicles, staleCount
    ///   2. If(staleCount > 0):
    ///      - DisconnectVehicles(staleVehicles)
    /// </summary>
    public class ScheduleCheckvehiclesWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            
            builder.DefinitionId = "SCHEDULE-CHECKVEHICLES";
            builder.Name = "SCHEDULE-CHECKVEHICLES";
            builder.Description = "Vehicle EventTime 검사: 1분 이상 미갱신 Vehicle DISCONNECT 처리";
            
            // 워크플로우 변수
            var staleVehicles = new Variable<ICollection<VehicleEx>> { Name = "StaleVehicles" };
            var staleCount = new Variable<int> { Name = "StaleCount" };

            builder.WithVariable(staleVehicles);
            builder.WithVariable(staleCount);
            
            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: EventTime이 만료된 Vehicle 목록 조회
                    new CheckVehiclesEventTimeActivity
                    {
                        StaleVehicles = new(staleVehicles),
                        StaleCount = new(staleCount)
                    },

                    // Step 2: 만료 Vehicle이 있으면 DISCONNECT 처리
                    new If
                    {
                        Condition = new(ctx => staleCount.Get(ctx) > 0),
                        Then = new DisconnectVehiclesActivity
                        {
                            Vehicles = new(ctx => staleVehicles.Get(ctx))
                        }
                    }
                }
            };
        }
    }
}
