using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

/// <summary>
/// NA_T_TRANSPORTCMD_HISTORY 조회 ViewModel. LogViewModel 패턴 동일.
/// 시간 입력은 로컬(컴퓨터) 시간 기준이며, API 전송 시 UTC로 변환된다(AcsApiService).
/// 응답 Time(UTC)은 Row에서 ToLocalTime()으로 변환해 표시한다.
/// </summary>
public partial class TransportCmdHistoryViewModel : ObservableObject
{
    private readonly IAcsApiService _apiService;
    private DispatcherTimer? _autoRefreshTimer;

    public TransportCmdHistoryViewModel(IAcsApiService apiService)
    {
        _apiService = apiService;
        var today = DateTime.Today;
        _fromDate = today;
        _fromTime = TimeSpan.Zero;
        _toDate = today;
        _toTime = new TimeSpan(23, 59, 59);
    }

    /// <summary>State 콤보박스 항목.</summary>
    public string[] States { get; } =
        { "All", "QUEUED", "ASSIGNED", "STARTED", "LOADED", "UNLOADED", "COMPLETED", "CANCELED", "FAILED" };

    /// <summary>JobType 콤보박스 항목 (선택값 — 자유 입력 시 "All" 외 자유 텍스트).</summary>
    public string[] JobTypes { get; } =
        { "All", "LOAD", "UNLOAD", "TRANSFER", "MOVE", "RECOVER" };

    public ObservableCollection<TransportCmdHistoryRow> Histories { get; } = new();

    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private TimeSpan? _fromTime;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private TimeSpan? _toTime;
    [ObservableProperty] private string _selectedState = "All";
    [ObservableProperty] private string _selectedJobType = "All";
    [ObservableProperty] private string _jobId = "";
    [ObservableProperty] private string _vehicleId = "";
    [ObservableProperty] private string _carrierId = "";
    [ObservableProperty] private string _bayId = "";
    [ObservableProperty] private int _limit = 1000;

    [ObservableProperty] private bool _autoRefreshEnabled;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    [ObservableProperty] private TransportCmdHistoryRow? _selectedHistory;
    [ObservableProperty] private string _selectedHistoryDetail = "";

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var filter = new TransportCmdHistoryQueryFilter
            {
                FromLocal = Combine(FromDate, FromTime),
                ToLocal = Combine(ToDate, ToTime),
                JobId = JobId,
                VehicleId = VehicleId,
                CarrierId = CarrierId,
                State = SelectedState,
                JobType = SelectedJobType,
                BayId = BayId,
                Limit = Limit <= 0 ? 1000 : Limit
            };

            var result = await _apiService.GetTransportCmdHistoriesAsync(filter);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Histories.Clear();
                foreach (var dto in result)
                    Histories.Add(new TransportCmdHistoryRow(dto));
                StatusMessage = $"{Histories.Count}건 조회 — {DateTime.Now:HH:mm:ss}";
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
        SelectedState = "All";
        SelectedJobType = "All";
        JobId = "";
        VehicleId = "";
        CarrierId = "";
        BayId = "";
        Limit = 1000;
        Histories.Clear();
        SelectedHistory = null;
        SelectedHistoryDetail = "";
        StatusMessage = "";
    }

    /// <summary>행 선택 시 상세 패널에 전체 컬럼 dump 표시.</summary>
    partial void OnSelectedHistoryChanged(TransportCmdHistoryRow? value)
    {
        SelectedHistoryDetail = value == null ? "" : value.FormatDetail();
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

    private static DateTime? Combine(DateTime? date, TimeSpan? time)
    {
        if (!date.HasValue) return null;
        return date.Value.Date + (time ?? TimeSpan.Zero);
    }
}

/// <summary>
/// DataGrid 표시용 행. Time/CreateTime 등(UTC)을 LocalTime으로 변환해 보관한다.
/// </summary>
public class TransportCmdHistoryRow
{
    public string Id { get; }
    public DateTime? LocalTime { get; }
    public string JobId { get; }
    public int Priority { get; }
    public string State { get; }
    public string VehicleId { get; }
    public string VehicleEvent { get; }
    public string CarrierId { get; }
    public string Source { get; }
    public string Dest { get; }
    public string Path { get; }
    public string JobType { get; }
    public string BayId { get; }
    public string EqpId { get; }
    public string PortId { get; }
    public string AgvName { get; }
    public string MidLoc { get; }
    public string MidPortId { get; }
    public string OriginLoc { get; }
    public string Reason { get; }
    public string Code { get; }
    public string Description { get; }
    public string AdditionalInfo { get; }
    public DateTime? CreateTime { get; }
    public DateTime? QueuedTime { get; }
    public DateTime? AssignedTime { get; }
    public DateTime? StartedTime { get; }
    public DateTime? LoadArrivedTime { get; }
    public DateTime? LoadedTime { get; }
    public DateTime? UnloadArrivedTime { get; }
    public DateTime? UnloadedTime { get; }
    public DateTime? LoadingTime { get; }
    public DateTime? UnloadingTime { get; }
    public DateTime? CompletedTime { get; }

