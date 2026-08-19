using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// EI → Trans 프로세스로 전송되는 RAIL-VEHICLEARRIVED JSON 메시지 모델.
    /// AMR reply(status=ARRIVED, 목적 노드 도착 명시 보고) 를 Trans 로 라우팅한다.
    /// Trans 는 이를 pose 기반 도착 판정(RailVehicleUpdate → RAIL-VEHICLEDESTARRIVED)과 같은
    /// 진입점(RAIL-VEHICLEDESTARRIVED)으로 수렴시키며, 중복 보고는 TC AdditionalInfo 의 ARRIVED 키로 방어한다.
    /// </summary>
    public class RailVehicleArrivedMessage
    {
        [JsonPropertyName("header")]
        public RailVehicleArrivedHeader Header { get; set; }

        [JsonPropertyName("data")]
        public RailVehicleArrivedData Data { get; set; }
    }

    public class RailVehicleArrivedHeader
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

    public class RailVehicleArrivedData
    {
        /// <summary>도착 보고가 속한 TransportCommand JobId (reply.jobId ?? reply.cmdId)</summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; }

        /// <summary>Vehicle ID (DB VehicleId)</summary>
        [JsonPropertyName("vehicleId")]
        public string VehicleId { get; set; }

        /// <summary>AMR 이 보고한 step (선택, EXCHANGE 20 등)</summary>
        [JsonPropertyName("step")]
        public int? Step { get; set; }
    }
}
