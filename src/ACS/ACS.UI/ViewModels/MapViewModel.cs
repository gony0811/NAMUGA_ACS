using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private List<NodeDto> _nodes = new();
    private List<LinkDto> _links = new();
    private List<VehicleDto> _vehicles = new();
    private List<StationDto> _stations = new();
    private List<LocationDto> _locations = new();

    public IReadOnlyList<NodeDto> Nodes => _nodes;
    public IReadOnlyList<LinkDto> Links => _links;
    public IReadOnlyList<VehicleDto> Vehicles => _vehicles;
    public IReadOnlyList<StationDto> Stations => _stations;
    public IReadOnlyList<LocationDto> Locations => _locations;

    public event Action DataChanged;

    // ── Minimap 연동: 메인 맵 viewport(월드좌표 4모서리) ──
    // MapCanvas.Render에서 매번 publish, MinimapCanvas가 read.
    // 회전이 있을 경우 축 정렬이 아닌 4점 polygon이 된다.
    public (double X, double Y) ViewportP0 { get; private set; }
    public (double X, double Y) ViewportP1 { get; private set; }
    public (double X, double Y) ViewportP2 { get; private set; }
    public (double X, double Y) ViewportP3 { get; private set; }
    public bool HasViewport { get; private set; }
    public event Action? ViewportChanged;
    public event Action<double, double>? CenterOnWorldRequested;

    public void UpdateViewport(
        (double X, double Y) p0,
        (double X, double Y) p1,
        (double X, double Y) p2,
        (double X, double Y) p3)
    {
        ViewportP0 = p0; ViewportP1 = p1; ViewportP2 = p2; ViewportP3 = p3;
        HasViewport = true;
        ViewportChanged?.Invoke();
    }

    public void RequestCenterOnWorld(double worldX, double worldY)
        => CenterOnWorldRequested?.Invoke(worldX, worldY);

    // ── 표시 옵션 (Option 탭) ──
    [ObservableProperty] private bool _showLinks = true;
    partial void OnShowLinksChanged(bool value) => DataChanged?.Invoke();

    // ── Node 배치 모드 ──
    [ObservableProperty] private bool _isNodePlacementMode;
    public List<(double X, double Y)> PendingPlacementNodes { get; } = new();
    public event Action<List<(double X, double Y)>> NodePlacementCompleted;
    public event Action? NodePlacementCancelled;

    public void EnterNodePlacementMode()
    {
        PendingPlacementNodes.Clear();
        IsNodePlacementMode = true;
        DataChanged?.Invoke();
    }

    public void AddPendingNode(double x, double y)
    {
        PendingPlacementNodes.Add((x, y));
        DataChanged?.Invoke();
    }

    public void FinishNodePlacement()
    {
        IsNodePlacementMode = false;
        var result = new List<(double X, double Y)>(PendingPlacementNodes);
        PendingPlacementNodes.Clear();
        DataChanged?.Invoke();
        if (result.Count > 0)
            NodePlacementCompleted?.Invoke(result);
    }

    public void RemoveLastPendingNode()
    {
        if (PendingPlacementNodes.Count > 0)
        {
            PendingPlacementNodes.RemoveAt(PendingPlacementNodes.Count - 1);
            DataChanged?.Invoke();
        }
    }

    public void CancelNodePlacement()
    {
        IsNodePlacementMode = false;
        PendingPlacementNodes.Clear();
        DataChanged?.Invoke();
        NodePlacementCancelled?.Invoke();
    }

    // ── Node 드래그 이동 ──
    public event Action<string, double, double>? NodePositionChanged;

    public void UpdateNodePosition(string nodeId, double x, double y)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            node.Xpos = x;
            node.Ypos = y;
            DataChanged?.Invoke();
        }
    }

    public void CommitNodePosition(string nodeId, double x, double y)
    {
        NodePositionChanged?.Invoke(nodeId, x, y);
    }

    // ── Link 선택 모드 ──
    [ObservableProperty] private bool _isLinkSelectionMode;
    [ObservableProperty] private string? _selectedFromNodeId;
    [ObservableProperty] private string? _hoveredNodeId;
    public event Action<string, string>? LinkSelectionCompleted;
    public event Action? LinkSelectionCancelled;

    public void EnterLinkSelectionMode()
    {
        SelectedFromNodeId = null;
        HoveredNodeId = null;
        IsLinkSelectionMode = true;
        DataChanged?.Invoke();
    }

    public void SetHoveredNode(string? nodeId)
    {
        if (HoveredNodeId != nodeId)
        {
            HoveredNodeId = nodeId;
            DataChanged?.Invoke();
        }
    }

    public void SelectNode(string nodeId)
    {
        if (SelectedFromNodeId == null)
        {
            SelectedFromNodeId = nodeId;
            DataChanged?.Invoke();
        }
        else
        {
            // To Node 선택 완료
            string fromId = SelectedFromNodeId;
            IsLinkSelectionMode = false;
            SelectedFromNodeId = null;
            HoveredNodeId = null;
            DataChanged?.Invoke();
            LinkSelectionCompleted?.Invoke(fromId, nodeId);
        }
    }

    public void CancelLinkSelection()
    {
        IsLinkSelectionMode = false;
        SelectedFromNodeId = null;
        HoveredNodeId = null;
        DataChanged?.Invoke();
        LinkSelectionCancelled?.Invoke();
    }

    /// <summary>
    /// 맵에서 상호작용 모드가 활성화되어 있는지 확인
    /// </summary>
    public bool IsInteractionMode => IsNodePlacementMode || IsLinkSelectionMode;

    public void UpdateNodes(List<NodeDto> nodes)
    {
        _nodes = nodes ?? new List<NodeDto>();
        DataChanged?.Invoke();
    }

    public void UpdateLinks(List<LinkDto> links)
    {
        _links = links ?? new List<LinkDto>();
        DataChanged?.Invoke();
    }

    public void UpdateVehicles(List<VehicleDto> vehicles)
    {
        var incoming = vehicles ?? new List<VehicleDto>();

        // Refresh 후에도 SignalR로 받은 실시간 POSE가 사라지지 않도록 기존 차량의 PoseX/Y/Angle을 머지.
        if (_vehicles.Count > 0)
        {
            foreach (var nv in incoming)
            {
                string nvVid = nv.VehicleId?.Trim();
                string nvCid = nv.CommId?.Trim();
                bool hasVid = !string.IsNullOrEmpty(nvVid);
                bool hasCid = !string.IsNullOrEmpty(nvCid);
                if (!hasVid && !hasCid) continue;

                var prev = _vehicles.FirstOrDefault(v =>
                    (hasVid && string.Equals(v.VehicleId?.Trim(), nvVid, StringComparison.OrdinalIgnoreCase)) ||
                    (hasCid && string.Equals(v.CommId?.Trim(), nvCid, StringComparison.OrdinalIgnoreCase)));

                if (prev != null)
                {
                    nv.PoseX = prev.PoseX;
                    nv.PoseY = prev.PoseY;
                    nv.PoseAngle = prev.PoseAngle;
                }
            }
        }

        _vehicles = incoming;
        DataChanged?.Invoke();
    }

    public void UpdateStations(List<StationDto> stations)
    {
        _stations = stations ?? new List<StationDto>();
        DataChanged?.Invoke();
    }

    public void UpdateLocations(List<LocationDto> locations)
    {
        _locations = locations ?? new List<LocationDto>();
        DataChanged?.Invoke();
    }

    /// <summary>
    /// SignalR로 수신한 차량 실시간 텔레메트리(POSE + 상태)를 적용한다.
    /// 호출 측에서 UI 스레드 마샬링을 보장해야 한다(Dispatcher.UIThread.Post).
    /// VehicleId(DB PK) 또는 CommId(MQTT 식별자) 어느 쪽으로도 매칭되도록 OrdinalIgnoreCase 비교.
    /// 차량이 아직 목록에 없거나 두 키 모두 비어 있으면 무시.
    /// 상태 필드는 항상 머지하되, POSE는 수신된 경우(non-null)에만 갱신하여
    /// POSE 없는 상태 메시지가 기존 위치를 지우지 않도록 한다.
    /// </summary>
    private DateTime _lastNoMatchLogAt = DateTime.MinValue;
    private bool _loggedFirstMatch;
    private static readonly TimeSpan NoMatchLogInterval = TimeSpan.FromSeconds(5);

    public void ApplyVehicleUpdate(VehicleUpdateDto dto)
    {
        if (dto == null) return;
        string vid = dto.VehicleId?.Trim();
        string cid = dto.CommId?.Trim();
        bool hasVid = !string.IsNullOrEmpty(vid);
        bool hasCid = !string.IsNullOrEmpty(cid);
        if (!hasVid && !hasCid) return;

        var vehicle = _vehicles.FirstOrDefault(v =>
            (hasVid && string.Equals(v.VehicleId?.Trim(), vid, StringComparison.OrdinalIgnoreCase)) ||
            (hasCid && string.Equals(v.CommId?.Trim(), cid, StringComparison.OrdinalIgnoreCase)));

        if (vehicle == null)
        {
            // 1Hz × N대 텔레메트리에서 로그 폭주를 막기 위해 5초 간격으로 throttle.
            var now = DateTime.UtcNow;
            if (now - _lastNoMatchLogAt >= NoMatchLogInterval)
            {
                _lastNoMatchLogAt = now;
                var known = string.Join(", ", _vehicles.Select(v => $"(vid={v.VehicleId},cid={v.CommId})"));
                Console.WriteLine($"[ApplyVehicleUpdate] no-match vid='{dto.VehicleId}' cid='{dto.CommId}'; known=[{known}]");
            }
            return;
        }

        if (!_loggedFirstMatch)
        {
            _loggedFirstMatch = true;
            Console.WriteLine($"[ApplyVehicleUpdate] match-ok vid='{dto.VehicleId}' cid='{dto.CommId}' -> vehicle.VehicleId='{vehicle.VehicleId}' CommId='{vehicle.CommId}'");
        }

        // 상태 필드 머지. 문자열은 비어 있으면 기존 값을 덮어쓰지 않는다
        // (특히 CurrentNodeId는 노드 변경 시에만 채워지므로 빈 값으로 클리어되면 안 됨).
        if (!string.IsNullOrEmpty(dto.RunState)) vehicle.RunState = dto.RunState;
        if (!string.IsNullOrEmpty(dto.ConnectionState)) vehicle.ConnectionState = dto.ConnectionState;
        if (!string.IsNullOrEmpty(dto.CurrentNodeId)) vehicle.CurrentNodeId = dto.CurrentNodeId;
        if (!string.IsNullOrEmpty(dto.VehicleDestNodeId)) vehicle.VehicleDestNodeId = dto.VehicleDestNodeId;
        vehicle.BatteryRate = dto.BatteryRate;
        vehicle.BatteryVoltage = dto.BatteryVoltage;

        // Trans 권위 스냅샷 필드. 매 메시지마다 vehicle 권위값으로 오므로 빈 값도 "실제로 비어 있음"을 의미한다.
        // ProcessingState/State는 정상적으로 비지 않으므로 null일 때만 기존 값 유지하고, 그 외엔 그대로 반영한다.
        if (dto.ProcessingState != null) vehicle.ProcessingState = dto.ProcessingState;
        if (dto.State != null) vehicle.State = dto.State;
        if (dto.TransferState != null) vehicle.TransferState = dto.TransferState;
        // 작업 완료 시 ""로 클리어되는 필드 — 빈 값으로도 UI를 비워야 하므로 직접 대입한다.
        vehicle.AcsDestNodeId = dto.AcsDestNodeId;
        vehicle.TransportCommandId = dto.TransportCommandId;
        vehicle.Path = dto.Path;

        // POSE는 수신된 경우에만 갱신.
        if (dto.PoseX.HasValue) vehicle.PoseX = dto.PoseX;
        if (dto.PoseY.HasValue) vehicle.PoseY = dto.PoseY;
        if (dto.PoseAngle.HasValue) vehicle.PoseAngle = dto.PoseAngle;

        DataChanged?.Invoke();
    }
}
