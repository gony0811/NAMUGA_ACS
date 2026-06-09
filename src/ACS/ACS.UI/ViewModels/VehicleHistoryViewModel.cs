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
/// NA_T_VEHICLE_HISTORY 조회 ViewModel. LogViewModel 패턴 동일.
/// </summary>
public partial class VehicleHistoryViewModel : ObservableObject
{
    private readonly IAcsApiService _apiService;
    private DispatcherTimer? _autoRefreshTimer;

    public VehicleHistoryViewModel(IAcsApiService apiService)
    {
        _apiService = apiService;
        var today = DateTime.Today;
        _fromDate = today;
        _fromTime = TimeSpan.Zero;
        _toDate = today;
        _toTime = new TimeSpan(23, 59, 59);
    }

    /// <summary>State 콤보박스 항목 (VehicleEx.STATE_*).</summary>
    public string[] States { get; } =
        { "All", "IDLE", "BUSY", "ERROR", "RUN", "STOP", "PAUSE" };

    public ObservableCollection<VehicleHistoryRow> Histories { get; } = new();

    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private TimeSpan? _fromTime;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private TimeSpan? _toTime;
    [ObservableProperty] private string _vehicleId = "";
    [ObservableProperty] private string _bayId = "";
    [ObservableProperty] private string _selectedState = "All";
    [ObservableProperty] private string _transportCommandId = "";
    [ObservableProperty] private string _messageName = "";
    [ObservableProperty] private int _limit = 1000;

    [ObservableProperty] private bool _autoRefreshEnabled;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    [ObservableProperty] private VehicleHistoryRow? _selectedHistory;
    [ObservableProperty] private string _selectedHistoryDetail = "";

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var filter = new VehicleHistoryQueryFilter
            {
                FromLocal = Combine(FromDate, FromTime),
                ToLocal = Combine(ToDate, ToTime),
                VehicleId = VehicleId,
                BayId = BayId,
                State = SelectedState,
                TransportCommandId = TransportCommandId,
                MessageName = MessageName,
                Limit = Limit <= 0 ? 1000 : Limit
            };

            var result = await _apiService.GetVehicleHistoriesAsync(filter);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Histories.Clear();
                foreach (var dto in result)
                    Histories.Add(new VehicleHistoryRow(dto));
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
        VehicleId = "";
        BayId = "";
        SelectedState = "All";
        TransportCommandId = "";
        MessageName = "";
        Limit = 1000;
        Histories.Clear();
        SelectedHistory = null;
        SelectedHistoryDetail = "";
        StatusMessage = "";
    }

    partial void OnSelectedHistoryChanged(VehicleHistoryRow? value)
    {
        SelectedHistoryDetail = value == null ? "" : value.FormatDetail();
    }

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
/// DataGrid 표시용 행. Time(UTC)을 LocalTime으로 변환해 보관한다.
/// </summary>
public class VehicleHistoryRow
{
    public string Id { get; }
    public DateTime? LocalTime { get; }
    public string VehicleId { get; }
    public string BayId { get; }
    public string CarrierType { get; }
    public string ConnectionState { get; }
    public string AlarmState { get; }
    public string ProcessingState { get; }
    public string CurrentNodeId { get; }
    public string TransportCommandId { get; }
    public string Path { get; }
    public DateTime? NodeCheckTime { get; }
    public string State { get; }
    public string Installed { get; }
    public string TransferState { get; }
    public string RunState { get; }
    public string FullState { get; }
    public string MessageName { get; }
    public string AcsDestNodeId { get; }
    public string VehicleDestNodeId { get; }

    public VehicleHistoryRow(VehicleHistoryDto d)
    {
        Id = d.Id;
        LocalTime = d.Time?.ToLocalTime();
        VehicleId = d.VehicleId;
        BayId = d.BayId;
        CarrierType = d.CarrierType;
        ConnectionState = d.ConnectionState;
        AlarmState = d.AlarmState;
        ProcessingState = d.ProcessingState;
        CurrentNodeId = d.CurrentNodeId;
        TransportCommandId = d.TransportCommandId;
        Path = d.Path;
        NodeCheckTime = d.NodeCheckTime?.ToLocalTime();
        State = d.State;
        Installed = d.Installed;
        TransferState = d.TransferState;
        RunState = d.RunState;
        FullState = d.FullState;
        MessageName = d.MessageName;
        AcsDestNodeId = d.AcsDestNodeId;
        VehicleDestNodeId = d.VehicleDestNodeId;
    }

    public string FormatDetail()
    {
        const string TimeFmt = "yyyy-MM-dd HH:mm:ss.fff";
        var sb = new StringBuilder();
        sb.AppendLine($"Id                : {Id}");
        sb.AppendLine($"Time              : {LocalTime?.ToString(TimeFmt)}");
        sb.AppendLine($"VehicleId         : {VehicleId}");
        sb.AppendLine($"BayId             : {BayId}");
        sb.AppendLine($"CarrierType       : {CarrierType}");
        sb.AppendLine($"ConnectionState   : {ConnectionState}");
        sb.AppendLine($"AlarmState        : {AlarmState}");
        sb.AppendLine($"ProcessingState   : {ProcessingState}");
        sb.AppendLine($"CurrentNodeId     : {CurrentNodeId}");
        sb.AppendLine($"TransportCommandId: {TransportCommandId}");
        sb.AppendLine($"Path              : {Path}");
        sb.AppendLine($"NodeCheckTime     : {NodeCheckTime?.ToString(TimeFmt)}");
        sb.AppendLine($"State             : {State}");
        sb.AppendLine($"Installed         : {Installed}");
        sb.AppendLine($"TransferState     : {TransferState}");
        sb.AppendLine($"RunState          : {RunState}");
        sb.AppendLine($"FullState         : {FullState}");
        sb.AppendLine($"MessageName       : {MessageName}");
        sb.AppendLine($"AcsDestNodeId     : {AcsDestNodeId}");
        sb.AppendLine($"VehicleDestNodeId : {VehicleDestNodeId}");
        return sb.ToString();
    }
}
