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

        [JsonPropertyName("alarmState")]
        public string AlarmState { get; set; }

        [JsonPropertyName("batteryRate")]
        public int BatteryRate { get; set; }

        [JsonPropertyName("batteryVoltage")]
        public float BatteryVoltage { get; set; }
        
        [JsonPropertyName("batteryChargingState")]
        public string BatteryChargingState { get; set; }

        [JsonPropertyName("vehicleDestNodeId")]
        public string VehicleDestNodeId { get; set; }

        /// <summary>노드 변경 시에만 값이 설정됨</summary>
        [JsonPropertyName("currentNodeId")]
        public string CurrentNodeId { get; set; }

        /// <summary>노드 변경 여부 플래그</summary>
        [JsonPropertyName("nodeChanged")]
        public bool NodeChanged { get; set; }

        [JsonPropertyName("connectionState")]
        public string ConnectionState { get; set; }

        [JsonPropertyName("eventTime")]
        public DateTime EventTime { get; set; }
    }
}
