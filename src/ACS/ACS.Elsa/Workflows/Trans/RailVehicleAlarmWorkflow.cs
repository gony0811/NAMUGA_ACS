using System;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Logging;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEALARM 워크플로우.
    ///
    /// EI 프로세스에서 AMR error 발생/해소 전이가 감지되면 SET/RESET 메시지를 보낸다.
    /// Trans 프로세스의 ESListener가 수신하여 이 워크플로우를 실행하고,
    /// Vehicle.AlarmState를 ALARM/NOALARM 으로 업데이트한다.
    /// </summary>
    public class RailVehicleAlarmWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEALARM";
            builder.Name = "RAIL-VEHICLEALARM";
            builder.Description = "AMR Alarm SET/RESET 메시지 수신 시 Vehicle.AlarmState 업데이트";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new RailVehicleAlarmActivity(),
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEALARM 처리 Activity.
    /// JSON을 역직렬화해 type=SET 이면 ALARMSTATE_ALARM, type=RESET 이면 ALARMSTATE_NOALARM 으로
    /// Vehicle.AlarmState를 업데이트한다. 이미 같은 상태이면 NO-OP.
    /// </summary>
    [Activity("ACS.Trans", "Rail Vehicle Alarm",
        "AMR Alarm SET/RESET JSON으로 Vehicle.AlarmState 업데이트")]
    public class RailVehicleAlarmActivity : CodeActivity
    {
        private const string MsgName = "RAIL-VEHICLEALARM";
        private static readonly Logger logger = Logger.GetLogger(typeof(RailVehicleAlarmActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("RailVehicleAlarmActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var json = args[0] as string;
                if (string.IsNullOrEmpty(json))
                {
                    logger.Error("RailVehicleAlarmActivity: JSON 메시지가 null입니다.");
                    return;
                }

                var alarmMessage = JsonSerializer.Deserialize<RailVehicleAlarmMessage>(json);
                if (alarmMessage?.Data == null)
                {
                    logger.Error("RailVehicleAlarmActivity: JSON 역직렬화 실패.");
                    return;
                }

                var data = alarmMessage.Data;
                logger.Info($"RailVehicleAlarmActivity 시작: vehicleId={data.VehicleId}, type={data.Type}, errorCode={data.ErrorCode}");

                string nextAlarmState;
                if (string.Equals(data.Type, RailVehicleAlarmData.TYPE_SET, StringComparison.OrdinalIgnoreCase))
                {
                    nextAlarmState = VehicleEx.ALARMSTATE_ALARM;
                }
                else if (string.Equals(data.Type, RailVehicleAlarmData.TYPE_RESET, StringComparison.OrdinalIgnoreCase))
                {
                    nextAlarmState = VehicleEx.ALARMSTATE_NOALARM;
                }
                else
                {
                    logger.Warn($"RailVehicleAlarmActivity: 미지원 type={data.Type}. 스킵.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("RailVehicleAlarmActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                if (resourceManager == null)
                {
                    logger.Error("RailVehicleAlarmActivity: IResourceManagerEx를 찾을 수 없습니다.");
                    return;
                }

                VehicleEx vehicle = resourceManager.GetVehicle(data.VehicleId);
                if (vehicle == null)
                {
                    logger.Warn($"RailVehicleAlarmActivity: Vehicle을 찾을 수 없습니다. vehicleId={data.VehicleId}");
                    return;
                }

                if (string.Equals(vehicle.AlarmState, nextAlarmState, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info($"RailVehicleAlarmActivity: AlarmState 이미 {nextAlarmState} — 변경 없음. vehicleId={data.VehicleId}");
                    return;
                }

                resourceManager.UpdateVehicleAlarmState(vehicle, nextAlarmState, MsgName);
                logger.Info($"RailVehicleAlarmActivity 완료: vehicleId={data.VehicleId}, AlarmState {vehicle.AlarmState} → {nextAlarmState} (type={data.Type}, errorCode={data.ErrorCode})");
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleAlarmActivity 오류", e);
            }
        }
    }
}