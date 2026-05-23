using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

/// <summary>
/// Application Management 화면 ViewModel.
/// NA_X_APPLICATION을 조회해 Primary/Secondary 하드웨어별 트리에 Type 그룹으로 표시하고,
/// 상태(active/inactive/hang)에 따라 정지/강제종료/실행을 수행한다.
/// </summary>
public partial class AppManagementViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;
    private DispatcherTimer? _autoRefreshTimer;

    [ObservableProperty]
    private ObservableCollection<ProcessNodeModel> _primaryProcesses = new();

    [ObservableProperty]
    private ObservableCollection<ProcessNodeModel> _secondaryProcesses = new();

    [ObservableProperty]
    private ProcessNodeModel? _selectedProcess;

    [ObservableProperty]
    private ObservableCollection<PropertyItem> _selectedProperties = new();

    [ObservableProperty]
    private bool _autoRefreshEnabled;

    [ObservableProperty]
    private string _statusMessage = "";

    public AppManagementViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
        _ = RefreshAsync();
    }

    partial void OnSelectedProcessChanged(ProcessNodeModel? value)
    {
        SelectedProperties.Clear();
        if (value?.Properties != null)
        {
            foreach (var kvp in value.Properties)
            {
                SelectedProperties.Add(new PropertyItem { Property = kvp.Key, Value = kvp.Value });
            }
        }
    }

    /// <summary>NA_X_APPLICATION을 조회해 트리를 갱신한다.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_apiService == null) return;

        try
        {
            var apps = await _apiService.GetApplicationsAsync();

            var primary = BuildTree(apps.Where(a => !IsSecondary(a.RunningHardware)));
            var secondary = BuildTree(apps.Where(a => IsSecondary(a.RunningHardware)));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PrimaryProcesses = primary;
                SecondaryProcesses = secondary;
                StatusMessage = $"{apps.Count} application(s) — {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (Exception ex)
        {
            // 백엔드 미기동/엔드포인트 없음(404) 등 — UI 크래시 방지, 상태만 표시.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = "조회 실패: " + ex.Message;
            });
        }
    }

    private static bool IsSecondary(string? hardware)
        => !string.IsNullOrEmpty(hardware) &&
           hardware.Contains("SECONDARY", StringComparison.OrdinalIgnoreCase);

    /// <summary>애플리케이션 목록을 Type별 그룹 노드로 묶어 트리를 구성한다.</summary>
    private static ObservableCollection<ProcessNodeModel> BuildTree(IEnumerable<ApplicationDto> apps)
    {
        var roots = new ObservableCollection<ProcessNodeModel>();

        foreach (var group in apps.GroupBy(a => a.Type ?? "").OrderBy(g => g.Key))
        {
            var groupNode = new ProcessNodeModel
            {
                Name = string.IsNullOrEmpty(group.Key) ? "(none)" : group.Key.ToUpperInvariant(),
                Type = group.Key,
                IsApplication = false
            };

            foreach (var app in group.OrderBy(a => a.Name))
            {
                groupNode.Children.Add(new ProcessNodeModel
                {
                    Name = app.Name,
                    Type = app.Type,
                    State = app.State,
                    IsApplication = true,
                    Properties = new Dictionary<string, string>
                    {
                        ["NAME"] = app.Name ?? "",
                        ["TYPE"] = app.Type ?? "",
                        ["STATE"] = app.State ?? "",
                        ["RUNNINGHARDWARE"] = app.RunningHardware ?? "",
                        ["STARTTIME"] = app.StartTime ?? "",
                        ["CHECKTIME"] = app.CheckTime ?? "",
                        ["DESCRIPTION"] = app.Description ?? ""
                    }
                });
            }

            roots.Add(groupNode);
        }

        return roots;
    }

    /// <summary>inactive 프로세스 실행</summary>
    [RelayCommand]
    private async Task StartAsync(ProcessNodeModel? node)
    {
        if (_apiService == null || node is not { IsApplication: true }) return;
        await _apiService.StartApplicationAsync(node.Name);
        await RefreshAsync();
    }

    /// <summary>active 프로세스 정지</summary>
    [RelayCommand]
    private async Task StopAsync(ProcessNodeModel? node)
    {
        if (_apiService == null || node is not { IsApplication: true }) return;
        await _apiService.StopApplicationAsync(node.Name);
        await RefreshAsync();
    }

    /// <summary>hang 프로세스 강제종료(덤프 후 종료)</summary>
    [RelayCommand]
    private async Task ForceKillAsync(ProcessNodeModel? node)
    {
        if (_apiService == null || node is not { IsApplication: true }) return;
        await _apiService.ForceKillApplicationAsync(node.Name);
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        AutoRefreshEnabled = !AutoRefreshEnabled;

        if (AutoRefreshEnabled)
        {
            _autoRefreshTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoRefreshTimer.Tick -= OnAutoRefreshTick;
            _autoRefreshTimer.Tick += OnAutoRefreshTick;
            _autoRefreshTimer.Start();
        }
        else
        {
            _autoRefreshTimer?.Stop();
        }
    }

    private void OnAutoRefreshTick(object? sender, EventArgs e) => _ = RefreshAsync();
}

/// <summary>
/// Properties DataGrid 바인딩용 아이템
/// </summary>
public class PropertyItem
{
    public string Property { get; set; } = "";
    public string Value { get; set; } = "";
}
