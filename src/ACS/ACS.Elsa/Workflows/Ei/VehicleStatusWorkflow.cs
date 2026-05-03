using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows.Ei
{
    /// <summary>
    /// VEHICLE-STATUS 워크플로우.
    ///
    /// MQTT status 토픽으로 AMR 상태 메시지가 수신되면 MqttInterfaceManager에서 호출.
    /// 다음 3단계 Activity를 순차 실행한다:
    ///   1) ParseAmrStatus       — AmrStatusMessage 파싱 + Vehicle 조회 + 노드/AlarmState 계산
    ///   2) SendVehicleUpdate    — RAIL-VEHICLEUPDATE JSON을 Trans로 전송
    ///   3) SendVehicleAlarm     — AlarmState 전이가 있을 때만 RAIL-VEHICLEALARM(SET/RESET) 전송
    /// DB 업데이트는 Trans 측 워크플로우(RAIL-VEHICLEUPDATE / RAIL-VEHICLEALARM)에서 수행.
    /// </summary>
    public class VehicleStatusWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "VEHICLE-STATUS";
            builder.Name = "VEHICLE-STATUS";
            builder.Description = "AMR 상태 메시지 수신 시 Update/Alarm 메시지를 Trans에 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ParseAmrStatusActivity(),
                    new SendVehicleUpdateActivity(),
                    new SendVehicleAlarmActivity(),
                }
            };
        }
    }
}
