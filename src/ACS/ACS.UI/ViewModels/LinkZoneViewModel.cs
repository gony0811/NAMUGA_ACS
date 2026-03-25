using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

public partial class LinkZoneViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<LinkZoneDto> LinkZones { get; } = new();

    [ObservableProperty]
    private int _totalCount;

    public LinkZoneViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadLinkZonesAsync()
    {
        if (_apiService == null) return;

        try
        {
            var linkZones = await _apiService.GetLinkZonesAsync();
            LinkZones.Clear();
            foreach (var lz in linkZones)
                LinkZones.Add(lz);
            TotalCount = LinkZones.Count;
        }
        catch { }
    }
}
