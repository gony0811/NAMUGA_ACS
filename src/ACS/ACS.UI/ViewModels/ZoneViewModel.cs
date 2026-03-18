using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
/// Zone View ViewModel — Zone 목록 CRUD
/// </summary>
public partial class ZoneViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<ZoneDto> Zones { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditZoneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteZoneCommand))]
    private ZoneDto? _selectedZone;

    [ObservableProperty]
    private int _totalCount;

    public ZoneViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadZonesAsync()
    {
        if (_apiService == null) return;

        try
        {
            var zones = await _apiService.GetZonesAsync();
            Zones.Clear();
            foreach (var zone in zones)
            {
                Zones.Add(zone);
            }
            TotalCount = Zones.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    [RelayCommand]
    private async Task AddZoneAsync()
    {
        if (_apiService == null) return;

        var emptyZone = new ZoneDto();
        var bays = await _apiService.GetBaysAsync();
        var dialog = new ZoneEditWindow(emptyZone, bays, isEditMode: false);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateZoneAsync(dialog.Zone);
            if (success)
                await LoadZonesAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedZone))]
    private async Task EditZoneAsync()
    {
        if (_apiService == null || SelectedZone == null) return;

        var bays = await _apiService.GetBaysAsync();
        var dialog = new ZoneEditWindow(SelectedZone, bays, isEditMode: true);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateZoneAsync(dialog.Zone);
            if (success)
                await LoadZonesAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedZone))]
    private async Task DeleteZoneAsync()
    {
        if (_apiService == null || SelectedZone == null) return;

        var msgBox = new Window
        {
            Title = "Delete Zone",
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(SelectedZone.Id)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteZoneAsync(SelectedZone.Id);
            if (success)
                await LoadZonesAsync();
        }
    }

    private bool HasSelectedZone() => SelectedZone != null;

    private static object CreateDeleteConfirmContent(string zoneId)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"Zone '{zoneId}' 을(를) 삭제하시겠습니까?",
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
        return Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Zone") ?? desktop.MainWindow!
            : null!;
    }
}
