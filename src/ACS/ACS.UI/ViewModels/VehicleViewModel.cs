using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

/// <summary>
/// Vehicle View ViewModel — NA_R_VEHICLE 테이블 기반 차량 목록 (read-only)
/// </summary>
public partial class VehicleViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<VehicleDto> Vehicles { get; } = new();

    [ObservableProperty]
    private int _totalCount;

    public VehicleViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadVehiclesAsync()
    {
        if (_apiService == null) return;

        try
        {
            var vehicles = await _apiService.GetVehiclesAsync();
            Vehicles.Clear();
            foreach (var v in vehicles)
            {
                Vehicles.Add(v);
            }
            TotalCount = Vehicles.Count;
        }
        catch
        {
            // 로드 실패 시 무시
        }
    }
}
