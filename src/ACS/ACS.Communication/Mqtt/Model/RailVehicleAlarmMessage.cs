using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEALARM JSON 메시지 모델.
    /// AMR error 발생 시 SET, 해소 시 RESET 으로 전이되며 Trans는 이를 받아
    /// Vehicle.AlarmState 를 ALARM/NOALARM 으로 갱신한다.
    /// </summary>
    public class RailVehicleAlarmMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleAlarmHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleAlarmData Data { get; set; }
    }

    public class RailVehicleAlarmHeader
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

    public class RailVehicleAlarmData
    {
        public const string TYPE_SET = "SET";
        public const string TYPE_RESET = "RESET";

        /// <summary>DB PK (VehicleEx.VehicleId)</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>MQTT vehicleId (CommId)</summary>
        [JsonPropertyName("commId")]
        public string CommId { get; set; }

        /// <summary>"SET" (알람 발생) 또는 "RESET" (알람 해소)</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>AMR error code (SET 시 nonzero, RESET 시 0)</summary>
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>AMR error message (SET 시 사유, RESET 시 비어있을 수 있음)</summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("eventTime")]
        public DateTime EventTime { get; set; }
    }
}