    public TransportCmdHistoryRow(TransportCommandHistoryDto d)
    {
        Id = d.Id;
        LocalTime = d.Time?.ToLocalTime();
        JobId = d.JobId;
        Priority = d.Priority;
        State = d.State;
        VehicleId = d.VehicleId;
        VehicleEvent = d.VehicleEvent;
        CarrierId = d.CarrierId;
        Source = d.Source;
        Dest = d.Dest;
        Path = d.Path;
        JobType = d.JobType;
        BayId = d.BayId;
        EqpId = d.EqpId;
        PortId = d.PortId;
        AgvName = d.AgvName;
        MidLoc = d.MidLoc;
        MidPortId = d.MidPortId;
        OriginLoc = d.OriginLoc;
        Reason = d.Reason;
        Code = d.Code;
        Description = d.Description;
        AdditionalInfo = d.AdditionalInfo;
        CreateTime = d.CreateTime?.ToLocalTime();
        QueuedTime = d.QueuedTime?.ToLocalTime();
        AssignedTime = d.AssignedTime?.ToLocalTime();
        StartedTime = d.StartedTime?.ToLocalTime();
        LoadArrivedTime = d.LoadArrivedTime?.ToLocalTime();
        LoadedTime = d.LoadedTime?.ToLocalTime();
        UnloadArrivedTime = d.UnloadArrivedTime?.ToLocalTime();
        UnloadedTime = d.UnloadedTime?.ToLocalTime();
        LoadingTime = d.LoadingTime?.ToLocalTime();
        UnloadingTime = d.UnloadingTime?.ToLocalTime();
        CompletedTime = d.CompletedTime?.ToLocalTime();
    }

    public string FormatDetail()
    {
        const string TimeFmt = "yyyy-MM-dd HH:mm:ss.fff";
        var sb = new StringBuilder();
        sb.AppendLine($"Id                : {Id}");
        sb.AppendLine($"Time              : {LocalTime?.ToString(TimeFmt)}");
        sb.AppendLine($"JobId             : {JobId}");
        sb.AppendLine($"Priority          : {Priority}");
        sb.AppendLine($"State             : {State}");
        sb.AppendLine($"VehicleId         : {VehicleId}");
        sb.AppendLine($"VehicleEvent      : {VehicleEvent}");
        sb.AppendLine($"CarrierId         : {CarrierId}");
        sb.AppendLine($"Source            : {Source}");
        sb.AppendLine($"Dest              : {Dest}");
        sb.AppendLine($"Path              : {Path}");
        sb.AppendLine($"JobType           : {JobType}");
        sb.AppendLine($"BayId             : {BayId}");
        sb.AppendLine($"EqpId             : {EqpId}");
        sb.AppendLine($"PortId            : {PortId}");
        sb.AppendLine($"AgvName           : {AgvName}");
        sb.AppendLine($"MidLoc            : {MidLoc}");
        sb.AppendLine($"MidPortId         : {MidPortId}");
        sb.AppendLine($"OriginLoc         : {OriginLoc}");
        sb.AppendLine($"Reason            : {Reason}");
        sb.AppendLine($"Code              : {Code}");
        sb.AppendLine($"Description       : {Description}");
        sb.AppendLine($"AdditionalInfo    : {AdditionalInfo}");
        sb.AppendLine($"CreateTime        : {CreateTime?.ToString(TimeFmt)}");
        sb.AppendLine($"QueuedTime        : {QueuedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"AssignedTime      : {AssignedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"StartedTime       : {StartedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"LoadArrivedTime   : {LoadArrivedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"LoadedTime        : {LoadedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"UnloadArrivedTime : {UnloadArrivedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"UnloadedTime      : {UnloadedTime?.ToString(TimeFmt)}");
        sb.AppendLine($"LoadingTime       : {LoadingTime?.ToString(TimeFmt)}");
        sb.AppendLine($"UnloadingTime     : {UnloadingTime?.ToString(TimeFmt)}");
        sb.AppendLine($"CompletedTime     : {CompletedTime?.ToString(TimeFmt)}");
        return sb.ToString();
    }
}
