using System;

namespace ACS.UI.Models;

public class MqttConfigDto
{
    public int Seq { get; set; }
    public string Name { get; set; } = "";
    public string ApplicationName { get; set; } = "";
    public string WorkflowManagerName { get; set; } = "";
    public string BrokerIp { get; set; } = "";
    public int BrokerPort { get; set; } = 1883;
    public string TopicPrefix { get; set; } = "amr/";
    public string ClientId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public int KeepAliveSeconds { get; set; } = 30;
    public int ReconnectDelayMs { get; set; } = 5000;
    public string State { get; set; } = "LOADED";
    public string Description { get; set; } = "";
    public DateTime? CreateTime { get; set; }
    public DateTime? EditTime { get; set; }
    public string Creator { get; set; } = "";
    public string Editor { get; set; } = "";
}
