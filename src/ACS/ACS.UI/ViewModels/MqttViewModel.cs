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
/// MQTT View ViewModel — NA_C_MQTT 목록 CRUD
/// </summary>
public partial class MqttViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<MqttConfigDto> MqttConfigs { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditMqttConfigCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteMqttConfigCommand))]
    private MqttConfigDto? _selectedMqttConfig;

    [ObservableProperty]
    private int _totalCount;

    public MqttViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadMqttConfigsAsync()
    {
        if (_apiService == null) return;

        try
        {
            var configs = await _apiService.GetMqttConfigsAsync();
            MqttConfigs.Clear();
            foreach (var c in configs)
                MqttConfigs.Add(c);
            TotalCount = MqttConfigs.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    [RelayCommand]
    private async Task AddMqttConfigAsync()
    {
        if (_apiService == null) return;

        var dialog = new MqttEditWindow(new MqttConfigDto(), isEditMode: false);
        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateMqttConfigAsync(dialog.Config);
            if (success)
                await LoadMqttConfigsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMqttConfig))]
    private async Task EditMqttConfigAsync()
    {
        if (_apiService == null || SelectedMqttConfig == null) return;

        var dialog = new MqttEditWindow(SelectedMqttConfig, isEditMode: true);
        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateMqttConfigAsync(dialog.Config);
            if (success)
                await LoadMqttConfigsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMqttConfig))]
    private async Task DeleteMqttConfigAsync()
    {
        if (_apiService == null || SelectedMqttConfig == null) return;

        var seq = SelectedMqttConfig.Seq;
        var name = SelectedMqttConfig.Name;

        var msgBox = new Window
        {
            Title = "Delete MQTT Config",
            Width = 340,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(name, seq)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteMqttConfigAsync(seq);
            if (success)
                await LoadMqttConfigsAsync();
        }
    }

    private bool HasSelectedMqttConfig() => SelectedMqttConfig != null;

    private static object CreateDeleteConfirmContent(string name, int seq)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"MQTT '{name}' (seq={seq}) 을(를) 삭제하시겠습니까?",
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
            ? desktop.Windows.FirstOrDefault(w => w.Title == "MQTT" && w.IsVisible) ?? desktop.MainWindow!
            : null!;
    }
}
