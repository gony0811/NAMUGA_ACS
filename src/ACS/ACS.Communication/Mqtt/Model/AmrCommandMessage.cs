using System;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// ACS가 발행하는 명령 메시지 (amr/{id}/command 토픽)
    /// </summary>
    public class AmrCommandMessage
    {
        public string CommandType { get; set; }
        public string Parameter { get; set; }
        public string TransactionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
