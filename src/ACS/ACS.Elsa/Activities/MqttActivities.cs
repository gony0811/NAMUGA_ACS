using System;
using System.Collections;
using System.Collections.Generic;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Core.Base.Interface;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Message.Model;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Resource.Model;
using ACS.Communication.Mqtt;
using ACS.Communication.Mqtt.Model;
using Microsoft.Extensions.Configuration;

namespace ACS.Elsa.Activities
{
    /// <summary>
    /// MQTT 설정을 DB에서 로드하고 브로커 연결을 시작하는 Activity.
    /// MqttInterfaceManager.Load() → Start()를 수행한다.
    /// </summary>
    [Activity("ACS.Mqtt", "Load And Start MQTT",
        "MQTT 설정 로드 및 브로커 연결 시작")]
    public class LoadAndStartMqttActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(LoadAndStartMqttActivity));

        [Output(Description = "MQTT 시작 성공 여부")]
        public Output<bool> Result { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            bool success = false;

            try
            {
                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("AutofacContainerAccessor를 찾을 수 없습니다.");
                    context.Set(Result, false);
                    return;
                }

                var mqttInterfaceManager = accessor.Resolve<MqttInterfaceManager>();
                var configuration = accessor.Resolve<IConfiguration>();

                if (mqttInterfaceManager == null)
                {
                    logger.Warn("MqttInterfaceManager가 등록되지 않았습니다.");
                    context.Set(Result, false);
                    return;
                }

                string applicationName = configuration?["Acs:Process:Name"];
                if (string.IsNullOrEmpty(applicationName))
                {
                    logger.Error("Acs:Process:Name 설정을 찾을 수 없습니다.");
                    context.Set(Result, false);
                    return;
                }

                // MQTT 설정 로드
                mqttInterfaceManager.Load(applicationName);

                // 설정이 로드되었으면 연결 시작
                if (mqttInterfaceManager.MqttConfigData != null)
                {
                    success = mqttInterfaceManager.Start();
                    logger.Info($"MQTT 브로커 연결 시작: applicationName={applicationName}, result={success}");
                }
                else
                {
                    logger.Info($"MQTT 설정이 없어 연결을 시작하지 않습니다: applicationName={applicationName}");
                }
            }
            catch (Exception e)
            {
                logger.Error("MQTT 초기화 중 오류", e);
            }

            context.Set(Result, success);
        }
    }

    /// <summary>
    /// MQTT 브로커 연결을 종료하고 heartbeat 타이머를 정지하는 Activity.
    /// MqttInterfaceManager.Stop()을 수행한다.
    /// </summary>
    [Activity("ACS.Mqtt", "Stop MQTT",
        "MQTT 브로커 연결 종료 및 heartbeat 타이머 정지")]
    public class StopMqttActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(StopMqttActivity));

        [Output(Description = "MQTT 정지 성공 여부")]
        public Output<bool> Result { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            bool success = false;

            try
            {
                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("AutofacContainerAccessor를 찾을 수 없습니다.");
                    context.Set(Result, false);
                    return;
                }

                var mqttInterfaceManager = accessor.Resolve<MqttInterfaceManager>();
                if (mqttInterfaceManager == null)
                {
                    logger.Warn("MqttInterfaceManager가 등록되지 않았습니다.");
                    context.Set(Result, false);
                    return;
                }

                if (mqttInterfaceManager.MqttConfigData != null)
                {
                    success = mqttInterfaceManager.Stop();
                    logger.Info($"MQTT 브로커 연결 종료: result={success}");
                }
                else
                {
                    logger.Info("MQTT 설정이 로드되지 않아 정지할 대상이 없습니다.");
                    success = true;
                }
            }
            catch (Exception e)
            {
                logger.Error("MQTT 정지 중 오류", e);
            }

            context.Set(Result, success);
        }
    }

    /// <summary>
    /// AMR 상태 메시지를 수신하여 Vehicle 상태를 업데이트하는 Activity.
    /// MqttInterfaceManager에서 VEHICLE-MESSAGERECEIVED 워크플로우로 전달된
    /// AmrStatusMessage를 파싱하여 VehicleEx의 각 필드를 갱신한다.
    /// </summary>
    [Activity("ACS.Mqtt", "Process AMR Status",
        "AMR 상태 메시지 수신 후 Vehicle 상태 업데이트")]
    public class ProcessAmrStatusActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ProcessAmrStatusActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 Arguments 추출: [AmrStatusMessage, vehicleId]
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 2)
                {
                    logger.Error("ProcessAmrStatusActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var status = args[0] as AmrStatusMessage;
                var vehicleId = args[1] as string;

                if (status == null || string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("ProcessAmrStatusActivity: AmrStatusMessage 또는 vehicleId가 null입니다.");
                    return;
                }

                // Autofac에서 ResourceManager 해결
                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("ProcessAmrStatusActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                // CommId로 Vehicle 조회 (MQTT vehicleId == VehicleEx.CommId)
                // DB: VehicleExs 타입으로 매핑되어 있으므로 PersistentDao로 직접 조회
                var persistentDao = accessor.Resolve<IPersistentDao>();
                VehicleEx vehicle = null;
                // EF Core는 VehicleExs(VehicleEx 상속)로 매핑되어 있음
                var vehicleExsType = System.Type.GetType("ACS.Core.Resource.Model.VehicleExs, ACS.Core");

                if (persistentDao != null)
                {
                    var attrs = new Dictionary<string, object>
                    {
                        { "CommId", vehicleId },
                        { "CommType", "MQTT" }
                    };
                    IList results = persistentDao.FindByAttributes(vehicleExsType ?? typeof(VehicleEx), attrs);
                    if (results != null && results.Count > 0)
                    {
                        vehicle = (VehicleEx)results[0];
                    }
                }

                if (vehicle == null)
                {
                    logger.Warn($"ProcessAmrStatusActivity: Vehicle을 찾을 수 없습니다. commId={vehicleId}, commType=MQTT");
                    return;
                }

                // VehicleExs 타입 (EF Core 매핑 타입)으로 직접 DB 업데이트
                string dbVehicleId = vehicle.VehicleId;
                var dbType = vehicleExsType ?? typeof(VehicleEx);

                logger.Info($"ProcessAmrStatusActivity: commId={vehicleId}, dbVehicleId={dbVehicleId}, " +
                            $"runState={status.State?.RunState}, workState={status.State?.WorkState}, " +
                            $"errorCode={status.Error?.Code}, fullState={status.State?.FullState}");

                // PersistentDao로 직접 업데이트 (VehicleExs 타입, VehicleId 조건)
                // 1. RunState 업데이트: Run→RUN, Stop→STOP
                string runState = MapRunState(status.State?.RunState);
                if (!string.IsNullOrEmpty(runState) && runState != vehicle.RunState)
                {
                    persistentDao.UpdateByAttribute(dbType, "RunState", runState, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle RunState 업데이트: {vehicle.RunState} → {runState}, vehicleId={dbVehicleId}");
                }

                // 2. FullState 업데이트: Full→FULL, Empty→EMPTY
                string fullState = MapFullState(status.State?.FullState);
                if (!string.IsNullOrEmpty(fullState) && fullState != vehicle.FullState)
                {
                    persistentDao.UpdateByAttribute(dbType, "FullState", fullState, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle FullState 업데이트: {vehicle.FullState} → {fullState}, vehicleId={dbVehicleId}");
                }

                // 3. VehicleDestNodeId 업데이트
                string destNode = status.State?.VehicleDestNode;
                if (!string.IsNullOrEmpty(destNode) && destNode != vehicle.VehicleDestNodeId)
                {
                    persistentDao.UpdateByAttribute(dbType, "VehicleDestNodeId", destNode, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle VehicleDestNodeId 업데이트: {vehicle.VehicleDestNodeId} → {destNode}, vehicleId={dbVehicleId}");
                }

                // 4. AlarmState 업데이트: error.code 0→NOALARM, >0→ALARM
                int errorCode = status.Error?.Code ?? 0;
                string alarmState = errorCode == 0
                    ? VehicleEx.ALARMSTATE_NOALARM
                    : VehicleEx.ALARMSTATE_ALARM;
                if (alarmState != vehicle.AlarmState)
                {
                    persistentDao.UpdateByAttribute(dbType, "AlarmState", alarmState, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle AlarmState 업데이트: {vehicle.AlarmState} → {alarmState}, vehicleId={dbVehicleId}");
                }

                // 5. BatteryRate 업데이트 (battery.levelPercent → 잔량 %)
                if (status.Battery != null)
                {
                    int batteryRate = (int)status.Battery.LevelPercent;
                    if (batteryRate != vehicle.BatteryRate)
                    {
                        persistentDao.UpdateByAttribute(dbType, "BatteryRate", batteryRate, "VehicleId", dbVehicleId);
                        logger.Info($"Vehicle BatteryRate 업데이트: {vehicle.BatteryRate} → {batteryRate}, vehicleId={dbVehicleId}");
                    }

                    // 6. BatteryVoltage 업데이트 (battery.voltage → 전압 V)
                    float batteryVoltage = status.Battery.Voltage;
                    if (Math.Abs(batteryVoltage - vehicle.BatteryVoltage) > 0.01f)
                    {
                        persistentDao.UpdateByAttribute(dbType, "BatteryVoltage", batteryVoltage, "VehicleId", dbVehicleId);
                        logger.Info($"Vehicle BatteryVoltage 업데이트: {vehicle.BatteryVoltage} → {batteryVoltage}, vehicleId={dbVehicleId}");
                    }
                }

                // 7. EventTime 업데이트
                persistentDao.UpdateByAttribute(dbType, "EventTime", DateTime.UtcNow, "VehicleId", dbVehicleId);

                // 9. Pose 로깅 (현재 VehicleEx에 좌표 필드 없음)
                if (status.Pose != null)
                {
                    logger.Info($"AMR Pose: x={status.Pose.X}, y={status.Pose.Y}, angle={status.Pose.Angle}, vehicleId={vehicleId}");
                }

                // 10. Abnormal 로깅
                if (status.Abnormal != null && !string.IsNullOrEmpty(status.Abnormal.Type))
                {
                    logger.Warn($"AMR Abnormal: type={status.Abnormal.Type}, node={status.Abnormal.Node}, " +
                                $"timestamp={status.Abnormal.Timestamp}, vehicleId={vehicleId}");
                }
            }
            catch (Exception e)
            {
                logger.Error("ProcessAmrStatusActivity 오류", e);
            }
        }

        /// <summary>
        /// state.runState → VehicleEx.RunState 매핑
        /// </summary>
        private static string MapRunState(string runState)
        {
            return runState switch
            {
                "Run" => VehicleEx.RUNSTATE_RUN,
                "Stop" => VehicleEx.RUNSTATE_STOP,
                _ => null
            };
        }

        /// <summary>
        /// state.fullState → VehicleEx.FullState 매핑
        /// </summary>
        private static string MapFullState(string fullState)
        {
            return fullState switch
            {
                "Full" => VehicleEx.FULLSTATE_FULL,
                "Empty" => VehicleEx.FULLSTATE_EMPTY,
                _ => null
            };
        }

    }

    /// <summary>
    /// AMR 연결/연결 끊김 시 Vehicle ConnectionState를 업데이트하는 Activity.
    /// MqttInterfaceManager.CheckAmrHeartbeats()에서 CONNECTED/DISCONNECTED 워크플로우로 호출.
    /// Arguments: [vehicleId(CommId)]
    /// </summary>
    [Activity("ACS.Mqtt", "Update AMR Connection State",
        "AMR 연결 상태 변경 시 Vehicle ConnectionState 업데이트")]
    public class UpdateAmrConnectionStateActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(UpdateAmrConnectionStateActivity));

        /// <summary>설정할 ConnectionState 값 (CONNECT 또는 DISCONNECT)</summary>
        [Input(Description = "ConnectionState 값 (CONNECT / DISCONNECT)")]
        public Input<string> ConnectionState { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 Arguments 추출: [vehicleId(CommId)]
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("UpdateAmrConnectionStateActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var vehicleId = args[0] as string;
                if (string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("UpdateAmrConnectionStateActivity: vehicleId가 null입니다.");
                    return;
                }

                string connectionState = ConnectionState?.Get(context);
                if (string.IsNullOrEmpty(connectionState))
                {
                    logger.Error("UpdateAmrConnectionStateActivity: ConnectionState가 설정되지 않았습니다.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("UpdateAmrConnectionStateActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var persistentDao = accessor.Resolve<IPersistentDao>();
                var vehicleExsType = System.Type.GetType("ACS.Core.Resource.Model.VehicleExs, ACS.Core");
                var dbType = vehicleExsType ?? typeof(VehicleEx);

                // CommId + CommType="MQTT"로 Vehicle 조회
                VehicleEx vehicle = null;
                if (persistentDao != null)
                {
                    var attrs = new Dictionary<string, object>
                    {
                        { "CommId", vehicleId },
                        { "CommType", "MQTT" }
                    };
                    IList results = persistentDao.FindByAttributes(dbType, attrs);
                    if (results != null && results.Count > 0)
                    {
                        vehicle = (VehicleEx)results[0];
                    }
                }

                if (vehicle == null)
                {
                    logger.Warn($"UpdateAmrConnectionStateActivity: Vehicle을 찾을 수 없습니다. commId={vehicleId}, commType=MQTT");
                    return;
                }

                string dbVehicleId = vehicle.VehicleId;

                if (connectionState != vehicle.ConnectionState)
                {
                    persistentDao.UpdateByAttribute(dbType, "ConnectionState", connectionState, "VehicleId", dbVehicleId);
                    persistentDao.UpdateByAttribute(dbType, "EventTime", DateTime.UtcNow, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle ConnectionState 업데이트: {vehicle.ConnectionState} → {connectionState}, vehicleId={dbVehicleId}, commId={vehicleId}");
                }
            }
            catch (Exception e)
            {
                logger.Error("UpdateAmrConnectionStateActivity 오류", e);
            }
        }
    }

    /// <summary>
    /// AMR Pose에서 가장 가까운 Node를 찾고, 노드가 변경되었으면
    /// RAIL-VEHICLELOCATIONCHANGED XML 메시지를 RabbitMQ로 Trans 프로세스에 전송하는 Activity.
    ///
    /// 레거시 T_CODE(RFID 태그 통과 시 nodeId 직접 전달)를 대체하여,
    /// Pose(X, Y) 좌표 기반으로 가장 가까운 노드를 판별한다.
    /// 노드가 변경되지 않은 경우(동일 노드 반복 수신) 전송하지 않는다.
    /// </summary>
    [Activity("ACS.Mqtt", "Process AMR Location Change",
        "AMR Pose에서 가장 가까운 Node를 찾고 위치 변경 시 Trans에 전송")]
    public class ProcessAmrLocationChangeActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ProcessAmrLocationChangeActivity));
        private static readonly NearestNodeFinder _nodeFinder = new NearestNodeFinder();

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 Arguments 추출: [AmrStatusMessage, vehicleId]
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 2)
                {
                    return;
                }

                var status = args[0] as AmrStatusMessage;
                var vehicleId = args[1] as string;

                if (status?.Pose == null || string.IsNullOrEmpty(vehicleId))
                {
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("ProcessAmrLocationChangeActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                // 노드 목록 조회
                var cacheManager = accessor.Resolve<ICacheManagerEx>();
                if (cacheManager == null)
                {
                    logger.Error("ProcessAmrLocationChangeActivity: ICacheManagerEx를 찾을 수 없습니다.");
                    return;
                }

                var nodes = cacheManager.GetNodeACS();
                if (nodes == null || nodes.Count == 0)
                {
                    logger.Warn($"ProcessAmrLocationChangeActivity: 노드 목록이 비어 있습니다. nodes={nodes?.Count ?? -1}");
                    return;
                }

                logger.Debug($"ProcessAmrLocationChangeActivity: 노드 {nodes.Count}개 로드됨, pose=({status.Pose.X}, {status.Pose.Y})");

                // threshold 설정 조회
                var configuration = accessor.Resolve<IConfiguration>();
                double threshold = 2.0;
                string thresholdStr = configuration?["Acs:Amr:NearestNodeThresholdMeters"];
                if (!string.IsNullOrEmpty(thresholdStr) && double.TryParse(thresholdStr, out double configThreshold))
                {
                    threshold = configThreshold;
                }

                // 가장 가까운 노드 찾기
                var nearestNode = _nodeFinder.FindNearestNode(nodes, status.Pose.X, status.Pose.Y, threshold);
                if (nearestNode == null)
                {
                    logger.Debug($"ProcessAmrLocationChangeActivity: threshold({threshold}m) 내에 노드 없음. " +
                                 $"pose=({status.Pose.X}, {status.Pose.Y}), vehicleId={vehicleId}");
                    return;
                }

                // Vehicle 조회 (CommId + CommType=MQTT)
                var persistentDao = accessor.Resolve<IPersistentDao>();
                VehicleEx vehicle = null;
                var vehicleExsType = System.Type.GetType("ACS.Core.Resource.Model.VehicleExs, ACS.Core");
                var dbType = vehicleExsType ?? typeof(VehicleEx);

                if (persistentDao != null)
                {
                    var attrs = new Dictionary<string, object>
                    {
                        { "CommId", vehicleId },
                        { "CommType", "MQTT" }
                    };
                    IList results = persistentDao.FindByAttributes(dbType, attrs);
                    if (results != null && results.Count > 0)
                    {
                        vehicle = (VehicleEx)results[0];
                    }
                }

                if (vehicle == null)
                {
                    logger.Warn($"ProcessAmrLocationChangeActivity: Vehicle을 찾을 수 없습니다. commId={vehicleId}");
                    return;
                }

                // 노드 변경 확인: 동일 노드이면 스킵
                if (string.Equals(nearestNode.NodeId, vehicle.CurrentNodeId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                logger.Info($"ProcessAmrLocationChangeActivity: 노드 변경 감지. " +
                            $"vehicleId={vehicle.VehicleId}, commId={vehicleId}, " +
                            $"이전={vehicle.CurrentNodeId}, 신규={nearestNode.NodeId}, " +
                            $"pose=({status.Pose.X}, {status.Pose.Y}), " +
                            $"distance={Math.Sqrt(Math.Pow(nearestNode.Xpos - status.Pose.X, 2) + Math.Pow(nearestNode.Ypos - status.Pose.Y, 2)):F2}m");

                // VehicleMessageEx 생성 및 RAIL-VEHICLELOCATIONCHANGED 메시지 RabbitMQ 전송
                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("ProcessAmrLocationChangeActivity: IMessageManagerEx를 찾을 수 없습니다.");
                    return;
                }

                var vehicleMsg = messageManager.CreateVehicleMessage(vehicle);
                vehicleMsg.NodeId = nearestNode.NodeId;
                vehicleMsg.MessageName = "RAIL-VEHICLELOCATIONCHANGED";

                // SendVehicleMessageTCodeEnter: XML 생성(RAIL-VEHICLELOCATIONCHANGED) + RabbitMQ 전송
                messageManager.SendVehicleMessageTCodeEnter("RAIL-VEHICLELOCATIONCHANGED", vehicleMsg);

                logger.Info($"ProcessAmrLocationChangeActivity: RAIL-VEHICLELOCATIONCHANGED 전송 완료. " +
                            $"vehicleId={vehicle.VehicleId}, nodeId={nearestNode.NodeId}");
            }
            catch (Exception e)
            {
                logger.Error("ProcessAmrLocationChangeActivity 오류", e);
            }
        }
    }
}
