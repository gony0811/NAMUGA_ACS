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
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
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
    /// VehicleStatusWorkflow 내에서 ParseAmrStatus → SendVehicleUpdate → SendVehicleAlarm
    /// 단계 사이를 잇는 컨텍스트 번들. 파싱 결과(매핑된 필드, 노드 변경, DB AlarmState 등)를 보관하여
    /// 후속 Activity가 재조회/재계산 없이 사용한다.
    /// </summary>
    public class VehicleUpdateContext
    {
        public const string PropertyKey = "VehicleUpdateContext";

        public string CommId;
        public string DbVehicleId;
        public string RunState;
        public string FullState;
        public int BatteryRate;
        public float BatteryVoltage;
        public string BatteryChargingState;
        public string VehicleDestNodeId;
        public string CurrentNodeId;
        public bool NodeChanged;
        public float? PoseX;
        public float? PoseY;
        public float? PoseAngle;

        // Alarm 전이 판정용
        public int ErrorCode;
        public string ErrorMessage;
        public string PreviousAlarmState;   // DB 조회 시점의 vehicle.AlarmState
        public string ComputedAlarmState;   // ErrorCode 기준 NOALARM/ALARM

        // Abnormal → RAIL-VEHICLEABNORMAL 메시지 적재용. AbnormalType 비어있으면 송신 생략.
        public string AbnormalType;
        public string AbnormalCode;
        public string AbnormalNode;
        public DateTime AbnormalTime;
    }

    /// <summary>
    /// AMR status 메시지를 파싱하고 Vehicle DB 조회, 위치 노드 매핑, AlarmState 계산까지 수행한 뒤
    /// VehicleUpdateContext를 WorkflowExecutionContext.Properties에 저장하는 Activity.
    /// 후속 SendVehicleUpdateActivity, SendVehicleAlarmActivity가 이 컨텍스트를 소비한다.
    /// </summary>
    [Activity("ACS.Mqtt", "Parse AMR Status",
        "AMR status 메시지 파싱 및 VehicleUpdateContext 생성")]
    public class ParseAmrStatusActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ParseAmrStatusActivity));
        private static readonly NearestNodeFinder _nodeFinder = new NearestNodeFinder();

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 2)
                {
                    logger.Error("ParseAmrStatusActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }
                var status = args[0] as AmrStatusMessage;
                var vehicleId = args[1] as string;

                if (status == null || string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("ParseAmrStatusActivity: AmrStatusMessage 또는 vehicleId가 null입니다.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("ParseAmrStatusActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
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
                    logger.Warn($"ParseAmrStatusActivity: Vehicle을 찾을 수 없습니다. commId={vehicleId}, commType=MQTT");
                    return;
                }

                string dbVehicleId = vehicle.VehicleId;

                logger.Info($"ParseAmrStatusActivity: commId={vehicleId}, dbVehicleId={dbVehicleId}, " +
                            $"runState={status.State?.RunState}, workState={status.State?.WorkState}, " +
                            $"errorCode={status.Error?.Code}, fullState={status.State?.FullState}");

                int errorCode = status.Error?.Code ?? 0;
                string previousAlarmState = string.IsNullOrEmpty(vehicle.AlarmState)
                    ? VehicleEx.ALARMSTATE_NOALARM : vehicle.AlarmState;
                string computedAlarmState = errorCode == 0 ? VehicleEx.ALARMSTATE_NOALARM : VehicleEx.ALARMSTATE_ALARM;

                var bundle = new VehicleUpdateContext
                {
                    CommId = vehicleId,
                    DbVehicleId = dbVehicleId,
                    RunState = MapRunState(status.State?.RunState) ?? vehicle.RunState,
                    FullState = MapFullState(status.State?.FullState) ?? vehicle.FullState,
                    BatteryRate = status.Battery != null ? (int)status.Battery.LevelPercent : vehicle.BatteryRate,
                    BatteryVoltage = status.Battery != null ? status.Battery.Voltage : vehicle.BatteryVoltage,
                    BatteryChargingState = status.Battery != null ? status.Battery.ChargingState.ToUpper() : "DISCHARGING",
                    VehicleDestNodeId = !string.IsNullOrEmpty(status.State?.VehicleDestNode) ? status.State.VehicleDestNode : "",
                    PoseX = status.Pose?.X,
                    PoseY = status.Pose?.Y,
                    PoseAngle = status.Pose?.Angle,
                    ErrorCode = errorCode,
                    ErrorMessage = status.Error?.Message ?? "",
                    PreviousAlarmState = previousAlarmState,
                    ComputedAlarmState = computedAlarmState
                };

                // Pose → 최근접 노드 판별
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
                                bundle.CurrentNodeId = nearestNode.NodeId;
                                bundle.NodeChanged = true;
                                logger.Info($"ParseAmrStatusActivity: 노드 변경 감지. " +
                                            $"vehicleId={dbVehicleId}, 이전={vehicle.CurrentNodeId}, 신규={bundle.CurrentNodeId}");
                            }
                        }
                    }
                }

                // Abnormal 로깅 + bundle 에 적재 → SendVehicleAbnormalActivity 가 RAIL-VEHICLEABNORMAL 메시지로 Trans 에 전송.
                // 실제 도메인 처리(TC 삭제, Vehicle 상태 초기화)는 Trans 측 RailVehicleAbnormalWorkflow 의 책임.
                if (status.Abnormal != null && !string.IsNullOrEmpty(status.Abnormal.Type))
                {
                    logger.Warn($"AMR Abnormal: type={status.Abnormal.Type}, node={status.Abnormal.Node}, " +
                                $"timestamp={status.Abnormal.Timestamp}, vehicleId={vehicleId}");

                    bundle.AbnormalType = status.Abnormal.Type;
                    bundle.AbnormalCode = MapAbnormalCode(status.Abnormal.Type);
                    bundle.AbnormalNode = status.Abnormal.Node ?? "";
                    bundle.AbnormalTime = status.Abnormal.Timestamp;
                }

                context.WorkflowExecutionContext.Properties[VehicleUpdateContext.PropertyKey] = bundle;
            }
            catch (Exception e)
            {
                logger.Error("ParseAmrStatusActivity 오류", e);
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

        // AMR 이 type 으로 이름("OPERATOR_ABORT") 만 보내거나 코드("200") 만 보내는 경우 모두 대응.
        // 알려진 매핑: OPERATOR_ABORT ↔ 200. 그 외 type 은 그대로 type 을 code 자리에도 둠(TS 에서 type 우선 분기).
        private static string MapAbnormalCode(string abnormalType)
        {
            if (string.IsNullOrEmpty(abnormalType))
                return "";
            if (string.Equals(abnormalType, "OPERATOR_ABORT", StringComparison.OrdinalIgnoreCase))
                return "200";
            return abnormalType;
        }
    }

    /// <summary>
    /// VehicleUpdateContext를 읽어 RAIL-VEHICLEUPDATE JSON 메시지를 생성하고 Trans 프로세스로 전송하는 Activity.
    /// AlarmState는 RAIL-VEHICLEALARM 으로 분리되어 더 이상 이 메시지에 포함되지 않는다.
    /// </summary>
    [Activity("ACS.Mqtt", "Send Vehicle Update",
        "VehicleUpdateContext 기반 RAIL-VEHICLEUPDATE JSON을 Trans에 전송")]
    public class SendVehicleUpdateActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(SendVehicleUpdateActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                if (!context.WorkflowExecutionContext.Properties.TryGetValue(VehicleUpdateContext.PropertyKey, out var raw)
                    || raw is not VehicleUpdateContext bundle)
                {
                    logger.Warn("SendVehicleUpdateActivity: VehicleUpdateContext가 없습니다 — Parse 단계 실패. 스킵.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("SendVehicleUpdateActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

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
                        VehicleId = bundle.DbVehicleId,
                        CommId = bundle.CommId,
                        RunState = bundle.RunState,
                        FullState = bundle.FullState,
                        BatteryRate = bundle.BatteryRate,
                        BatteryVoltage = bundle.BatteryVoltage,
                        BatteryChargingState = bundle.BatteryChargingState,
                        VehicleDestNodeId = bundle.VehicleDestNodeId,
                        CurrentNodeId = bundle.CurrentNodeId,
                        NodeChanged = bundle.NodeChanged,
                        ConnectionState = "CONNECT",
                        EventTime = DateTime.UtcNow,
                        PoseX = bundle.PoseX,
                        PoseY = bundle.PoseY,
                        PoseAngle = bundle.PoseAngle
                    }
                };

                string json = JsonSerializer.Serialize(updateMessage);

                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendVehicleUpdateActivity: IMessageManagerEx를 찾을 수 없습니다.");
                    return;
                }

                messageManager.SendVehicleUpdateJson(json);

                logger.Info($"SendVehicleUpdateActivity: RAIL-VEHICLEUPDATE 전송 완료. " +
                            $"vehicleId={bundle.DbVehicleId}, nodeChanged={bundle.NodeChanged}" +
                            (bundle.NodeChanged ? $", nodeId={bundle.CurrentNodeId}" : ""));
            }
            catch (Exception e)
            {
                logger.Error("SendVehicleUpdateActivity 오류", e);
            }
        }
    }

    /// <summary>
    /// VehicleUpdateContext의 PreviousAlarmState↔ComputedAlarmState 전이가 일어났을 때만
    /// RAIL-VEHICLEALARM JSON 메시지를 생성하여 Trans에 전송한다.
    /// - NOALARM → ALARM : type=SET (errorCode 동봉)
    /// - ALARM → NOALARM : type=RESET
    /// 동일 상태가 유지되는 동안에는 메시지 발행 없음.
    /// </summary>
    [Activity("ACS.Mqtt", "Send Vehicle Alarm",
        "AlarmState 전이 시 RAIL-VEHICLEALARM JSON을 Trans에 전송")]
    public class SendVehicleAlarmActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(SendVehicleAlarmActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                if (!context.WorkflowExecutionContext.Properties.TryGetValue(VehicleUpdateContext.PropertyKey, out var raw)
                    || raw is not VehicleUpdateContext bundle)
                {
                    logger.Warn("SendVehicleAlarmActivity: VehicleUpdateContext가 없습니다 — Parse 단계 실패. 스킵.");
                    return;
                }

                if (string.Equals(bundle.PreviousAlarmState, bundle.ComputedAlarmState, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string type;
                if (string.Equals(bundle.ComputedAlarmState, VehicleEx.ALARMSTATE_ALARM, StringComparison.OrdinalIgnoreCase))
                {
                    type = RailVehicleAlarmData.TYPE_SET;
                }
                else if (string.Equals(bundle.ComputedAlarmState, VehicleEx.ALARMSTATE_NOALARM, StringComparison.OrdinalIgnoreCase))
                {
                    type = RailVehicleAlarmData.TYPE_RESET;
                }
                else
                {
                    logger.Warn($"SendVehicleAlarmActivity: 미지원 ComputedAlarmState={bundle.ComputedAlarmState}. 스킵.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("SendVehicleAlarmActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var alarmMessage = new RailVehicleAlarmMessage
                {
                    Header = new RailVehicleAlarmHeader
                    {
                        MessageName = "RAIL-VEHICLEALARM",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "EI"
                    },
                    Data = new RailVehicleAlarmData
                    {
                        VehicleId = bundle.DbVehicleId,
                        CommId = bundle.CommId,
                        Type = type,
                        ErrorCode = bundle.ErrorCode,
                        ErrorMessage = bundle.ErrorMessage,
                        EventTime = DateTime.UtcNow
                    }
                };

                string json = JsonSerializer.Serialize(alarmMessage);

                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendVehicleAlarmActivity: IMessageManagerEx를 찾을 수 없습니다.");
                    return;
                }

                messageManager.SendVehicleAlarmJson(json);

                logger.Info($"SendVehicleAlarmActivity: RAIL-VEHICLEALARM 전송 완료. " +
                            $"vehicleId={bundle.DbVehicleId}, type={type}, errorCode={bundle.ErrorCode}, " +
                            $"transition={bundle.PreviousAlarmState}→{bundle.ComputedAlarmState}");
            }
            catch (Exception e)
            {
                logger.Error("SendVehicleAlarmActivity 오류", e);
            }
        }
    }

    /// <summary>
    /// VehicleUpdateContext 의 AbnormalType 이 채워져 있을 때 RAIL-VEHICLEABNORMAL JSON 메시지를
    /// 생성하여 Trans 에 전송한다. 실제 도메인 처리(TC 삭제, Vehicle 상태 초기화)는 Trans 측
    /// RailVehicleAbnormalWorkflow 가 type 별 분기로 수행한다 (현재 OPERATOR_ABORT 대응).
    /// </summary>
    [Activity("ACS.Mqtt", "Send Vehicle Abnormal",
        "AMR Abnormal 수신 시 RAIL-VEHICLEABNORMAL JSON을 Trans에 전송")]
    public class SendVehicleAbnormalActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(SendVehicleAbnormalActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                if (!context.WorkflowExecutionContext.Properties.TryGetValue(VehicleUpdateContext.PropertyKey, out var raw)
                    || raw is not VehicleUpdateContext bundle)
                {
                    logger.Warn("SendVehicleAbnormalActivity: VehicleUpdateContext가 없습니다 — Parse 단계 실패. 스킵.");
                    return;
                }

                if (string.IsNullOrEmpty(bundle.AbnormalType))
                {
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("SendVehicleAbnormalActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var abnormalMessage = new RailVehicleAbnormalMessage
                {
                    Header = new RailVehicleAbnormalHeader
                    {
                        MessageName = "RAIL-VEHICLEABNORMAL",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "EI"
                    },
                    Data = new RailVehicleAbnormalData
                    {
                        VehicleId = bundle.DbVehicleId,
                        CommId = bundle.CommId,
                        Type = bundle.AbnormalType,
                        Code = bundle.AbnormalCode ?? "",
                        Node = bundle.AbnormalNode ?? "",
                        AbnormalTime = bundle.AbnormalTime,
                        EventTime = DateTime.UtcNow
                    }
                };

                string json = JsonSerializer.Serialize(abnormalMessage);

                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("SendVehicleAbnormalActivity: IMessageManagerEx를 찾을 수 없습니다.");
                    return;
                }

                messageManager.SendVehicleAbnormalJson(json);

                logger.Info($"SendVehicleAbnormalActivity: RAIL-VEHICLEABNORMAL 전송 완료. " +
                            $"vehicleId={bundle.DbVehicleId}, type={bundle.AbnormalType}, code={bundle.AbnormalCode}, node={bundle.AbnormalNode}");
            }
            catch (Exception e)
            {
                logger.Error("SendVehicleAbnormalActivity 오류", e);
            }
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

                // JSON 파싱: vehicleId, destNodeId, port, jobType, portType 추출
                string vehicleId = null;
                string destNodeId = null;
                string commandId = null;
                string port = null;
                string jobType = null;
                string portType = null;

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
                        if (dataEl.TryGetProperty("portType", out var ptEl))
                            portType = ptEl.GetString();
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
                // cmdId=commandId(=TC.JobId) 로 발행해야 AMR reply 수신 시 TC 조회(JobType fallback)가 가능
                // amrSlot은 도메인 매핑이 없어 사양 default 1 사용
                var result = mqttManager.SendDestination(vehicle.CommId, destNodeId, port, jobType, commandId, portType)
                    .GetAwaiter().GetResult();

                if (result)
                {
                    logger.Info($"HandleCarrierTransferActivity: MQTT 이동 명령 전송 완료. " +
                        $"commandId={commandId}, vehicleId={vehicleId}, commId={vehicle.CommId}, " +
                        $"destNodeId={destNodeId}, port={port}, jobType={jobType}, portType={portType}");
                }
                else
                {
                    logger.Error($"HandleCarrierTransferActivity: MQTT 이동 명령 전송 실패. " +
                        $"vehicleId={vehicleId}, commId={vehicle.CommId}, destNodeId={destNodeId}, portType={portType}");
                }

                // RAIL-CARRIERTRANSFERREPLY를 Trans 프로세스로 회신
                SendCarrierTransferReply(accessor, commandId, vehicleId, result ? "OK" : "FAIL");
            }
            catch (Exception e)
            {
                logger.Error("HandleCarrierTransferActivity 오류", e);
            }
        }

        /// <summary>
        /// RAIL-CARRIERTRANSFERREPLY JSON을 Trans 프로세스로 RabbitMQ 전송.
        /// </summary>
        private void SendCarrierTransferReply(Bridge.AutofacContainerAccessor accessor, string commandId, string vehicleId, string resultCode)
        {
            try
            {
                var transAgent = accessor.ResolveNamed<ACS.Communication.Msb.IMessageAgent>("TransAgentSender");
                if (transAgent == null)
                {
                    logger.Error("HandleCarrierTransferActivity: TransAgentSender를 찾을 수 없습니다.");
                    return;
                }

                var replyMessage = new RailCarrierTransferReplyMessage
                {
                    Header = new RailCarrierTransferHeader
                    {
                        MessageName = "RAIL-CARRIERTRANSFERREPLY",
                        TransactionId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Sender = "EI"
                    },
                    Data = new RailCarrierTransferReplyData
                    {
                        CommandId = commandId,
                        ResultCode = resultCode
                    }
                };

                string replyJson = System.Text.Json.JsonSerializer.Serialize(replyMessage);
                transAgent.Send((object)replyJson);

                logger.Info($"HandleCarrierTransferActivity: RAIL-CARRIERTRANSFERREPLY 전송 완료. " +
                    $"commandId={commandId}, resultCode={resultCode}");
            }
            catch (Exception ex)
            {
                logger.Error($"HandleCarrierTransferActivity: Reply 전송 실패 - {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// RAIL-ACTIONCMD JSON 을 수신하여 Vehicle 의 MQTT 브로커로 actionCmd 발행.
    /// vehicleId → NA_R_VEHICLE(CommType, CommId) → MqttInterfaceManager.SendAction(nodeId, port, jobType, cmdId)
    /// MQTT 페이로드: { "command": "actionCmd", "nodeId": "...", "port": "...", "jobType": "..." }
    /// (docs/mqtt_interface.md §actionCmd 참조)
    /// </summary>
    [Activity("ACS.Mqtt", "Handle Action Cmd",
        "RAIL-ACTIONCMD 수신 시 MQTT 로 Vehicle 에 actionCmd 전송")]
    public class HandleActionCmdActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleActionCmdActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleActionCmdActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleActionCmdActivity: JSON 메시지가 null입니다.");
                    return;
                }

                string vehicleId = null;
                string nodeId = null;
                string commandId = null;
                string port = null;
                string jobType = null;
                string actionType = null;

                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("vehicleId", out var vid))
                            vehicleId = vid.GetString();
                        if (dataEl.TryGetProperty("nodeId", out var nid))
                            nodeId = nid.GetString();
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("port", out var portEl))
                            port = portEl.GetString();
                        if (dataEl.TryGetProperty("jobType", out var jtEl))
                            jobType = jtEl.GetString();
                        if (dataEl.TryGetProperty("actionType", out var atEl))
                            actionType = atEl.GetString();
                    }
                }

                if (string.IsNullOrEmpty(vehicleId) || string.IsNullOrEmpty(nodeId))
                {
                    logger.Error($"HandleActionCmdActivity: vehicleId 또는 nodeId가 없습니다. vehicleId={vehicleId}, nodeId={nodeId}");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("HandleActionCmdActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var resourceManager = accessor.Resolve<ACS.Core.Resource.IResourceManagerEx>();
                var vehicle = resourceManager?.GetVehicle(vehicleId);
                if (vehicle == null)
                {
                    logger.Error($"HandleActionCmdActivity: Vehicle을 찾을 수 없습니다. vehicleId={vehicleId}");
                    return;
                }

                if (!"MQTT".Equals(vehicle.CommType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleActionCmdActivity: Vehicle CommType이 MQTT가 아닙니다. vehicleId={vehicleId}, commType={vehicle.CommType}");
                    return;
                }

                var mqttManager = accessor.Resolve<MqttInterfaceManager>();
                if (mqttManager == null)
                {
                    logger.Error("HandleActionCmdActivity: MqttInterfaceManager를 찾을 수 없습니다.");
                    return;
                }

                // jobType 이 비어있으면 MES 가 보낸 actionType 으로 폴백
                string effectiveJobType = string.IsNullOrEmpty(jobType) ? (actionType ?? "") : jobType;

                var result = mqttManager.SendAction(vehicle.CommId, nodeId, port, effectiveJobType, commandId)
                    .GetAwaiter().GetResult();

                if (result)
                {
                    logger.Info($"HandleActionCmdActivity: MQTT actionCmd 전송 완료. " +
                        $"commandId={commandId}, vehicleId={vehicleId}, commId={vehicle.CommId}, " +
                        $"nodeId={nodeId}, port={port}, jobType={effectiveJobType}, actionType={actionType}");
                }
                else
                {
                    logger.Error($"HandleActionCmdActivity: MQTT actionCmd 전송 실패. " +
                        $"vehicleId={vehicleId}, commId={vehicle.CommId}, nodeId={nodeId}");
                }
            }
            catch (Exception e)
            {
                logger.Error("HandleActionCmdActivity 오류", e);
            }
        }
    }

    /// <summary>
    /// AMR reply(amr/{id}/reply) 메시지 수신 시 status=COMPLETED와 jobType에 따라
    /// Trans 프로세스로 RAIL-VEHICLEACQUIRECOMPLETED(UNLOAD) 또는
    /// RAIL-VEHICLEDEPOSITCOMPLETED(LOAD) JSON을 전송한다.
    ///
    /// Arguments: [AmrReplyMessage reply, string vehicleId]
    /// </summary>
    [Activity("ACS.Mqtt", "Handle AMR Reply",
        "AMR reply 수신 → COMPLETED일 때 jobType 분기로 Trans에 ACQUIRE/DEPOSIT 전송")]
    public class HandleAmrReplyActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleAmrReplyActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 2)
                {
                    logger.Error("HandleAmrReplyActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var reply = args[0] as AmrReplyMessage;
                var vehicleId = args[1] as string;

                if (reply == null || string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("HandleAmrReplyActivity: reply 또는 vehicleId가 null입니다.");
                    return;
                }

                // COMPLETED 만 Trans에 보고. ACCEPTED/EXECUTING/REJECTED/FAILED는 현재 라우팅 대상 아님.
                if (!"COMPLETED".Equals(reply.Status, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Debug($"HandleAmrReplyActivity: status={reply.Status}, 전송 생략. cmdId={reply.CmdId}");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("HandleAmrReplyActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                // AMR reply 스펙(docs/mqtt_interface.md)에는 jobType이 없으므로 reply.JobType이 비어있으면
                // cmdId(=TC JobId)로 TC를 조회해 TC.State(TransferringState) 기준으로 LOAD/UNLOAD 를 결정한다.
                // tc.JobType 은 상위 분류(AUTOCALL/ACSCALL/CHARGEMOVE 등)라서 LOAD/UNLOAD phase 구분에 부적합 — 사용하지 않음.
                string jobType = reply.JobType;
                if (string.IsNullOrEmpty(jobType))
                {
                    var transferManager = accessor.Resolve<ITransferManagerEx>();
                    var tc = transferManager?.GetTransportCommand(reply.CmdId);
                    if (tc == null)
                    {
                        logger.Warn($"HandleAmrReplyActivity: reply에 jobType이 없고 TC 조회 실패. 라우팅 불가. cmdId={reply.CmdId}, vehicleId={vehicleId}");
                        return;
                    }

                    if (tc.State == TransportCommandEx.STATE_ASSIGNED
                        || tc.State == TransportCommandEx.STATE_TRANSFERRING_SOURCE)
                    {
                        jobType = TransportCommandEx.JOBTYPE_UNLOAD;
                    }
                    else if (tc.State == TransportCommandEx.STATE_TRANSFERRING_DEST)
                    {
                        jobType = TransportCommandEx.JOBTYPE_LOAD;
                    }
                    else
                    {
                        logger.Warn($"HandleAmrReplyActivity: TC.State 매핑 실패. 라우팅 생략. cmdId={reply.CmdId}, vehicleId={vehicleId}, state={tc.State}");
                        return;
                    }
                    logger.Info($"HandleAmrReplyActivity: TC.State 로 jobType 보완. cmdId={reply.CmdId}, state={tc.State}, jobType={jobType}");
                }

                string dbVehicleId = ResolveDbVehicleId(accessor, vehicleId);

                string resultCode = reply.ResultCode == 0 ? "OK" : "FAIL";
                string errorCode = reply.ResultCode == 0 ? "" : reply.ResultCode.ToString();
                string errorMessage = reply.Message ?? "";

                string messageName;
                string json;

                if ("UNLOAD".Equals(jobType, StringComparison.OrdinalIgnoreCase))
                {
                    messageName = "RAIL-VEHICLEACQUIRECOMPLETED";
                    var msg = new RailVehicleAcquireCompletedMessage
                    {
                        Header = new RailVehicleAcquireCompletedHeader
                        {
                            MessageName = messageName,
                            TransactionId = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow,
                            Sender = "EI"
                        },
                        Data = new RailVehicleAcquireCompletedData
                        {
                            CommandId = reply.CmdId ?? "",
                            VehicleId = dbVehicleId,
                            ResultCode = resultCode,
                            ErrorCode = errorCode,
                            ErrorMessage = errorMessage
                        }
                    };
                    json = JsonSerializer.Serialize(msg);
                }
                else if ("LOAD".Equals(jobType, StringComparison.OrdinalIgnoreCase))
                {
                    messageName = "RAIL-VEHICLEDEPOSITCOMPLETED";
                    var msg = new RailVehicleDepositCompletedMessage
                    {
                        Header = new RailVehicleDepositCompletedHeader
                        {
                            MessageName = messageName,
                            TransactionId = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow,
                            Sender = "EI"
                        },
                        Data = new RailVehicleDepositCompletedData
                        {
                            CommandId = reply.CmdId ?? "",
                            VehicleId = dbVehicleId,
                            ResultCode = resultCode,
                            ErrorCode = errorCode,
                            ErrorMessage = errorMessage
                        }
                    };
                    json = JsonSerializer.Serialize(msg);
                }
                else
                {
                    logger.Info($"HandleAmrReplyActivity: jobType={jobType}은 라우팅 대상 아님. 전송 생략.");
                    return;
                }

                // Trans로 JSON 전송 (tsAgent = TransAgentSender → VM/DEMO/ES/LISTENER)
                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (messageManager == null)
                {
                    logger.Error("HandleAmrReplyActivity: IMessageManagerEx를 찾을 수 없습니다.");
                    return;
                }

                messageManager.SendVehicleUpdateJson(json);

                logger.Info($"HandleAmrReplyActivity: {messageName} 전송 완료. commandId={reply.CmdId}, vehicleId={dbVehicleId}, resultCode={resultCode}");
            }
            catch (Exception e)
            {
                logger.Error("HandleAmrReplyActivity 오류", e);
            }
        }

        /// <summary>
        /// MQTT 토픽의 vehicleId(=CommId) → DB VehicleEx.VehicleId로 매핑.
        /// 조회 실패 시 원본 vehicleId 그대로 반환.
        /// </summary>
        private static string ResolveDbVehicleId(Bridge.AutofacContainerAccessor accessor, string commId)
        {
            try
            {
                var persistentDao = accessor.Resolve<IPersistentDao>();
                if (persistentDao == null) return commId;

                var vehicleExsType = System.Type.GetType("ACS.Core.Resource.Model.VehicleExs, ACS.Core");
                var attrs = new Dictionary<string, object>
                {
                    { "CommId", commId },
                    { "CommType", "MQTT" }
                };
                IList results = persistentDao.FindByAttributes(vehicleExsType ?? typeof(VehicleEx), attrs);
                if (results != null && results.Count > 0 && results[0] is VehicleEx vehicle)
                {
                    return vehicle.VehicleId;
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"ResolveDbVehicleId: 조회 실패, commId={commId} - {ex.Message}");
            }
            return commId;
        }
    }
}
