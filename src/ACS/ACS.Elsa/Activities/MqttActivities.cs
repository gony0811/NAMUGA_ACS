using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
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
    /// AMR 상태 메시지를 수신하여 RAIL-VEHICLEUPDATE JSON 메시지를 생성하고
    /// RabbitMQ를 통해 Trans 프로세스로 전송하는 Activity.
    ///
    /// 기존 ProcessAmrStatusActivity(DB 직접 업데이트)와
    /// ProcessAmrLocationChangeActivity(위치 변경 시 XML 전송)를 통합하여,
    /// 모든 상태+위치를 하나의 JSON 메시지로 Trans에 전달한다.
    /// DB 업데이트는 Trans 프로세스의 RailVehicleUpdateActivity에서 수행.
    /// </summary>
    [Activity("ACS.Mqtt", "Send AMR Vehicle Update",
        "AMR 상태+위치를 RAIL-VEHICLEUPDATE JSON으로 Trans에 전송")]
    public class SendAmrVehicleUpdateActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(SendAmrVehicleUpdateActivity));
        private static readonly NearestNodeFinder _nodeFinder = new NearestNodeFinder();

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 Arguments 추출: [AmrStatusMessage, vehicleId]
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 2)
                {
                    logger.Error("SendAmrVehicleUpdateActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }
                var status = args[0] as AmrStatusMessage;
                var vehicleId = args[1] as string;

                if (status == null || string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("SendAmrVehicleUpdateActivity: AmrStatusMessage 또는 vehicleId가 null입니다.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("SendAmrVehicleUpdateActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                // CommId로 Vehicle 조회 (MQTT vehicleId == VehicleEx.CommId)
                var persistentDao = accessor.Resolve<IPersistentDao>();
                VehicleEx vehicle = null;
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
                    logger.Warn($"SendAmrVehicleUpdateActivity: Vehicle을 찾을 수 없습니다. commId={vehicleId}, commType=MQTT");
                    return;
                }

                string dbVehicleId = vehicle.VehicleId;

                logger.Info($"SendAmrVehicleUpdateActivity: commId={vehicleId}, dbVehicleId={dbVehicleId}, " +
                            $"runState={status.State?.RunState}, workState={status.State?.WorkState}, " +
                            $"errorCode={status.Error?.Code}, fullState={status.State?.FullState}");

                // 상태값 매핑
                string runState = MapRunState(status.State?.RunState) ?? vehicle.RunState;
                string fullState = MapFullState(status.State?.FullState) ?? vehicle.FullState;
                int errorCode = status.Error?.Code ?? 0;
                string alarmState = errorCode == 0 ? VehicleEx.ALARMSTATE_NOALARM : VehicleEx.ALARMSTATE_ALARM;
                int batteryRate = status.Battery != null ? (int)status.Battery.LevelPercent : vehicle.BatteryRate;
                float batteryVoltage = status.Battery != null ? status.Battery.Voltage : vehicle.BatteryVoltage;
                string batteryChargingState = status.Battery != null ? status.Battery.ChargingState.ToUpper() : "DISCHARGING";
                string vehicleDestNodeId = !string.IsNullOrEmpty(status.State?.VehicleDestNode)
                    ? status.State.VehicleDestNode : "";

                // Pose → 최근접 노드 판별
                string currentNodeId = null;
                bool nodeChanged = false;

                if (status.Pose != null)
                {
                    logger.Info($"AMR Pose: x={status.Pose.X}, y={status.Pose.Y}, angle={status.Pose.Angle}, vehicleId={vehicleId}");

                    var cacheManager = accessor.Resolve<ICacheManagerEx>();
                    if (cacheManager != null)
                    {
                        var nodes = cacheManager.GetNodeACS();
                        if (nodes != null && nodes.Count > 0)
                        {
                            var configuration = accessor.Resolve<IConfiguration>();
                            double threshold = 2.0;
                            string thresholdStr = configuration?["Acs:Amr:NearestNodeThresholdMeters"];
                            if (!string.IsNullOrEmpty(thresholdStr) && double.TryParse(thresholdStr, out double configThreshold))
                            {
                                threshold = configThreshold;
                            }

                            var nearestNode = _nodeFinder.FindNearestNode(nodes, status.Pose.X, status.Pose.Y, threshold);
                            if (nearestNode != null &&
                                !string.Equals(nearestNode.NodeId, vehicle.CurrentNodeId, StringComparison.OrdinalIgnoreCase))
                            {
                                currentNodeId = nearestNode.NodeId;
                                nodeChanged = true;
                                logger.Info($"SendAmrVehicleUpdateActivity: 노드 변경 감지. " +
                                            $"vehicleId={dbVehicleId}, 이전={vehicle.CurrentNodeId}, 신규={currentNodeId}");
                            }
                        }
                    }
                }

                // Abnormal 로깅
                if (status.Abnormal != null && !string.IsNullOrEmpty(status.Abnormal.Type))
                {
                    logger.Warn($"AMR Abnormal: type={status.Abnormal.Type}, node={status.Abnormal.Node}, " +
                                $"timestamp={status.Abnormal.Timestamp}, vehicleId={vehicleId}");
                }

                // RAIL-VEHICLEUPDATE JSON 메시지 생성
                var updateMessage = new RailVehicleUpdateMessage
                {
                    Header = new RailVehicleUpdateHeader
                    {
                        MessageName = "RAIL-VEHICLEUPDATE",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "EI"
                    },
                    Data = new RailVehicleUpdateData
                    {
                        VehicleId = dbVehicleId,
                        CommId = vehicleId,
                        RunState = runState,
                        FullState = fullState,
                        AlarmState = alarmState,
                        BatteryRate = batteryRate,
                        BatteryVoltage = batteryVoltage,
                        BatteryChargingState = batteryChargingState,
                        VehicleDestNodeId = vehicleDestNodeId,
                        CurrentNodeId = currentNodeId,
                        NodeChanged = nodeChanged,
                        ConnectionState = "CONNECT",
                        EventTime = DateTime.UtcNow
                    }
                };

                string json = JsonSerializer.Serialize(updateMessage);

                // Trans로 JSON 전송
                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendAmrVehicleUpdateActivity: IMessageManagerEx를 찾을 수 없습니다.");
                    return;
                }

                messageManager.SendVehicleUpdateJson(json);

                logger.Info($"SendAmrVehicleUpdateActivity: RAIL-VEHICLEUPDATE 전송 완료. " +
                            $"vehicleId={dbVehicleId}, nodeChanged={nodeChanged}" +
                            (nodeChanged ? $", nodeId={currentNodeId}" : ""));
            }
            catch (Exception e)
            {
                logger.Error("SendAmrVehicleUpdateActivity 오류", e);
            }
        }

        private static string MapRunState(string runState)
        {
            return runState switch
            {
                "Run" => VehicleEx.RUNSTATE_RUN,
                "Stop" => VehicleEx.RUNSTATE_STOP,
                _ => null
            };
        }

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
    /// RAIL-CARRIERTRANSFER JSON을 수신하여 Vehicle의 MQTT 브로커를 통해 이동 명령 전송.
    /// vehicleId → NA_R_VEHICLE(CommType, CommId) → NA_C_MQTT → SendDestination(destNodeId)
    /// Arguments: [jsonMessage(string)]
    /// </summary>
    [Activity("ACS.Mqtt", "Handle Carrier Transfer",
        "RAIL-CARRIERTRANSFER 수신 시 MQTT로 Vehicle에 이동 명령 전송")]
    public class HandleCarrierTransferActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleCarrierTransferActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 JSON 메시지 추출
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleCarrierTransferActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleCarrierTransferActivity: JSON 메시지가 null입니다.");
                    return;
                }

                // JSON 파싱: vehicleId, destNodeId, port, jobType 추출
                string vehicleId = null;
                string destNodeId = null;
                string commandId = null;
                string port = null;
                string jobType = null;

                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("vehicleId", out var vid))
                            vehicleId = vid.GetString();
                        if (dataEl.TryGetProperty("destNodeId", out var nid))
                            destNodeId = nid.GetString();
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("port", out var portEl))
                            port = portEl.GetString();
                        if (dataEl.TryGetProperty("jobType", out var jtEl))
                            jobType = jtEl.GetString();
                    }
                }

                if (string.IsNullOrEmpty(vehicleId) || string.IsNullOrEmpty(destNodeId))
                {
                    logger.Error($"HandleCarrierTransferActivity: vehicleId 또는 destNodeId가 없습니다. vehicleId={vehicleId}, destNodeId={destNodeId}");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("HandleCarrierTransferActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                // Vehicle 조회 → CommType, CommId 확인
                var resourceManager = accessor.Resolve<ACS.Core.Resource.IResourceManagerEx>();
                var vehicle = resourceManager?.GetVehicle(vehicleId);
                if (vehicle == null)
                {
                    logger.Error($"HandleCarrierTransferActivity: Vehicle을 찾을 수 없습니다. vehicleId={vehicleId}");
                    return;
                }

                if (!"MQTT".Equals(vehicle.CommType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleCarrierTransferActivity: Vehicle CommType이 MQTT가 아닙니다. vehicleId={vehicleId}, commType={vehicle.CommType}");
                    return;
                }

                // MqttInterfaceManager를 통해 MQTT 이동 명령 전송
                var mqttManager = accessor.Resolve<MqttInterfaceManager>();
                if (mqttManager == null)
                {
                    logger.Error("HandleCarrierTransferActivity: MqttInterfaceManager를 찾을 수 없습니다.");
                    return;
                }

                // CommId로 Vehicle을 식별하여 MQTT command 토픽으로 이동 명령 전송
                var result = mqttManager.SendDestination(vehicle.CommId, destNodeId, port, jobType)
                    .GetAwaiter().GetResult();

                if (result)
                {
                    logger.Info($"HandleCarrierTransferActivity: MQTT 이동 명령 전송 완료. " +
                        $"commandId={commandId}, vehicleId={vehicleId}, commId={vehicle.CommId}, " +
                        $"destNodeId={destNodeId}, port={port}, jobType={jobType}");
                }
                else
                {
                    logger.Error($"HandleCarrierTransferActivity: MQTT 이동 명령 전송 실패. " +
                        $"vehicleId={vehicleId}, commId={vehicle.CommId}, destNodeId={destNodeId}");
                }
            }
            catch (Exception e)
            {
                logger.Error("HandleCarrierTransferActivity 오류", e);
            }
        }
    }
}
