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
/// Bay View ViewModel — Bay 목록 CRUD
/// </summary>
public partial class BayViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<BayDto> Bays { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditBayCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteBayCommand))]
    private BayDto? _selectedBay;

    [ObservableProperty]
    private int _totalCount;

    public BayViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadBaysAsync()
    {
        if (_apiService == null) return;

        try
        {
            var bays = await _apiService.GetBaysAsync();
            Bays.Clear();
            foreach (var bay in bays)
            {
                Bays.Add(bay);
            }
            TotalCount = Bays.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    [RelayCommand]
    private async Task AddBayAsync()
    {
        if (_apiService == null) return;

        var emptyBay = new BayDto();
        var dialog = new BayEditWindow(emptyBay, isEditMode: false);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateBayAsync(dialog.Bay);
            if (success)
                await LoadBaysAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedBay))]
    private async Task EditBayAsync()
    {
        if (_apiService == null || SelectedBay == null) return;

        var dialog = new BayEditWindow(SelectedBay, isEditMode: true);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateBayAsync(dialog.Bay);
            if (success)
                await LoadBaysAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedBay))]
    private async Task DeleteBayAsync()
    {
        if (_apiService == null || SelectedBay == null) return;

        var msgBox = new Window
        {
            Title = "Delete Bay",
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(SelectedBay.Id)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteBayAsync(SelectedBay.Id);
            if (success)
                await LoadBaysAsync();
        }
    }

    private bool HasSelectedBay() => SelectedBay != null;

    private static object CreateDeleteConfirmContent(string bayId)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"Bay '{bayId}' 을(를) 삭제하시겠습니까?",
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
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Bay") ?? desktop.MainWindow!
            : null!;
    }
}
