using System.Collections.Generic;
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
/// Transfer Command View ViewModel — NA_T_TRANSPORTCMD 반송 명령 목록 조회 + 삭제/초기화
/// (추가/수정 없음, 읽기 전용 그리드)
/// </summary>
public partial class TransferCommandViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<TransportCommandDto> TransferCommands { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private TransportCommandDto? _selectedCommand;

    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// DataGrid 다중 선택된 명령 목록 (code-behind에서 동기화)
    /// </summary>
    public List<TransportCommandDto> SelectedCommands { get; set; } = new();

    public TransferCommandViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// API에서 반송 명령 목록 로드 (NA_T_TRANSPORTCMD)
    /// </summary>
    [RelayCommand]
    public async Task LoadTransferCommandsAsync()
    {
        if (_apiService == null) return;

        try
        {
            var commands = await _apiService.GetTransportCommandsAsync();
            TransferCommands.Clear();
            foreach (var cmd in commands)
            {
                TransferCommands.Add(cmd);
            }
            TotalCount = TransferCommands.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    /// <summary>
    /// 선택 행 삭제 (다중 선택 지원)
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task DeleteAsync()
    {
        if (_apiService == null || SelectedCommand == null) return;

        var targets = SelectedCommands.Count > 1
            ? SelectedCommands.ToList()
            : new List<TransportCommandDto> { SelectedCommand };

        string message = targets.Count == 1
            ? $"반송 명령 '{targets[0].JobId}' 을(를) 삭제하시겠습니까?"
            : $"선택된 {targets.Count}개의 반송 명령을 삭제하시겠습니까?";

        if (!await ConfirmAsync("Delete Transfer Command", message)) return;

        foreach (var cmd in targets)
        {
            if (string.IsNullOrEmpty(cmd.JobId)) continue;
            await _apiService.DeleteTransportCommandAsync(cmd.JobId);
        }
        await LoadTransferCommandsAsync();
    }

    /// <summary>
    /// 선택 행 초기화 — State=QUEUED, VehicleId 비우기 (다중 선택 지원)
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task ResetAsync()
    {
        if (_apiService == null || SelectedCommand == null) return;

        var targets = SelectedCommands.Count > 1
            ? SelectedCommands.ToList()
            : new List<TransportCommandDto> { SelectedCommand };

        string message = targets.Count == 1
            ? $"반송 명령 '{targets[0].JobId}' 을(를) 초기화하시겠습니까?\n(State → QUEUED, VehicleId 비움)"
            : $"선택된 {targets.Count}개의 반송 명령을 초기화하시겠습니까?\n(State → QUEUED, VehicleId 비움)";

        if (!await ConfirmAsync("Reset Transfer Command", message)) return;

        foreach (var cmd in targets)
        {
            if (string.IsNullOrEmpty(cmd.JobId)) continue;
            await _apiService.ResetTransportCommandAsync(cmd.JobId);
        }
        await LoadTransferCommandsAsync();
    }

    private bool HasSelected() => SelectedCommand != null;

    /// <summary>
    /// OK/Cancel 확인 다이얼로그 (true = OK)
    /// </summary>
    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var msgBox = new Window
        {
            Title = title,
            Width = 360,
            Height = 170,
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
        // TransferCommandView는 팝업 Window의 Content로 설정되므로, 해당 Window를 찾는다
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Transfer Command" && w.IsVisible) ?? desktop.MainWindow!
            : null!;
    }
}
