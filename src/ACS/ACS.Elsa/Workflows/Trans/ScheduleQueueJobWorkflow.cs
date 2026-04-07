using System.Collections.Generic;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// SCHEDULE-QUEUEJOB 워크플로우.
    ///
    /// Daemon 서버의 AwakeQueueTransportJob이 20초마다 트리거.
    /// Queued 상태의 TransportCommand를 Dest 기준 가장 가까운 idle AMR에 할당하고
    /// Host에 JOBREPORT(START), EI에 RAIL-CARRIERTRANSFER를 전송.
    ///
    /// 워크플로우 구조:
    ///   1. GetQueuedTransportCommands → queuedList
    ///   2. ForEach(queuedList):
    ///      a. FindSuitableVehicle(tc) → vehicle, found
    ///      b. If(found):
    ///         - AssignVehicleToTransportCommand(tc, vehicle)
    ///         - SendJobReportStart(tc, vehicleId)
    ///         - SendCarrierTransfer(tc, vehicleId)
    /// </summary>
    public class ScheduleQueueJobWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "SCHEDULE-QUEUEJOB";
            builder.Name = "SCHEDULE-QUEUEJOB";
            builder.Description = "Queued TC 스케줄링: idle AMR 할당 + JOBREPORT(START) + RAIL-CARRIERTRANSFER";

            // 워크플로우 변수
            var queuedList = new Variable<ICollection<TransportCommandEx>> { Name = "QueuedList" };
            var queuedCount = new Variable<int> { Name = "QueuedCount" };
            var currentTc = new Variable<TransportCommandEx> { Name = "CurrentTC" };
            var vehicle = new Variable<VehicleEx> { Name = "Vehicle" };
            var found = new Variable<bool> { Name = "Found" };

            builder.WithVariable(queuedList);
            builder.WithVariable(queuedCount);
            builder.WithVariable(currentTc);
            builder.WithVariable(vehicle);
            builder.WithVariable(found);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Queued TC 목록 조회 (워크플로우 input JSON에서 bayId 자동 추출)
                    new GetQueuedTransportCommandsActivity
                    {
                        QueuedCommands = new(queuedList),
                        Count = new(queuedCount)
                    },
                    
                    // Step 2: 각 TC에 대해 Vehicle 할당 시도
                    new ForEach<TransportCommandEx>
                    {
                        Items = new(ctx => queuedList.Get(ctx)),
                        CurrentValue = new(currentTc),
                        Body = new Sequence
                        {
                            Activities =
                            {
                                // 2a. 적합 Vehicle 검색
                                new FindSuitableVehicleActivity
                                {
                                    TransportCommand = new(ctx => currentTc.Get(ctx)),
                                    Vehicle = new(vehicle),
                                    Found = new(found)
                                },

                                // 2b. Vehicle을 찾은 경우에만 할당 + 전송
                                new If
                                {
                                    Condition = new(ctx => found.Get(ctx)),
                                    Then = new Sequence
                                    {
                                        Activities =
                                        {
                                            // 할당
                                            new AssignVehicleToTransportCommandActivity
                                            {
                                                TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                Vehicle = new(ctx => vehicle.Get(ctx))
                                            },

                                            // JOBREPORT(START) 전송
                                            new SendJobReportStartActivity
                                            {
                                                TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                VehicleId = new(ctx => vehicle.Get(ctx)?.VehicleId ?? "")
                                            },

                                            // RAIL-CARRIERTRANSFER 전송
                                            new SendCarrierTransferActivity
                                            {
                                                TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                VehicleId = new(ctx => vehicle.Get(ctx)?.VehicleId ?? "")
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
