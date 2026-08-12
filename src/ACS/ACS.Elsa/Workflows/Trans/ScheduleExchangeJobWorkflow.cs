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
    /// SCHEDULE-EXCHANGEJOB 워크플로우 (EXCHANGE v2 — S4 배차 슬라이스).
    ///
    /// Daemon 서버의 AwakeExchangeTransportJob 이 10초마다 트리거.
    /// EXCHANGE_QUEUED TC 를 Origin 최근접 + 4슬롯 EMPTY 인 idle AMR 에 배차하고
    /// EI 에 RAIL-CARRIERTRANSFER(Origin행), Host 에 EXCHANGE-JOBREPORT(START, Step=10) 전송.
    /// 기존 ScheduleQueueJobWorkflow 와 병렬 신규 경로 (D4/D5).
    ///
    /// 워크플로우 구조:
    ///   1. GetExchangeQueuedTransportCommands → queuedList
    ///   2. ForEach(queuedList):
    ///      a. FindSuitableExchangeVehicle(tc) → vehicle, found (IDLE+CONNECT+4슬롯 EMPTY)
    ///      b. If(found):
    ///         - AssignExchangeVehicle(tc, vehicle) → loadSlot, assignSuccess
    ///           (슬롯 페어 예약 + EXCHANGE_ASSIGNED 전이 + LOADSLOT/UNLOADSLOT 기록)
    ///         - If(assignSuccess):
    ///             - SendCarrierTransferWithRetry(tc, vehicleId, UNLOAD, useSource=true)
    ///               → Origin 픽업 지시 (5초 타임아웃, 최대 3회 재시도, 기존 액티비티 재사용)
    ///             - If(transferSuccess):
    ///                 Then: SendExchangeJobReportStart(tc, vehicleId)  — Step=10, PICKUP_NEW
    ///                 Else: RollbackExchangeAssignment(tc, vehicle)   — 슬롯 예약 해제 포함
    /// </summary>
    public class ScheduleExchangeJobWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "SCHEDULE-EXCHANGEJOB";
            builder.Name = "SCHEDULE-EXCHANGEJOB";
            builder.Description = "EXCHANGE_QUEUED TC 배차: 슬롯 적격 AMR 할당 + RAIL-CARRIERTRANSFER(Origin) + EXCHANGE-JOBREPORT(START)";

            var queuedList = new Variable<ICollection<TransportCommandEx>> { Name = "ExchangeQueuedList" };
            var queuedCount = new Variable<int> { Name = "ExchangeQueuedCount" };
            var currentTc = new Variable<TransportCommandEx> { Name = "CurrentTC" };
            var vehicle = new Variable<VehicleEx> { Name = "Vehicle" };
            var found = new Variable<bool> { Name = "Found" };
            var loadSlot = new Variable<string> { Name = "LoadSlot" };
            var assignSuccess = new Variable<bool> { Name = "AssignSuccess" };
            var transferSuccess = new Variable<bool> { Name = "TransferSuccess" };

            builder.WithVariable(queuedList);
            builder.WithVariable(queuedCount);
            builder.WithVariable(currentTc);
            builder.WithVariable(vehicle);
            builder.WithVariable(found);
            builder.WithVariable(loadSlot);
            builder.WithVariable(assignSuccess);
            builder.WithVariable(transferSuccess);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: EXCHANGE_QUEUED TC 목록 조회 (input JSON에서 bayId 추출)
                    new GetExchangeQueuedTransportCommandsActivity
                    {
                        QueuedCommands = new(queuedList),
                        Count = new(queuedCount)
                    },

                    // Step 2: 각 TC에 대해 배차 시도
                    new ForEach<TransportCommandEx>
                    {
                        Items = new(ctx => queuedList.Get(ctx)),
                        CurrentValue = new(currentTc),
                        Body = new Sequence
                        {
                            Activities =
                            {
                                // 2a. 적격 차량 검색 (IDLE + CONNECT + 기할당없음 + 4슬롯 EMPTY)
                                new FindSuitableExchangeVehicleActivity
                                {
                                    TransportCommand = new(ctx => currentTc.Get(ctx)),
                                    Vehicle = new(vehicle),
                                    Found = new(found)
                                },

                                new If
                                {
                                    Condition = new(ctx => found.Get(ctx)),
                                    Then = new Sequence
                                    {
                                        Activities =
                                        {
                                            // 2b. 슬롯 페어 예약 + EXCHANGE_ASSIGNED 전이
                                            new AssignExchangeVehicleActivity
                                            {
                                                TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                Vehicle = new(ctx => vehicle.Get(ctx)),
                                                LoadSlot = new(loadSlot),
                                                Success = new(assignSuccess)
                                            },

                                            new If
                                            {
                                                Condition = new(ctx => assignSuccess.Get(ctx)),
                                                Then = new Sequence
                                                {
                                                    Activities =
                                                    {
                                                        // 2c. RAIL-CARRIERTRANSFER(Origin행 픽업) — 기존 액티비티 재사용
                                                        new SendCarrierTransferWithRetryActivity
                                                        {
                                                            TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                            VehicleId = new(ctx => vehicle.Get(ctx)?.VehicleId ?? ""),
                                                            JobType = new(TransportCommandEx.JOBTYPE_UNLOAD),
                                                            UseSource = new(true),
                                                            Success = new(transferSuccess)
                                                        },

                                                        new If
                                                        {
                                                            Condition = new(ctx => transferSuccess.Get(ctx)),

                                                            // 성공: EXCHANGE-JOBREPORT(START, Step=10, PICKUP_NEW)
                                                            Then = new SendExchangeJobReportStartActivity
                                                            {
                                                                TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                                VehicleId = new(ctx => vehicle.Get(ctx)?.VehicleId ?? "")
                                                            },

                                                            // 실패: TC/Vehicle/슬롯 예약 롤백
                                                            Else = new RollbackExchangeAssignmentActivity
                                                            {
                                                                TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                                Vehicle = new(ctx => vehicle.Get(ctx))
                                                            }
                                                        }
                                                    }
                                                },

                                                // 슬롯 페어 예약 실패 등 배차 실패: 잔여 예약 정리
                                                Else = new RollbackExchangeAssignmentActivity
                                                {
                                                    TransportCommand = new(ctx => currentTc.Get(ctx)),
                                                    Vehicle = new(ctx => vehicle.Get(ctx))
                                                }
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
