using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

/// <summary>
/// 로그 조회 화면 ViewModel.
/// 시간 입력은 로컬(컴퓨터) 시간 기준이며, API 전송 시 UTC로 변환된다(AcsApiService).
/// 응답 Time(UTC)은 LogRow에서 ToLocalTime()으로 변환해 표시한다.
/// </summary>
public partial class LogViewModel : ObservableObject
{
    private readonly IAcsApiService _apiService;
    private DispatcherTimer? _autoRefreshTimer;

    public LogViewModel(IAcsApiService apiService)
    {
        _apiService = apiService;
        // 기본 조회 범위: 오늘 00:00 ~ 오늘 23:59:59 (로컬)
        var today = DateTime.Today;
        _fromDate = today;
        _fromTime = TimeSpan.Zero;
        _toDate = today;
        _toTime = new TimeSpan(23, 59, 59);
    }

    /// <summary>LogLevel 콤보박스 항목.</summary>
    public string[] LogLevels { get; } =
        { "All", "FATAL", "ERROR", "WARN", "INFO", "FINE", "DEBUG" };

    public ObservableCollection<LogRow> Logs { get; } = new();

    // ── 필터 ──────────────────────────────────────────────
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private TimeSpan? _fromTime;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private TimeSpan? _toTime;
    [ObservableProperty] private string _selectedLogLevel = "All";
    [ObservableProperty] private string _keyword = "";
    [ObservableProperty] private string _processName = "";
    [ObservableProperty] private string _messageName = "";
    [ObservableProperty] private string _transactionId = "";
    [ObservableProperty] private int _limit = 1000;

    [ObservableProperty] private bool _autoRefreshEnabled;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    [ObservableProperty] private LogRow? _selectedLog;
    [ObservableProperty] private string _selectedLogFullText = "";

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var filter = new LogQueryFilter
            {
                FromLocal = Combine(FromDate, FromTime),
                ToLocal = Combine(ToDate, ToTime),
                Level = SelectedLogLevel,
                Keyword = Keyword,
                ProcessName = ProcessName,
                MessageName = MessageName,
                TransactionId = TransactionId,
                Limit = Limit <= 0 ? 1000 : Limit
            };

            var result = await _apiService.GetLogsAsync(filter);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Logs.Clear();
                foreach (var dto in result)
                    Logs.Add(new LogRow(dto));
                StatusMessage = $"{Logs.Count}건 조회 — {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = "조회 실패: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        var today = DateTime.Today;
        FromDate = today;
        FromTime = TimeSpan.Zero;
        ToDate = today;
        ToTime = new TimeSpan(23, 59, 59);
        SelectedLogLevel = "All";
        Keyword = "";
        ProcessName = "";
        MessageName = "";
        TransactionId = "";
        Limit = 1000;
        Logs.Clear();
        SelectedLog = null;
        SelectedLogFullText = "";
        StatusMessage = "";
    }

    /// <summary>행 선택 시 전체 메시지(NA_L_LARGELOGMESSAGE 재조합)를 로드.</summary>
    partial void OnSelectedLogChanged(LogRow? value)
    {
        if (value == null)
        {
            SelectedLogFullText = "";
            return;
        }
        _ = LoadFullTextAsync(value.Id);
    }

    private async Task LoadFullTextAsync(string id)
    {
        try
        {
            var text = await _apiService.GetLogTextAsync(id);
            await Dispatcher.UIThread.InvokeAsync(() => SelectedLogFullText = text ?? "");
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => SelectedLogFullText = "전체 메시지 로드 실패: " + ex.Message);
        }
    }

    /// <summary>AutoRefresh 토글 — 켜면 5초 폴링 + 즉시 1회 조회, 끄면 중지.</summary>
    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value)
        {
            _autoRefreshTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoRefreshTimer.Tick -= OnAutoRefreshTick;
            _autoRefreshTimer.Tick += OnAutoRefreshTick;
            _autoRefreshTimer.Start();
            _ = SearchAsync();
        }
        else
        {
            _autoRefreshTimer?.Stop();
        }
    }

    private void OnAutoRefreshTick(object? sender, EventArgs e) => _ = SearchAsync();

    /// <summary>날짜 + 시각을 로컬 DateTime으로 합성(시각 미지정 시 00:00).</summary>
    private static DateTime? Combine(DateTime? date, TimeSpan? time)
    {
        if (!date.HasValue) return null;
        return date.Value.Date + (time ?? TimeSpan.Zero);
    }
}

/// <summary>
/// DataGrid 표시용 행. Time(UTC)을 LocalTime으로 변환해 보관한다.
/// </summary>
public class LogRow
{
    public string Id { get; }
    public DateTime? LocalTime { get; }
    public string LogLevel { get; }
    public string ProcessName { get; }
    public string MessageName { get; }
    public string CommunicationMessageName { get; }
    public string TransactionId { get; }
    public string TransportCommandId { get; }
    public string OperationName { get; }
    public string CarrierName { get; }
    public string MachineName { get; }
    public string UnitName { get; }
    public string Text { get; }
    public bool HasLargeText { get; }

    public LogRow(LogMessageDto d)
    {
        Id = d.Id;
        LocalTime = d.Time?.ToLocalTime();
        LogLevel = d.LogLevel;
        ProcessName = d.ProcessName;
        MessageName = d.MessageName;
        CommunicationMessageName = d.CommunicationMessageName;
        TransactionId = d.TransactionId;
        TransportCommandId = d.TransportCommandId;
        OperationName = d.OperationName;
        CarrierName = d.CarrierName;
        MachineName = d.MachineName;
        UnitName = d.UnitName;
        Text = d.Text;
        HasLargeText = d.HasLargeText;
    }
}
