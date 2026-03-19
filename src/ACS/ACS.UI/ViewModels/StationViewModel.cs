using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;
using ACS.UI.Views;

namespace ACS.UI.ViewModels;

/// <summary>
/// Station View ViewModel — 스테이션 목록 CRUD
/// </summary>
public partial class StationViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<StationDto> Stations { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditStationCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteStationCommand))]
    private StationDto? _selectedStation;

    [ObservableProperty]
    private int _totalCount;

    public StationViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadStationsAsync()
    {
        if (_apiService == null) return;

        try
        {
            var stations = await _apiService.GetStationsAsync();
            Stations.Clear();
            foreach (var station in stations)
            {
                Stations.Add(station);
            }
            TotalCount = Stations.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    [RelayCommand]
    private async Task AddStationAsync()
    {
        if (_apiService == null) return;

        var nodes = await _apiService.GetNodesAsync();
        var links = await _apiService.GetLinksAsync();

        var emptyStation = new StationDto { Type = "ACQUIRE" };
        var dialog = new StationEditWindow(emptyStation, nodes, links, isEditMode: false);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateStationAsync(dialog.Station);
            if (success)
                await LoadStationsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedStation))]
    private async Task EditStationAsync()
    {
        if (_apiService == null || SelectedStation == null) return;

        var nodes = await _apiService.GetNodesAsync();
        var links = await _apiService.GetLinksAsync();

        var dialog = new StationEditWindow(SelectedStation, nodes, links, isEditMode: true);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateStationAsync(dialog.Station);
            if (success)
                await LoadStationsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedStation))]
    private async Task DeleteStationAsync()
    {
        if (_apiService == null || SelectedStation == null) return;

        var msgBox = new Window
        {
            Title = "Delete Station",
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(SelectedStation.Id)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteStationAsync(SelectedStation.Id);
            if (success)
                await LoadStationsAsync();
        }
    }

    private bool HasSelectedStation() => SelectedStation != null;

    private static object CreateDeleteConfirmContent(string stationId)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"Station '{stationId}' 을(를) 삭제하시겠습니까?",
            FontSize = 12
        });

        var btnPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };

        var okBtn = new Button { Content = "OK", Width = 80, Height = 28 };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, Height = 28 };

        okBtn.Click += (s, e) =>
        {
            var w = (s as Visual)?.FindAncestorOfType<Window>();
            w?.Close(true);
        };
        cancelBtn.Click += (s, e) =>
        {
            var w = (s as Visual)?.FindAncestorOfType<Window>();
            w?.Close(false);
        };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        return panel;
    }

    private Window GetOwnerWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Station") ?? desktop.MainWindow!
            : null!;
    }
}
