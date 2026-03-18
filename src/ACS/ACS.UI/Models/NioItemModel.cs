namespace ACS.UI.Models;

/// <summary>
/// NIO View DataGrid에 표시할 NIO 항목 모델
/// </summary>
public class NioItemModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string InterfaceClassName { get; set; } = "";
    public string WorkflowManagerName { get; set; } = "";
    public string ApplicationName { get; set; } = "";
    public int Port { get; set; }
    public string RemoteIp { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string State { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? CreateTime { get; set; }
}
