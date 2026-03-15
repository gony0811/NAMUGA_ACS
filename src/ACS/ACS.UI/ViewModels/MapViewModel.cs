using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private List<NodeDto> _nodes = new();
    private List<LinkDto> _links = new();
    private List<VehicleDto> _vehicles = new();

    public IReadOnlyList<NodeDto> Nodes => _nodes;
    public IReadOnlyList<LinkDto> Links => _links;
    public IReadOnlyList<VehicleDto> Vehicles => _vehicles;

    public event Action DataChanged;

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
}
