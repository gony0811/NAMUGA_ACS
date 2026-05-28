using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEUPDATE JSON 메시지 모델.
    /// AMR 상태(RunState, FullState, AlarmState, Battery 등)와 위치(CurrentNodeId)를
    /// 하나의 메시지로 통합하여 Trans에서 일괄 업데이트한다.
    /// </summary>
    public class RailVehicleUpdateMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleUpdateHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleUpdateData Data { get; set; }
    }

    public class RailVehicleUpdateHeader
    {
        [JsonPropertyName("messageName")]
        public string MessageName { get; set; }

        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("sender")]
        public string Sender { get; set; }
    }

    public class RailVehicleUpdateData
    {
        /// <summary>DB PK (VehicleEx.VehicleId)</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>MQTT vehicleId (CommId)</summary>
        [JsonPropertyName("commId")]
        public string CommId { get; set; }

        [JsonPropertyName("runState")]
        public string RunState { get; set; }

        [JsonPropertyName("fullState")]
        public string FullState { get; set; }

        /// <summary>Trans 권위 상태. forward 직전 vehicle.ProcessingState로 채워 UI 실시간 전달(EI 원본에는 없음).</summary>
        [JsonPropertyName("processingState")]
        public string ProcessingState { get; set; }

        /// <summary>Trans 권위 상태(ALIVE/BANNED 등). forward 직전 vehicle.State로 채움.</summary>
        [JsonPropertyName("state")]
        public string State { get; set; }

        /// <summary>Trans 권위 상태. forward 직전 vehicle.TransferState로 채움.</summary>
        [JsonPropertyName("transferState")]
        public string TransferState { get; set; }

        [JsonPropertyName("batteryRate")]
        public int BatteryRate { get; set; }

        [JsonPropertyName("batteryVoltage")]
        public float BatteryVoltage { get; set; }
        
        [JsonPropertyName("batteryChargingState")]
        public string BatteryChargingState { get; set; }

        [JsonPropertyName("vehicleDestNodeId")]
        public string VehicleDestNodeId { get; set; }

        /// <summary>ACS가 할당한 목적지. forward 직전 vehicle.AcsDestNodeId로 채움(작업 완료 시 ""로 클리어 가능).</summary>
        [JsonPropertyName("acsDestNodeId")]
        public string AcsDestNodeId { get; set; }

        /// <summary>현재 할당된 반송 명령 ID. forward 직전 vehicle.TransportCommandId로 채움(완료 시 ""로 클리어 가능).</summary>
        [JsonPropertyName("transportCommandId")]
        public string TransportCommandId { get; set; }

        /// <summary>경로 문자열. forward 직전 vehicle.Path로 채움(완료 시 ""로 클리어 가능).</summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }

        /// <summary>노드 변경 시에만 EI가 설정. Trans는 forward 직전 vehicle.CurrentNodeId(권위값)로 항상 덮어씀.</summary>
        [JsonPropertyName("currentNodeId")]
        public string CurrentNodeId { get; set; }

        /// <summary>노드 변경 여부 플래그</summary>
        [JsonPropertyName("nodeChanged")]
        public bool NodeChanged { get; set; }

        [JsonPropertyName("connectionState")]
        public string ConnectionState { get; set; }

        [JsonPropertyName("eventTime")]
        public DateTime EventTime { get; set; }

        /// <summary>AMR 원본 X 좌표 (meters). UI 실시간 위치 표시용. POSE 미수신 시 null.</summary>
        [JsonPropertyName("poseX")]
        public float? PoseX { get; set; }

        /// <summary>AMR 원본 Y 좌표 (meters). UI 실시간 위치 표시용. POSE 미수신 시 null.</summary>
        [JsonPropertyName("poseY")]
        public float? PoseY { get; set; }

        /// <summary>AMR 원본 각도 (radian). UI 실시간 회전 표시용. POSE 미수신 시 null.</summary>
        [JsonPropertyName("poseAngle")]
        public float? PoseAngle { get; set; }
    }
}
