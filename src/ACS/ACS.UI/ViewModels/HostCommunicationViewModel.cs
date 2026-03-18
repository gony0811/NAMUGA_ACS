using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

/// <summary>
/// Host Communication (TCP) 뷰 ViewModel
/// </summary>
public partial class HostCommunicationViewModel : ObservableObject
{
    private const int MaxLogCount = 1000;
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<CommunicationLogItem> Logs { get; } = new();

    [ObservableProperty]
    private CommunicationLogItem? _selectedLog;

    // Listen Port 연결 상태 (MES → ACS 수신)
    [ObservableProperty]
    private bool _isListenConnected;

    [ObservableProperty]
    private string _listenState = "Disconnected";

    // Sender Port 연결 상태 (ACS → MES 송신)
    [ObservableProperty]
    private bool _isSenderConnected;

    [ObservableProperty]
    private string _senderState = "Disconnected";

    public HostCommunicationViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// Listen Port 연결 상태 업데이트
    /// </summary>
    public void UpdateListenState(bool connected)
    {
        IsListenConnected = connected;
        ListenState = connected ? "Connected" : "Disconnected";
    }

    /// <summary>
    /// Sender Port 연결 상태 업데이트
    /// </summary>
    public void UpdateSenderState(bool connected)
    {
        IsSenderConnected = connected;
        SenderState = connected ? "Connected" : "Disconnected";
    }

    /// <summary>
    /// Job Report 전송 (Receive / Start / Arrived / Completed)
    /// </summary>
    [RelayCommand]
    private async Task SendJobReportAsync(string reportType)
    {
        AddLog("Send", $"Job Report: {reportType}");

        if (_apiService != null)
        {
            try
            {
                var success = await _apiService.SendJobReportAsync(reportType);
                AddLog(success ? "Receive" : "Error",
                       success ? $"Job Report [{reportType}] ACK" : $"Job Report [{reportType}] Failed");
            }
            catch (Exception ex)
            {
                AddLog("Error", $"Job Report [{reportType}] Exception: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 통신 로그 추가 (최신이 최상위, 최대 1000개)
    /// </summary>
    public void AddLog(string direction, string message, string? remoteEndPoint = null)
    {
        var item = new CommunicationLogItem
        {
            Timestamp = DateTime.Now,
            Direction = direction,
            RemoteEndPoint = remoteEndPoint ?? "",
            Message = message,
            Length = message.Length
        };

        Logs.Insert(0, item);

        while (Logs.Count > MaxLogCount)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }
}

/// <summary>
/// 통신 로그 항목
/// </summary>
public class CommunicationLogItem
{
    public DateTime Timestamp { get; init; }
    /// <summary>
    /// Connect / Disconnect / Send / Receive / Error
    /// </summary>
    public string Direction { get; init; } = "";
    public string RemoteEndPoint { get; init; } = "";
    public string Message { get; init; } = "";
    public int Length { get; init; }
}
