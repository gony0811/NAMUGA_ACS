using System;

namespace ACS.Communication.Http.Models
{
    public class MqttConfigDto
    {
        public int Seq { get; set; }
        public string Name { get; set; }
        public string ApplicationName { get; set; }
        public string WorkflowManagerName { get; set; }
        public string BrokerIp { get; set; }
        public int BrokerPort { get; set; }
        public string TopicPrefix { get; set; }
        public string ClientId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int KeepAliveSeconds { get; set; }
        public int ReconnectDelayMs { get; set; }
        public string State { get; set; }
        public string Description { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? EditTime { get; set; }
        public string Creator { get; set; }
        public string Editor { get; set; }
    }
}
