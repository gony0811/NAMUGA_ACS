using System;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Message.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Communication.Mqtt.Model;
using ACS.Service;

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

                // 1. ConnectionState → CONNECT
                if (!"CONNECT".Equals(vehicle.ConnectionState))
                {
                    resourceManager.UpdateVehicleConnectionState(vehicle, data.ConnectionState);
                    logger.Info($"Vehicle ConnectionState → {data.ConnectionState}: vehicleId={data.VehicleId}");
                }

                // 2. RunState 업데이트
                if (!string.IsNullOrEmpty(data.RunState) && data.RunState != vehicle.RunState)
                {
                    resourceManager.UpdateVehicleRunState(vehicle, data.RunState);
                    logger.Info($"Vehicle RunState 업데이트: {vehicle.RunState} → {data.RunState}, vehicleId={data.VehicleId}");
                }

                // 3. FullState 업데이트
                if (!string.IsNullOrEmpty(data.FullState) && data.FullState != vehicle.FullState)
                {
                    resourceManager.UpdateVehicleFullState(vehicle, data.FullState);
                    logger.Info($"Vehicle FullState 업데이트: {vehicle.FullState} → {data.FullState}, vehicleId={data.VehicleId}");
                }

                // 4. AlarmState 업데이트
                if (!string.IsNullOrEmpty(data.AlarmState) && data.AlarmState != vehicle.AlarmState)
                {
                    resourceManager.UpdateVehicleAlarmState(vehicle, data.AlarmState);
                    logger.Info($"Vehicle AlarmState 업데이트: {vehicle.AlarmState} → {data.AlarmState}, vehicleId={data.VehicleId}");
                }

                // 5. BatteryRate 업데이트
                if (data.BatteryRate != vehicle.BatteryRate)
                {
                    resourceManager.UpdateVehicleBatteryRate(vehicle, data.BatteryRate);
                    logger.Info($"Vehicle BatteryRate 업데이트: {vehicle.BatteryRate} → {data.BatteryRate}, vehicleId={data.VehicleId}");
                }

                // 6. BatteryVoltage 업데이트
                if (Math.Abs(data.BatteryVoltage - vehicle.BatteryVoltage) > 0.01f)
                {
                    resourceManager.UpdateVehicleBatteryVoltage(vehicle, data.BatteryVoltage);
                    logger.Info($"Vehicle BatteryVoltage 업데이트: {vehicle.BatteryVoltage} → {data.BatteryVoltage}, vehicleId={data.VehicleId}");
                }

                // 7. VehicleDestNodeId 업데이트
                if (!string.IsNullOrEmpty(data.VehicleDestNodeId) && data.VehicleDestNodeId != vehicle.VehicleDestNodeId)
                {
                    resourceManager.UpdateVehicleVehicleDestNodeId(vehicle, data.VehicleDestNodeId);
                    logger.Info($"Vehicle VehicleDestNodeId 업데이트: {vehicle.VehicleDestNodeId} → {data.VehicleDestNodeId}, vehicleId={data.VehicleId}");
                }

                // 7-1. ProcessingState 업데이트
                if (string.IsNullOrEmpty(data.VehicleDestNodeId))
                {
                    // 목적지가 없으면 IDLE 상태로 전환
                    resourceManager.UpdateVehicleProcessingState(data.VehicleId,
                        VehicleEx.PROCESSINGSTATE_IDLE, "RAIL-VEHICLEUPDATE");
                    logger.Info($"Vehicle ProcessingState → IDLE (VehicleDestNodeId 없음): vehicleId={data.VehicleId}");
                }
                else
                {
                    // 목적지가 있으면 ChangeVehicleProcessingState로 충전→IDLE 전환 로직 수행
                    var resourceService = accessor.Resolve<ResourceServiceEx>();
                    if (resourceService != null)
                    {
                        var vehicleMessage = new VehicleMessageEx();
                        vehicleMessage.VehicleId = data.VehicleId;
                        vehicleMessage.MessageName = "RAIL-VEHICLEUPDATE";
                        resourceService.ChangeVehicleProcessingState(vehicleMessage);
                    }
                }

                // 8. 노드 변경 시 CurrentNodeId 업데이트
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
                        logger.Info($"Vehicle 위치 업데이트: {previousNodeId} → {data.CurrentNodeId}, vehicleId={data.VehicleId}");
                    }
                }

                // 9. EventTime 업데이트
                resourceManager.UpdateVehicleEventTime(vehicle);

                logger.Info($"RailVehicleUpdateActivity 완료: vehicleId={data.VehicleId}");
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleUpdateActivity 오류", e);
            }
        }
    }
}
