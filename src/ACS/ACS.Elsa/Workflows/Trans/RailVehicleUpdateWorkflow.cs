using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Communication.Msb;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Message.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Database.Model.Resource;

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

        // 차량별 throttle 상태 — 1Hz 텔레메트리에서 EventTime/BatteryVoltage 같은 고빈도 필드의 DB 반영 빈도를 줄임
        private static readonly ConcurrentDictionary<string, VehicleTickState> _tickState
            = new ConcurrentDictionary<string, VehicleTickState>();

        private class VehicleTickState
        {
            public DateTime LastEventTimeFlushUtc;
            public float    LastFlushedBatteryVoltage;
            public DateTime LastBatteryVoltageFlushUtc;
        }

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
                logger.Info($"RailVehicleUpdateActivity 시작: vehicleId={data.VehicleId}, commId={data.CommId}, nodeChanged={data.NodeChanged}");

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

                // 변경분을 모두 모아 한 번의 SaveChanges로 묶어 적용한다.
                var changes = new Dictionary<string, object>();
                bool meaningfulChange = false;
                var now = DateTime.UtcNow;
                var tick = _tickState.GetOrAdd(data.VehicleId, _ => new VehicleTickState());

                // 1. ConnectionState
                if (!"CONNECT".Equals(vehicle.ConnectionState))
                {
                    changes["ConnectionState"] = data.ConnectionState;
                    meaningfulChange = true;
                    logger.Info($"Vehicle ConnectionState → {data.ConnectionState}: vehicleId={data.VehicleId}");
                }

                // 2. State → ALIVE (BANNED 보존, 이미 ALIVE면 스킵)
                if (!"BANNED".Equals(vehicle.State) && !Vehicle.STATE_ALIVE.Equals(vehicle.State))
                {
                    changes["State"] = Vehicle.STATE_ALIVE;
                    meaningfulChange = true;
                    logger.Info($"Vehicle State → ALIVE: vehicleId={data.VehicleId}");
                }

                // 3. RunState
                if (!string.IsNullOrEmpty(data.RunState) && data.RunState != vehicle.RunState)
                {
                    changes["RunState"] = data.RunState;
                    meaningfulChange = true;
                    logger.Info($"Vehicle RunState 업데이트: {vehicle.RunState} → {data.RunState}, vehicleId={data.VehicleId}");
                }

                // 4. FullState
                if (!string.IsNullOrEmpty(data.FullState) && data.FullState != vehicle.FullState)
                {
                    changes["FullState"] = data.FullState;
                    meaningfulChange = true;
                    logger.Info($"Vehicle FullState 업데이트: {vehicle.FullState} → {data.FullState}, vehicleId={data.VehicleId}");
                }

                // 5. AlarmState
                if (!string.IsNullOrEmpty(data.AlarmState) && data.AlarmState != vehicle.AlarmState)
                {
                    changes["AlarmState"] = data.AlarmState;
                    meaningfulChange = true;
                    logger.Info($"Vehicle AlarmState 업데이트: {vehicle.AlarmState} → {data.AlarmState}, vehicleId={data.VehicleId}");
                }

                // 6. BatteryRate (정수, 1% 단위 자연 throttle)
                if (data.BatteryRate != vehicle.BatteryRate)
                {
                    changes["BatteryRate"] = data.BatteryRate;
                    logger.Info($"Vehicle BatteryRate 업데이트: {vehicle.BatteryRate} → {data.BatteryRate}, vehicleId={data.VehicleId}");
                }

                // 7. BatteryVoltage — 0.1V 차이 + 5초 throttle
                bool voltageDiffOK = Math.Abs(data.BatteryVoltage - tick.LastFlushedBatteryVoltage) > 0.1f;
                bool voltageTimeOK = (now - tick.LastBatteryVoltageFlushUtc).TotalSeconds >= 5.0;
                if (voltageDiffOK && voltageTimeOK)
                {
                    changes["BatteryVoltage"] = data.BatteryVoltage;
                    tick.LastFlushedBatteryVoltage = data.BatteryVoltage;
                    tick.LastBatteryVoltageFlushUtc = now;
                    logger.Info($"Vehicle BatteryVoltage 업데이트: {vehicle.BatteryVoltage} → {data.BatteryVoltage}, vehicleId={data.VehicleId}");
                }

                // 8. VehicleDestNodeId
                if (data.VehicleDestNodeId != vehicle.VehicleDestNodeId)
                {
                    changes["VehicleDestNodeId"] = data.VehicleDestNodeId;
                    meaningfulChange = true;
                    logger.Info($"Vehicle VehicleDestNodeId 업데이트: {vehicle.VehicleDestNodeId} → {data.VehicleDestNodeId}, vehicleId={data.VehicleId}");
                }

                // 9. ProcessingState: 충전 완료(CHARGE → IDLE) 전이만 책임진다.
                //    RUN(Job 진행 중)은 절대 덮어쓰지 않아 Job 중복 할당을 방지하고,
                //    CHARGE 진입은 충전 스테이션 도착 시 ResourceServiceEx에서 처리한다.
                const int BATTERY_CHARGE_RELEASE_RATE = 30;
                if (vehicle.ProcessingState == VehicleEx.PROCESSINGSTATE_CHARGE
                    && data.BatteryRate >= BATTERY_CHARGE_RELEASE_RATE)
                {
                    changes["ProcessingState"] = VehicleEx.PROCESSINGSTATE_IDLE;
                    meaningfulChange = true;
                    logger.Info($"Vehicle ProcessingState CHARGE → IDLE (BatteryRate={data.BatteryRate}% ≥ {BATTERY_CHARGE_RELEASE_RATE}%): vehicleId={data.VehicleId}");
                }

                // 10. CurrentNodeId — NodeChanged + 캐시 등록 + 실값 변경 시
                if (data.NodeChanged && !string.IsNullOrEmpty(data.CurrentNodeId)
                    && data.CurrentNodeId != vehicle.CurrentNodeId)
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
                        changes["CurrentNodeId"] = data.CurrentNodeId;
                        changes["NodeCheckTime"] = now;
                        meaningfulChange = true;
                        logger.Info($"Vehicle 위치 업데이트: {previousNodeId} → {data.CurrentNodeId}, vehicleId={data.VehicleId}");
                    }
                }

                // 11. EventTime — 의미 있는 변경 동반 OR 마지막 flush 후 10초 경과
                bool eventTimeForced = (now - tick.LastEventTimeFlushUtc).TotalSeconds >= 10.0;
                if (meaningfulChange || changes.Count > 0 || eventTimeForced)
                {
                    changes["EventTime"] = now;
                    tick.LastEventTimeFlushUtc = now;
                }

                // 12. 단일 SaveChanges 경로
                if (changes.Count > 0)
                {
                    resourceManager.UpdateVehicleBatch(vehicle, changes,
                        "RAIL-VEHICLEUPDATE", createHistory: meaningfulChange);
                }

                logger.Info($"RailVehicleUpdateActivity 완료: vehicleId={data.VehicleId}, changedFields={changes.Count}, history={meaningfulChange}");

                // 13. UI 프로세스로 원본 JSON 그대로 forward (POSE 포함, 1Hz 텔레메트리)
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
                logger.Debug($"RailVehicleUpdateActivity: UI forward 완료, len={json?.Length ?? 0}");
            }
            catch (Exception ex)
            {
                logger.Warn($"RailVehicleUpdateActivity: UI forwarding 실패 - {ex.Message}");
            }
        }
    }
}
