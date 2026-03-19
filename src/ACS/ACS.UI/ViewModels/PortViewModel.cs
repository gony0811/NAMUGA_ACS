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

public partial class PortViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<LocationDto> Locations { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditLocationCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteLocationCommand))]
    private LocationDto? _selectedLocation;

    [ObservableProperty]
    private int _totalCount;

    public PortViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadLocationsAsync()
    {
        if (_apiService == null) return;

        try
        {
            var locations = await _apiService.GetLocationsAsync();
            Locations.Clear();
            foreach (var loc in locations)
                Locations.Add(loc);
            TotalCount = Locations.Count;
        }
        catch { }
    }

    [RelayCommand]
    private async Task AddLocationAsync()
    {
        if (_apiService == null) return;

        var stations = await _apiService.GetStationsAsync();
        var emptyLocation = new LocationDto { Type = "BUFFER", Direction = "LEFT" };
        var dialog = new LocationEditWindow(emptyLocation, stations, isEditMode: false);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateLocationAsync(dialog.Location);
            if (success)
                await LoadLocationsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLocation))]
    private async Task EditLocationAsync()
    {
        if (_apiService == null || SelectedLocation == null) return;

        var stations = await _apiService.GetStationsAsync();
        var dialog = new LocationEditWindow(SelectedLocation, stations, isEditMode: true);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateLocationAsync(dialog.Location);
            if (success)
                await LoadLocationsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLocation))]
    private async Task DeleteLocationAsync()
    {
        if (_apiService == null || SelectedLocation == null) return;

        var msgBox = new Window
        {
            Title = "Delete Location",
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(SelectedLocation.LocationId)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteLocationAsync(SelectedLocation.LocationId);
            if (success)
                await LoadLocationsAsync();
        }
    }

    private bool HasSelectedLocation() => SelectedLocation != null;

    private static object CreateDeleteConfirmContent(string locationId)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"Location '{locationId}' 을(를) 삭제하시겠습니까?",
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
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Port") ?? desktop.MainWindow!
            : null!;
    }
}
