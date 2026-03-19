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

    // ── Node 배치 모드 ──
    [ObservableProperty] private bool _isNodePlacementMode;
    public List<(int X, int Y)> PendingPlacementNodes { get; } = new();
    public event Action<List<(int X, int Y)>> NodePlacementCompleted;
    public event Action? NodePlacementCancelled;

    public void EnterNodePlacementMode()
    {
        PendingPlacementNodes.Clear();
        IsNodePlacementMode = true;
        DataChanged?.Invoke();
    }

    public void AddPendingNode(int x, int y)
    {
        PendingPlacementNodes.Add((x, y));
        DataChanged?.Invoke();
    }

    public void FinishNodePlacement()
    {
        IsNodePlacementMode = false;
        var result = new List<(int X, int Y)>(PendingPlacementNodes);
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
    public event Action<string, int, int>? NodePositionChanged;

    public void UpdateNodePosition(string nodeId, int x, int y)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            node.Xpos = x;
            node.Ypos = y;
            DataChanged?.Invoke();
        }
    }

    public void CommitNodePosition(string nodeId, int x, int y)
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
        _vehicles = vehicles ?? new List<VehicleDto>();
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
}
