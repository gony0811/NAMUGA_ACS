using System;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// AMR이 발행하는 명령 응답 메시지 (amr/{id}/response 토픽)
    /// </summary>
    public class AmrResponseMessage
    {
        public string VehicleId { get; set; }
        public string TransactionId { get; set; }
        public string ResponseType { get; set; }
        public string Detail { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
