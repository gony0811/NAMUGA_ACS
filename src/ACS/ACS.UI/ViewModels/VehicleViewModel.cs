using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

/// <summary>
/// Vehicle View ViewModel — NA_R_VEHICLE 테이블 기반 차량 목록 + 차량 초기화
/// </summary>
public partial class VehicleViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<VehicleDto> Vehicles { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetVehicleCommand))]
    private VehicleDto? _selectedVehicle;

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

    /// <summary>
    /// 선택 차량 초기화 — ProcessingState=IDLE, TransferState=NOTASSIGNED, TransportCommandId 비움.
    /// 묶여 있던 TransportCommand(NA_T_TRANSPORTCMD)는 State=QUEUED, VehicleId 비움으로 환원되어 재할당 대기.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedVehicle))]
    private async Task ResetVehicleAsync()
    {
        if (_apiService == null || SelectedVehicle == null) return;

        var vehicleId = SelectedVehicle.VehicleId;
        if (string.IsNullOrEmpty(vehicleId)) return;

        var message =
            $"차량 '{vehicleId}' 을(를) 초기화하시겠습니까?\n" +
            "(ProcessingState → IDLE, TransferState → NOTASSIGNED, TransportCommandId 비움;\n" +
            " 묶인 TransportCommand는 QUEUED로 환원)";

        if (!await ConfirmAsync("Reset Vehicle", message)) return;

        await _apiService.ResetVehicleAsync(vehicleId);
        await LoadVehiclesAsync();
    }

    private bool HasSelectedVehicle() => SelectedVehicle != null;

    /// <summary>
    /// OK/Cancel 확인 다이얼로그 (true = OK)
    /// </summary>
    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var msgBox = new Window
        {
            Title = title,
            Width = 380,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateConfirmContent(message)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        return result == true;
    }

    private static object CreateConfirmContent(string message)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 12,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
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
        // VehicleView는 팝업 Window(Title="Vehicle")의 Content로 호스팅됨
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Vehicle" && w.IsVisible) ?? desktop.MainWindow!
            : null!;
    }
}
