using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  Schedule Exchange Activities (EXCHANGE v2 — S4 배차 슬라이스)
    //  Category: ACS.Schedule.Exchange
    //
    //  EXCHANGE_QUEUED TC 배차: 조회 → 적격 차량(IDLE+CONNECT+4슬롯 EMPTY) →
    //  슬롯 페어 예약(ReserveExchangePair) + EXCHANGE_ASSIGNED 전이 →
    //  RAIL-CARRIERTRANSFER(Origin행, 기존 액티비티 재사용) →
    //  JOBREPORT(START, Step=10, PICKUP_NEW) 회신.
    //  기존 QUEUED 배차 경로(ScheduleActivities.cs)는 무수정 — 병렬 신규 경로 (D4/D5).
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// EXCHANGE_QUEUED 상태의 TransportCommand 목록을 조회.
    /// GetQueuedTransportCommandsActivity 미러 (조회 상태만 EXCHANGE_QUEUED).
    /// </summary>
    [Activity("ACS.Schedule.Exchange", "Get Exchange Queued TCs",
        "EXCHANGE_QUEUED 상태 TransportCommand 목록 조회 (BayId 필터)")]
    public class GetExchangeQueuedTransportCommandsActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Output(Description = "EXCHANGE_QUEUED TC 목록")]
        public Output<ICollection<TransportCommandEx>> QueuedCommands { get; set; }

        [Output(Description = "EXCHANGE_QUEUED TC 수")]
        public Output<int> Count { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                if (transferManager == null)
                {
                    logger.Error("GetExchangeQueuedTransportCommandsActivity: ITransferManagerEx not available");
                    context.Set(QueuedCommands, (ICollection<TransportCommandEx>)new List<TransportCommandEx>());
                    context.Set(Count, 0);
                    return;
                }

                string bayId = ExtractBayIdFromInput(context);
                if (string.IsNullOrEmpty(bayId))
                {
                    logger.Warn("GetExchangeQueuedTransportCommandsActivity: bayId not found in input — skip");
                    context.Set(QueuedCommands, (ICollection<TransportCommandEx>)new List<TransportCommandEx>());
                    context.Set(Count, 0);
                    return;
                }

                IList rawList = transferManager.GetExchangeQueuedTransportCommandsByBayId(bayId);
                var list = rawList?.Cast<TransportCommandEx>().ToList() ?? new List<TransportCommandEx>();
                context.Set(QueuedCommands, (ICollection<TransportCommandEx>)list);
                context.Set(Count, list.Count);

                if (list.Count > 0)
                    logger.Info($"GetExchangeQueuedTransportCommandsActivity: {list.Count} exchange-queued TC(s) found (bayId={bayId})");
            }
            catch (Exception ex)
            {
                logger.Error($"GetExchangeQueuedTransportCommandsActivity: {ex.Message}", ex);
                context.Set(QueuedCommands, (ICollection<TransportCommandEx>)new List<TransportCommandEx>());
                context.Set(Count, 0);
            }
        }

        private string ExtractBayIdFromInput(ActivityExecutionContext context)
        {
            try
            {
                if (!context.WorkflowExecutionContext.Input.TryGetValue("Arguments", out var argsObj))
                    return "";

                var args = argsObj as object[];
                if (args == null || args.Length == 0)
                    return "";

                string jsonStr = args[0] as string;
                if (string.IsNullOrEmpty(jsonStr))
                    return "";

                var msg = JsonSerializer.Deserialize<DaemonScheduleMessage>(jsonStr);
                return msg?.Data?.BayId ?? "";
            }
            catch (Exception ex)
            {
                logger.Warn($"GetExchangeQueuedTransportCommandsActivity: Failed to extract bayId from input: {ex.Message}");
                return "";
            }
        }
    }

    /// <summary>
    /// EXCHANGE 배차 적격 차량 검색: Origin(=tc.Source) 최근접 + IDLE + CONNECT +
    /// 기할당 없음 + 4슬롯 전부 EMPTY (트립 중간 상태 차량 배제, §4.7).
    /// FindSuitableVehicleActivity 미러 + 슬롯 적격 검사 추가.
    /// </summary>
    [Activity("ACS.Schedule.Exchange", "Find Suitable Exchange Vehicle",
        "Origin 최근접 idle 차량 검색 + 4슬롯 EMPTY 적격 검사")]
    public class FindSuitableExchangeVehicleActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "대상 EXCHANGE TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Output(Description = "검색된 Vehicle (없으면 null)")]
        public Output<VehicleEx> Vehicle { get; set; }

        [Output(Description = "적합한 Vehicle을 찾았는지 여부")]
        public Output<bool> Found { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            if (tc == null)
            {
                context.Set(Found, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var cacheManager = accessor?.Resolve<ICacheManagerEx>();
                var pathManager = accessor?.Resolve<IPathManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var slotManager = accessor?.Resolve<ISlotManagerEx>();

                if (cacheManager == null || pathManager == null || transferManager == null || slotManager == null)
                {
                    logger.Error("FindSuitableExchangeVehicleActivity: Required services not available");
                    context.Set(Found, false);
                    return;
                }

                // Origin(=Source) Location 조회 — 픽업 지점 기준 최근접 차량
                LocationEx sourceLocation = cacheManager.GetLocationByLocationId(tc.Source);
                if (sourceLocation == null)
                {
                    logger.Warn($"FindSuitableExchangeVehicleActivity: Location not found for source={tc.Source}");
                    context.Set(Found, false);
                    return;
                }

                VehicleEx vehicle = pathManager.SearchSuitableVehicle(sourceLocation, tc.BayId);
                if (vehicle == null)
                {
                    // 후보 조회 조건: CONNECT + ALIVE + IDLE + FullState=EMPTY + Installed=T (bayId 내)
                    logger.Info($"FindSuitableExchangeVehicleActivity: 후보 차량 없음 — " +
                                $"bayId={tc.BayId} 에 IDLE/CONNECT/ALIVE/FullState=EMPTY 차량 부재 (tc={tc.JobId})");
                    context.Set(Found, false);
                    return;
                }

                // 상태 검증: IDLE + CONNECT
                if (vehicle.ProcessingState != VehicleEx.PROCESSINGSTATE_IDLE ||
                    vehicle.ConnectionState != VehicleEx.CONNECTIONSTATE_CONNECT)
                {
                    logger.Info($"FindSuitableExchangeVehicleActivity: vehicle {vehicle.VehicleId} 상태 부적격 — " +
                                $"processingState={vehicle.ProcessingState}, connectionState={vehicle.ConnectionState} (tc={tc.JobId})");
                    context.Set(Found, false);
                    return;
                }

                // 기존 할당 확인
                var existingTc = transferManager.GetTransportCommandByVehicleId(vehicle.VehicleId);
                if (existingTc != null)
                {
                    logger.Info($"FindSuitableExchangeVehicleActivity: vehicle {vehicle.VehicleId} 기할당 — " +
                                $"existingTc={existingTc.JobId} (tc={tc.JobId})");
                    context.Set(Found, false);
                    return;
                }

                // 슬롯 적격: 미시드 차량 시딩 후 4슬롯 전부 EMPTY 여야 함
                slotManager.EnsureSlots(vehicle.VehicleId);
                if (!slotManager.AreAllSlotsEmpty(vehicle.VehicleId))
                {
                    var occupied = new List<string>();
                    foreach (var s in slotManager.GetSlots(vehicle.VehicleId))
                    {
                        if (!VehicleSlotEx.STATE_EMPTY.Equals(s.State, StringComparison.OrdinalIgnoreCase))
                            occupied.Add($"slot{s.SlotNo}={s.State}({s.JobId})");
                    }
                    logger.Info($"FindSuitableExchangeVehicleActivity: vehicle {vehicle.VehicleId} 슬롯 부적격 — " +
                                $"{string.Join(", ", occupied)} (tc={tc.JobId})");
                    context.Set(Found, false);
                    return;
                }

                context.Set(Vehicle, vehicle);
                context.Set(Found, true);
            }
            catch (Exception ex)
            {
                logger.Error($"FindSuitableExchangeVehicleActivity: {ex.Message}", ex);
                context.Set(Found, false);
            }
        }
    }

    /// <summary>
    /// EXCHANGE 배차: 슬롯 페어 예약(ReserveExchangePair) + TC EXCHANGE_ASSIGNED 전이 +
    /// AdditionalInfo(LOADSLOT/UNLOADSLOT) 기록 + Vehicle 상태 전이.
    /// AssignVehicleToTransportCommandActivity 미러 + 슬롯 예약 추가.
    /// TRIP 은 배칭 미도입(S4)이라 빈 값 유지, STEP 은 도착 전이므로 10 유지.
    /// </summary>
    [Activity("ACS.Schedule.Exchange", "Assign Exchange Vehicle",
        "슬롯 페어 예약 + TC EXCHANGE_ASSIGNED 전이 + Vehicle 상태 전이")]
    public class AssignExchangeVehicleActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "할당 대상 EXCHANGE TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "할당할 Vehicle")]
        public Input<VehicleEx> Vehicle { get; set; }

        [Output(Description = "예약된 투입(LOADSLOT) 슬롯 번호 (실패 시 빈 문자열)")]
        public Output<string> LoadSlot { get; set; }

        [Output(Description = "할당 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicle = Vehicle?.Get(context);

            if (tc == null || vehicle == null)
            {
                logger.Error("AssignExchangeVehicleActivity: TC or Vehicle is null");
                context.Set(LoadSlot, "");
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var slotManager = accessor?.Resolve<ISlotManagerEx>();

                if (transferManager == null || resourceManager == null || slotManager == null)
                {
                    logger.Error("AssignExchangeVehicleActivity: Required services not available");
                    context.Set(LoadSlot, "");
                    context.Set(Success, false);
                    return;
                }

                // 슬롯 페어 예약: INSERT 군 1개 + RETRIEVE 군 1개 (낮은 번호 우선, D3)
                Tuple<int, int> pair = slotManager.ReserveExchangePair(vehicle.VehicleId, tc.JobId);
                if (pair == null)
                {
                    logger.Warn($"AssignExchangeVehicleActivity: slot pair reservation failed - vehicle={vehicle.VehicleId}, tc={tc.JobId}");
                    context.Set(LoadSlot, "");
                    context.Set(Success, false);
                    return;
                }

                // TC 할당: EXCHANGE_ASSIGNED 전이 + 슬롯 번호 기록
                tc.VehicleId = vehicle.VehicleId;
                tc.State = TransportCommandEx.STATE_EXCHANGE_ASSIGNED;
                tc.AssignedTime = DateTime.Now;
                tc.AdditionalInfo = ExchangeInfo.Set(
                    ExchangeInfo.Set(tc.AdditionalInfo, ExchangeInfo.KEY_LOADSLOT, pair.Item1.ToString()),
                    ExchangeInfo.KEY_UNLOADSLOT, pair.Item2.ToString());
                transferManager.UpdateTransportCommand(tc);

                // Vehicle에 TC 할당
                resourceManager.UpdateVehicleTransportCommandId(vehicle, tc.JobId);
                resourceManager.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_ASSIGNED);
                resourceManager.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_RUN);

                // AcsDestNodeId = Origin(Source) StationId — 도착 검출 워크플로우 전제 (기존 배차와 동일 관례)
                var cacheManager = accessor?.Resolve<ICacheManagerEx>();
                if (cacheManager != null && !string.IsNullOrEmpty(tc.Source))
                {
                    LocationEx sourceLoc = cacheManager.GetLocationByLocationId(tc.Source);
                    if (sourceLoc != null && !string.IsNullOrEmpty(sourceLoc.StationId))
                    {
                        resourceManager.UpdateVehicleAcsDestNodeId(vehicle, sourceLoc.StationId, "SCHEDULE-EXCHANGEJOB");
                        vehicle.AcsDestNodeId = sourceLoc.StationId;
                    }
                    else
                    {
                        logger.Warn($"AssignExchangeVehicleActivity: source Location/StationId 조회 실패 source={tc.Source}, tc={tc.JobId}");
                    }
                }

                logger.Info($"AssignExchangeVehicleActivity: TC {tc.JobId} → Vehicle {vehicle.VehicleId}, slots load={pair.Item1}/unload={pair.Item2}, state={tc.State}");
                context.Set(LoadSlot, pair.Item1.ToString());
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"AssignExchangeVehicleActivity: {ex.Message}", ex);
                context.Set(LoadSlot, "");
                context.Set(Success, false);
            }
        }
    }

    /// <summary>
    /// EXCHANGE 배차 실패 롤백: TC→EXCHANGE_QUEUED 복원 + 슬롯 예약 해제 + Vehicle 복원.
    /// RollbackVehicleAssignmentActivity 미러 — 상태 격리(D5) 덕에 가드는
    /// EXCHANGE_ASSIGNED 여부만 확인하면 됨 (S4 에서 배차 후 상태는 이것뿐).
    /// </summary>
    [Activity("ACS.Schedule.Exchange", "Rollback Exchange Assignment",
        "배차 실패 시 TC/Vehicle/슬롯 예약 롤백")]
    public class RollbackExchangeAssignmentActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "롤백 대상 EXCHANGE TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "롤백 대상 Vehicle")]
        public Input<VehicleEx> Vehicle { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicle = Vehicle?.Get(context);

            if (tc == null || vehicle == null)
            {
                logger.Error("RollbackExchangeAssignmentActivity: TC or Vehicle is null");
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var slotManager = accessor?.Resolve<ISlotManagerEx>();

                if (transferManager == null || resourceManager == null || slotManager == null)
                {
                    logger.Error("RollbackExchangeAssignmentActivity: Required services not available");
                    return;
                }

                // 롤백 본체는 ExchangeTransHandlers.RollbackToQueued 로 일원화 (REJECTED@STEP10 롤백과 공유).
                // TC → EXCHANGE_QUEUED(슬롯 기록/ACT/ARRIVED 초기화, STEP=10), 슬롯 예약 해제, 차량 → NOTASSIGNED/IDLE.
                bool rolled = ExchangeTransHandlers.RollbackToQueued(tc, vehicle,
                    transferManager, resourceManager, slotManager, "assignment-rollback");
                if (!rolled)
                    logger.Warn($"RollbackExchangeAssignmentActivity: TC 상태 롤백 생략(예상외 상태) — tc={tc.JobId}");
                else
                    logger.Info($"RollbackExchangeAssignmentActivity: 롤백 완료 - TC {tc.JobId} → EXCHANGE_QUEUED, Vehicle {vehicle.VehicleId} → NOTASSIGNED/IDLE, 슬롯 예약 해제");
            }
            catch (Exception ex)
            {
                logger.Error($"RollbackExchangeAssignmentActivity: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// EXCHANGE JOBREPORT(START, Step=10, StepName=PICKUP_NEW) 를 Host 프로세스로 발행.
    /// 기존 JOBREPORT 릴레이에는 Step/StepName/CarrierSlot 필드가 없어(D4)
    /// messageName="EXCHANGE-JOBREPORT" 병렬 경로를 사용한다.
    /// </summary>
    [Activity("ACS.Schedule.Exchange", "Send Exchange JobReport START",
        "EXCHANGE JOBREPORT(START, Step=10, PICKUP_NEW) Host 프로세스 발행")]
    public class SendExchangeJobReportStartActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "EXCHANGE TransportCommand")]
        public Input<TransportCommandEx> TransportCommand { get; set; }

        [Input(Description = "할당된 Vehicle ID")]
        public Input<string> VehicleId { get; set; }

        [Output(Description = "전송 성공 여부")]
        public Output<bool> Success { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var tc = TransportCommand?.Get(context);
            var vehicleId = VehicleId?.Get(context);

            if (tc == null || string.IsNullOrEmpty(vehicleId))
            {
                context.Set(Success, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendExchangeJobReportStartActivity: IMessageManagerEx not resolved");
                    context.Set(Success, false);
                    return;
                }

                string loadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_LOADSLOT) ?? "";

                messageManager.SendExchangeJobReportToHost(
                    "START", tc.JobId, vehicleId,
                    "10", "PICKUP_NEW", loadSlot,
                    "EXCHANGE", tc.GetMaterialType() ?? "", "0", "");

                logger.Info($"SendExchangeJobReportStartActivity: EXCHANGE-JOBREPORT(START, Step=10) sent for TC {tc.JobId}, amr={vehicleId}, loadSlot={loadSlot}");
                context.Set(Success, true);
            }
            catch (Exception ex)
            {
                logger.Error($"SendExchangeJobReportStartActivity: {ex.Message}", ex);
                context.Set(Success, false);
            }
        }
    }
}
