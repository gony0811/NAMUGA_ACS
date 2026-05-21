using System;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Communication.Msb;
using ACS.Core.Cache;
using ACS.Core.History;
using ACS.Core.Logging;
using ACS.Core.Message.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Database.Model.Resource;
using ACS.Core.Workflow;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEUPDATE 워크플로우.
    ///
    /// EI 프로세스에서 AMR 상태+위치를 JSON 메시지로 전송하면,
    /// Trans 프로세스의 ESListener가 수신하여 이 워크플로우를 실행한다.
    /// 모든 Vehicle 상태(RunState, FullState, AlarmState, Battery 등)와
    /// 위치(CurrentNodeId)를 일괄 업데이트한다.
    /// </summary>
    public class RailVehicleUpdateWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEUPDATE";
            builder.Name = "RAIL-VEHICLEUPDATE";
            builder.Description = "AMR 상태+위치 JSON 메시지 수신 시 Vehicle 일괄 업데이트";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new RailVehicleUpdateActivity(),
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEUPDATE 처리 Activity.
    /// EI에서 전송한 JSON 메시지를 역직렬화하여 IResourceManagerEx를 통해
    /// Vehicle의 모든 상태와 위치를 업데이트한다.
    /// </summary>
    [Activity("ACS.Trans", "Rail Vehicle Update",
        "AMR 상태+위치 JSON으로 Vehicle 일괄 업데이트")]
    public class RailVehicleUpdateActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(RailVehicleUpdateActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 Arguments 추출: [jsonString]
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("RailVehicleUpdateActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var json = args[0] as string;
                if (string.IsNullOrEmpty(json))
                {
                    logger.Error("RailVehicleUpdateActivity: JSON 메시지가 null입니다.");
                    return;
                }

                // JSON 역직렬화
                var updateMessage = JsonSerializer.Deserialize<RailVehicleUpdateMessage>(json);
                if (updateMessage?.Data == null)
                {
                    logger.Error("RailVehicleUpdateActivity: JSON 역직렬화 실패.");
                    return;
                }

                var data = updateMessage.Data;
                if (logger.IsDebugEnabled)
                    logger.Debug($"RailVehicleUpdateActivity 시작: vehicleId={data.VehicleId}, commId={data.CommId}, nodeChanged={data.NodeChanged}");

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("RailVehicleUpdateActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                if (resourceManager == null)
                {
                    logger.Error("RailVehicleUpdateActivity: IResourceManagerEx를 찾을 수 없습니다.");
                    return;
                }

                // Vehicle 조회
                VehicleEx vehicle = resourceManager.GetVehicle(data.VehicleId);
                if (vehicle == null)
                {
                    logger.Warn($"RailVehicleUpdateActivity: Vehicle을 찾을 수 없습니다. vehicleId={data.VehicleId}");
                    return;
                }

                // 1. ConnectionState → CONNECT
                if (!"CONNECT".Equals(vehicle.ConnectionState))
                {
                    resourceManager.UpdateVehicleConnectionState(vehicle, data.ConnectionState);
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle ConnectionState → {data.ConnectionState}: vehicleId={data.VehicleId}");
                }

                if (!"BANNED".Equals(vehicle.State))
                {
                    resourceManager.UpdateVehicleState(vehicle, Vehicle.STATE_ALIVE, "RAIL-VEHICLEUPDATE");
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle State → ALIVE: vehicleId={data.VehicleId}");
                }



                // 2. RunState 업데이트
                if (!string.IsNullOrEmpty(data.RunState) && data.RunState != vehicle.RunState)
                {
                    string prev = vehicle.RunState;
                    resourceManager.UpdateVehicleRunState(vehicle, data.RunState);
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle RunState 업데이트: {prev} → {data.RunState}, vehicleId={data.VehicleId}");
                }


                // 3. FullState 업데이트
                if (!string.IsNullOrEmpty(data.FullState) && data.FullState != vehicle.FullState)
                {
                    string prev = vehicle.FullState;
                    resourceManager.UpdateVehicleFullState(vehicle, data.FullState);
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle FullState 업데이트: {prev} → {data.FullState}, vehicleId={data.VehicleId}");
                }

                // AlarmState 업데이트는 RAIL-VEHICLEALARM 워크플로우에서 SET/RESET 으로 처리.

                // 5. BatteryRate 업데이트
                if (data.BatteryRate != vehicle.BatteryRate)
                {
                    int prev = vehicle.BatteryRate;
                    resourceManager.UpdateVehicleBatteryRate(vehicle, data.BatteryRate);
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle BatteryRate 업데이트: {prev} → {data.BatteryRate}, vehicleId={data.VehicleId}");
                }

                // 6. BatteryVoltage 업데이트
                if (Math.Abs(data.BatteryVoltage - vehicle.BatteryVoltage) > 0.01f)
                {
                    float prev = vehicle.BatteryVoltage;
                    resourceManager.UpdateVehicleBatteryVoltage(vehicle, data.BatteryVoltage);
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle BatteryVoltage 업데이트: {prev} → {data.BatteryVoltage}, vehicleId={data.VehicleId}");
                }


                // 7. VehicleDestNodeId 업데이트
                if (data.VehicleDestNodeId != vehicle.VehicleDestNodeId)
                {
                    string prev = vehicle.VehicleDestNodeId;
                    resourceManager.UpdateVehicleVehicleDestNodeId(vehicle, data.VehicleDestNodeId);
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Vehicle VehicleDestNodeId 업데이트: {prev} → {data.VehicleDestNodeId}, vehicleId={data.VehicleId}");
                }

                // 7. 노드 변경 시 CurrentNodeId 업데이트 (충전 노드 도착 시 ProcessingState → CHARGE)
                //    같은 메시지 안에서 도착 + 충전 완료(BatteryRate≥30) 가 동시 성립하는 경우를 처리하기 위해
                //    Step 8(CHARGE→IDLE + TC 정리) 보다 먼저 평가한다.
                if (data.NodeChanged && !string.IsNullOrEmpty(data.CurrentNodeId))
                {
                    var cacheManager = accessor.Resolve<ICacheManagerEx>();
                    NodeEx node = cacheManager?.GetNode(data.CurrentNodeId);
                    if (node == null)
                    {
                        logger.Debug($"RailVehicleUpdateActivity: 등록되지 않은 노드. nodeId={data.CurrentNodeId}");
                    }
                    else
                    {
                        string previousNodeId = vehicle.CurrentNodeId;
                        resourceManager.UpdateVehicleLocation(vehicle, data.CurrentNodeId);
                        if (logger.IsDebugEnabled)
                            logger.Debug($"Vehicle 위치 업데이트: {previousNodeId} → {data.CurrentNodeId}, vehicleId={data.VehicleId}");

                        // NA_R_NODE.Type == CHARGE 노드 도착 시 ProcessingState → CHARGE
                        if (NodeEx.TYPE_CHARGE.Equals(node.Type, StringComparison.OrdinalIgnoreCase) &&
                            !VehicleEx.PROCESSINGSTATE_CHARGE.Equals(vehicle.ProcessingState, StringComparison.OrdinalIgnoreCase))
                        {
                            resourceManager.UpdateVehicleProcessingState(data.VehicleId,
                                VehicleEx.PROCESSINGSTATE_CHARGE, "RAIL-VEHICLEUPDATE");
                            // UpdateVehicleProcessingState(vehicleId,...) 는 새 VehicleEx 를 fetch 해 DB 만 갱신하므로
                            // 이 활동 안에서 이어지는 Step 8 조건 평가가 정확하도록 인메모리 객체도 동기화한다.
                            vehicle.ProcessingState = VehicleEx.PROCESSINGSTATE_CHARGE;
                            logger.Info($"Vehicle ProcessingState → CHARGE (충전 노드 도착): vehicleId={data.VehicleId}, nodeId={data.CurrentNodeId}");
                        }

                        // AcsDestNodeId(=source phase 목적지) 도착 시 RAIL-VEHICLEDESTARRIVED 디스패치.
                        // acquire-complete 이전(STATE_ASSIGNED) 에만 발화하며, 이후 acquire-complete 가
                        // AcsDestNodeId 를 dest 로 덮어쓰므로 dest 도착 시 자동으로 재발화하지 않는다.
                        DispatchDestArrivedIfNeeded(accessor, data.CurrentNodeId, vehicle);
                    }
                }

                // 8. ProcessingState: 충전 완료(CHARGE → IDLE) 전이만 책임진다.
                //    RUN(Job 진행 중)은 절대 덮어쓰지 않아 Job 중복 할당을 방지하고,
                //    CHARGE 진입은 Step 7(충전 노드 도착)에서 처리한다.
                const int BATTERY_CHARGE_RELEASE_RATE = 30;
                bool inCharge = VehicleEx.PROCESSINGSTATE_CHARGE.Equals(
                    vehicle.ProcessingState, StringComparison.OrdinalIgnoreCase);
                if (inCharge && data.BatteryRate >= BATTERY_CHARGE_RELEASE_RATE)
                {
                    logger.Info($"ChargeJob 완료 조건 진입: vehicleId={data.VehicleId}, " +
                                $"ProcessingState={vehicle.ProcessingState}, BatteryRate={data.BatteryRate}, threshold={BATTERY_CHARGE_RELEASE_RATE}");

                    // 충전 완료 → CHARGEMOVE TC 정리: History 이관 → NA_T_TRANSPORTCMD 삭제 → Vehicle 측 FK/잔여 필드 클리어
                    var transferManager = accessor.Resolve<ITransferManagerEx>();
                    var historyManager = accessor.Resolve<IHistoryManagerEx>();

                    if (transferManager != null && historyManager != null)
                    {
                        TransportCommandEx tc = transferManager.GetTransportCommandByVehicleId(vehicle.VehicleId);
                        if (tc == null)
                        {
                            logger.Info($"ChargeJob 완료: 정리할 TC 없음 vehicleId={vehicle.VehicleId}");
                        }
                        else if (!TransportCommandEx.JOBTYPE_CHARGEMOVE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                        {
                            logger.Warn($"ChargeJob 완료: TC 발견했으나 JobType 불일치 — 정리 건너뜀. " +
                                        $"vehicleId={vehicle.VehicleId}, tc={tc.JobId}, jobType={tc.JobType}");
                        }
                        else
                        {
                            historyManager.CreateTransportCommandHistory(tc, "", TransportCommandEx.STATE_CHARGE_COMPLETED);

                            int deleted = transferManager.DeleteTransportCommand(tc);
                            if (deleted > 0)
                                logger.Info($"ChargeJob 완료: TC 삭제 tc={tc.JobId}, vehicleId={vehicle.VehicleId}, deleted={deleted}");
                            else
                                logger.Error($"ChargeJob 완료: TC 삭제 호출했으나 0행 영향 tc={tc.JobId}, vehicleId={vehicle.VehicleId}");

                            resourceManager.UpdateVehicleTransportCommandId(vehicle, "", "RAIL-VEHICLEUPDATE");
                            resourceManager.UpdateVehicleAcsDestNodeId(vehicle, "", "RAIL-VEHICLEUPDATE");
                            resourceManager.UpdateVehicle(vehicle, "Path", "");
                            vehicle.TransportCommandId = "";
                            vehicle.AcsDestNodeId = "";
                            vehicle.Path = "";
                        }
                    }
                    else
                    {
                        logger.Warn("ChargeJob 완료 처리: ITransferManagerEx/IHistoryManagerEx 해석 실패, TC 정리 건너뜀");
                    }

                    resourceManager.UpdateVehicleProcessingState(data.VehicleId,
                        VehicleEx.PROCESSINGSTATE_IDLE, "RAIL-VEHICLEUPDATE");
                    // 이후 같은 활동에서 재참조 가능성 + 다음 메시지 fetch 전 일관성 확보
                    vehicle.ProcessingState = VehicleEx.PROCESSINGSTATE_IDLE;
                    logger.Info($"Vehicle ProcessingState CHARGE → IDLE (BatteryRate={data.BatteryRate}% ≥ {BATTERY_CHARGE_RELEASE_RATE}%): vehicleId={data.VehicleId}");
                }

                // 9. EventTime 업데이트
                resourceManager.UpdateVehicleEventTime(vehicle);

                if (logger.IsDebugEnabled)
                    logger.Debug($"RailVehicleUpdateActivity 완료: vehicleId={data.VehicleId}");

                // 10. UI 프로세스로 원본 JSON 그대로 forward (POSE 포함, 1Hz 텔레메트리)
                //     UI BackgroundService가 SignalR로 클라이언트에 브로드캐스트한다.
                ForwardToUi(accessor, json);
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleUpdateActivity 오류", e);
            }
        }

        private static void ForwardToUi(Bridge.AutofacContainerAccessor accessor, string json)
        {
            try
            {
                var uiAgent = accessor.ResolveNamed<IMessageAgent>("UiAgentSender");
                if (uiAgent == null)
                {
                    logger.Warn("RailVehicleUpdateActivity: UiAgentSender 미등록 — UI forward skip");
                    return;
                }
                uiAgent.Send((object)json);
                if (logger.IsDebugEnabled)
                    logger.Debug($"RailVehicleUpdateActivity: UI forward 완료, len={json?.Length ?? 0}");
            }
            catch (Exception ex)
            {
                logger.Warn($"RailVehicleUpdateActivity: UI forwarding 실패 - {ex.Message}");
            }
        }

        // currentNodeId == vehicle.AcsDestNodeId 이고, TC가 source phase(STATE_ASSIGNED)이며 CHARGEMOVE가 아닐 때
        // RAIL-VEHICLEDESTARRIVED 워크플로우를 dispatch 한다. dispatch 실패는 Update 본 흐름을 끊지 않는다.
        private static void DispatchDestArrivedIfNeeded(
            Bridge.AutofacContainerAccessor accessor, string currentNodeId, VehicleEx vehicle)
        {
            try
            {
                if (string.IsNullOrEmpty(vehicle?.AcsDestNodeId)) return;
                if (string.IsNullOrEmpty(vehicle.TransportCommandId)) return;
                if (!currentNodeId.Equals(vehicle.AcsDestNodeId, StringComparison.OrdinalIgnoreCase)) return;

                var transferManager = accessor.Resolve<ITransferManagerEx>();
                if (transferManager == null)
                {
                    logger.Warn("DispatchDestArrivedIfNeeded: ITransferManagerEx 해석 실패 — skip");
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommandByVehicleId(vehicle.VehicleId);
                if (tc == null) return;
                if (TransportCommandEx.JOBTYPE_CHARGEMOVE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase)) return;

                var workflowManager = accessor.Resolve<IWorkflowManager>();
                if (workflowManager == null)
                {
                    logger.Warn("DispatchDestArrivedIfNeeded: IWorkflowManager 해석 실패 — skip");
                    return;
                }

                logger.Info($"DispatchDestArrivedIfNeeded: source 도착 검출 → RAIL-VEHICLEDESTARRIVED dispatch. " +
                            $"vehicleId={vehicle.VehicleId}, tc={tc.JobId}, nodeId={currentNodeId}");
                workflowManager.Execute("RAIL-VEHICLEDESTARRIVED", (object)vehicle.VehicleId);
            }
            catch (Exception ex)
            {
                logger.Error("DispatchDestArrivedIfNeeded 오류", ex);
            }
        }
    }
}
