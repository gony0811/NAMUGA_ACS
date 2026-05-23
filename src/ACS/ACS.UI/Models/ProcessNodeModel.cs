using System;
using System.Collections.ObjectModel;

namespace ACS.UI.Models;

/// <summary>
/// Application Management 트리뷰에 표시할 프로세스/그룹 노드 모델
/// </summary>
public class ProcessNodeModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";  // trans, ei, daemon, host, control 또는 그룹 라벨
    public string State { get; set; } = ""; // active, inactive, hang, stanby (앱 노드만)

    /// <summary>실제 애플리케이션 노드 여부 (false면 type 그룹 노드 → 제어 버튼 숨김)</summary>
    public bool IsApplication { get; set; }

    public ObservableCollection<ProcessNodeModel> Children { get; set; } = new();
    public Dictionary<string, string> Properties { get; set; } = new();

    private bool IsState(string state) => string.Equals(State, state, StringComparison.OrdinalIgnoreCase);

    /// <summary>inactive → 실행 버튼 활성</summary>
    public bool CanStart => IsApplication && IsState("inactive");

    /// <summary>active → 정지 버튼 활성</summary>
    public bool CanStop => IsApplication && IsState("active");

    /// <summary>hang → 강제종료 버튼 활성</summary>
    public bool CanForceKill => IsApplication && IsState("hang");
}
