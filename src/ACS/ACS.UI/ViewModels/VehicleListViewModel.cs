using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

public partial class VehicleListViewModel : ObservableObject
{
    public ObservableCollection<VehicleDto> Vehicles { get; } = new();

    public void UpdateVehicles(List<VehicleDto> vehicles)
    {
        Vehicles.Clear();
        if (vehicles == null) return;
        foreach (var v in vehicles)
        {
            Vehicles.Add(v);
        }
    }
}
