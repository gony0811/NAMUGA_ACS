using System.Collections.ObjectModel;

namespace ACS.UI.Models;

/// <summary>
/// Application Management 트리뷰에 표시할 프로세스/NIO 노드 모델
/// </summary>
public class ProcessNodeModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";  // CS, TRANS, EI, DAEMON, NIO 등
    public string State { get; set; } = ""; // ACTIVE, INACTIVE, HANG, STANDBY 등
    public ObservableCollection<ProcessNodeModel> Children { get; set; } = new();
    public Dictionary<string, string> Properties { get; set; } = new();
}
