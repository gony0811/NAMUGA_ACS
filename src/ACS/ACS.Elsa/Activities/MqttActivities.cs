using System;
using System.Collections;
using System.Collections.Generic;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Core.Base.Interface;
using ACS.Core.Logging;
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
                            $"robotState={status.RobotState}, errorCode={status.ErrorCode}, navigationStatus={status.NavigationStatus}");

                // PersistentDao로 직접 업데이트 (VehicleExs 타입, VehicleId 조건)
                // 1. RunState 업데이트: Running→RUN, Off/Paused→STOP
                string runState = MapRobotStateToRunState(status.RobotState);
                if (!string.IsNullOrEmpty(runState) && runState != vehicle.RunState)
                {
                    persistentDao.UpdateByAttribute(dbType, "RunState", runState, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle RunState 업데이트: {vehicle.RunState} → {runState}, vehicleId={dbVehicleId}");
                }

                // 2. AlarmState 업데이트: errorCode 0→NOALARM, >0→ALARM
                string alarmState = status.ErrorCode == 0
                    ? VehicleEx.ALARMSTATE_NOALARM
                    : VehicleEx.ALARMSTATE_ALARM;
                if (alarmState != vehicle.AlarmState)
                {
                    persistentDao.UpdateByAttribute(dbType, "AlarmState", alarmState, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle AlarmState 업데이트: {vehicle.AlarmState} → {alarmState}, vehicleId={dbVehicleId}");
                }

                // 3. BatteryRate 업데이트
                if (status.Battery != null)
                {
                    int batteryRate = (int)status.Battery.VoltagePercent;
                    if (batteryRate != vehicle.BatteryRate)
                    {
                        persistentDao.UpdateByAttribute(dbType, "BatteryRate", batteryRate, "VehicleId", dbVehicleId);
                        logger.Info($"Vehicle BatteryRate 업데이트: {vehicle.BatteryRate} → {batteryRate}, vehicleId={dbVehicleId}");
                    }
                }

                // 4. ProcessingState 업데이트
                string processingState = MapToProcessingState(status);
                if (!string.IsNullOrEmpty(processingState) && processingState != vehicle.ProcessingState)
                {
                    persistentDao.UpdateByAttribute(dbType, "ProcessingState", processingState, "VehicleId", dbVehicleId);
                    logger.Info($"Vehicle ProcessingState 업데이트: {vehicle.ProcessingState} → {processingState}, vehicleId={dbVehicleId}");
                }

                // 5. EventTime 업데이트
                persistentDao.UpdateByAttribute(dbType, "EventTime", DateTime.UtcNow, "VehicleId", dbVehicleId);

                // 6. Pose 로깅 (현재 VehicleEx에 좌표 필드 없음)
                if (status.Pose != null)
                {
                    logger.Info($"AMR Pose: x={status.Pose.X}, y={status.Pose.Y}, angle={status.Pose.Angle}, vehicleId={vehicleId}");
                }
            }
            catch (Exception e)
            {
                logger.Error("ProcessAmrStatusActivity 오류", e);
            }
        }

        /// <summary>
        /// RobotState → VehicleEx.RunState 매핑
        /// </summary>
        private static string MapRobotStateToRunState(string robotState)
        {
            return robotState switch
            {
                "Running" => VehicleEx.RUNSTATE_RUN,
                "Off" => VehicleEx.RUNSTATE_STOP,
                "Paused" => VehicleEx.RUNSTATE_STOP,
                _ => null
            };
        }

        /// <summary>
        /// AmrStatusMessage → VehicleEx.ProcessingState 매핑
        /// ChargingState가 Charging이면 CHARGE가 우선 적용
        /// </summary>
        private static string MapToProcessingState(AmrStatusMessage status)
        {
            // 충전 중이면 CHARGE 우선
            if (status.Battery?.ChargingState == "Charging")
            {
                return VehicleEx.PROCESSINGSTATE_CHARGE;
            }

            return status.NavigationStatus switch
            {
                "Moving" => VehicleEx.PROCESSINGSTATE_RUN,
                "WaitingForArrival" => VehicleEx.PROCESSINGSTATE_IDLE,
                _ => null
            };
        }
    }
}
