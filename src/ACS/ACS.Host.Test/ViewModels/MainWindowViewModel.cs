using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ACS.Communication.Host;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS.Host.Test.ViewModels;

/// <summary>
/// ACS 시뮬레이터 ViewModel.
/// ACS 관점에서 동작:
///   - Listen Server (3334): MES로부터 MOVECMD/ACTIONCMD 수신
///   - Send Client (3333): MES로 JOBREPORT 송신
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    // ── Connection Settings ──
    [ObservableProperty] private int _listenPort = 3334;
    [ObservableProperty] private string _sendHost = "127.0.0.1";
    [ObservableProperty] private int _sendPort = 3333;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "Disconnected";

    // ── Job Report Fields ──
    [ObservableProperty] private string _jobId = "JOB001";
    [ObservableProperty] private string _amrId = "";
    [ObservableProperty] private string _acsId = "ACS01";
    [ObservableProperty] private string _materialType = "MAGAZINE";
    [ObservableProperty] private string _userId = "MES01";
    [ObservableProperty] private string _completedPort = "";

    // ── Last Received Command ──
    [ObservableProperty] private string _lastReceivedCommand = "-";
    [ObservableProperty] private string _lastReceivedJobId = "-";
    [ObservableProperty] private string _lastReceivedDetail = "-";

    // ── Log ──
    [ObservableProperty] private string _logText = "";

    // ── TCP ──
    private TcpListener _listener;
    private TcpClient _sendClient;
    private NetworkStream _sendStream;
    private CancellationTokenSource _cts;
    private readonly object _sendLock = new();

    private volatile bool _isListening;
    private volatile bool _isSendConnected;

    // ========================================================
    //  Connect / Disconnect
    // ========================================================

    [RelayCommand]
    private void Connect()
    {
        if (IsConnected) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // Listen 서버 시작 (MES로부터 MOVECMD/ACTIONCMD 수신)
        try
        {
            _listener = new TcpListener(IPAddress.Any, ListenPort);
            _listener.Start();
            _isListening = true;
            AppendLog($"[INFO] Listen 서버 시작: port {ListenPort}");
            _ = Task.Run(() => AcceptLoopAsync(ct), ct);
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR] Listen 서버 시작 실패: {ex.Message}");
            return;
        }

        // Send 클라이언트 연결 (MES에 접속하여 JOBREPORT 송신)
        _ = Task.Run(() => SendConnectLoopAsync(ct), ct);

        IsConnected = true;
        ConnectionStatus = "Connected";
    }

    [RelayCommand]
    private void Disconnect()
    {
        if (!IsConnected) return;

        _cts?.Cancel();

        try { _listener?.Stop(); } catch { }
        try { _sendStream?.Close(); } catch { }
        try { _sendClient?.Close(); } catch { }

        _isListening = false;
        _isSendConnected = false;
        _sendStream = null;
        _sendClient = null;

        IsConnected = false;
        ConnectionStatus = "Disconnected";
        AppendLog("[INFO] 연결 종료");
    }

    // ========================================================
    //  Listen Server (MES → ACS: MOVECMD/ACTIONCMD 수신)
    // ========================================================

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var ep = client.Client.RemoteEndPoint as IPEndPoint;
                AppendLog($"[INFO] MES 접속 수신: {ep?.Address}:{ep?.Port}");
                _ = Task.Run(() => ReceiveLoopAsync(client, ct), ct);
            }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    AppendLog($"[ERROR] Accept 오류: {ex.Message}");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken ct)
    {
        var ep = client.Client.RemoteEndPoint as IPEndPoint;
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                while (!ct.IsCancellationRequested)
                {
                    string xml = await HostMessageProtocol.ReadMessageAsync(stream, ct).ConfigureAwait(false);
                    if (xml == null)
                    {
                        AppendLog($"[INFO] MES 연결 끊김: {ep?.Address}:{ep?.Port}");
                        break;
                    }

                    string msgName = HostMessageProtocol.ExtractMessageName(xml);
                    AppendLog($"[RECV] {msgName} ({xml.Length} bytes)");
                    AppendLog(FormatXml(xml));

                    // MOVECMD/ACTIONCMD 수신 시 파싱하여 UI에 표시
                    if (msgName == "MOVECMD" || msgName == "ACTIONCMD")
                    {
                        ParseReceivedCommand(msgName, xml);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
                AppendLog($"[ERROR] 수신 오류: {ex.Message}");
        }
    }

    private void ParseReceivedCommand(string command, string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var dataLayer = doc.SelectSingleNode("//DataLayer");
            if (dataLayer == null) return;

            string jobId = dataLayer.SelectSingleNode("JobID")?.InnerText ?? "-";
            string detail;

            if (command == "MOVECMD")
            {
                string sourceEqp = dataLayer.SelectSingleNode("SourceEQP")?.InnerText ?? "";
                string sourcePort = dataLayer.SelectSingleNode("SourcePort")?.InnerText ?? "";
                string finalEqp = dataLayer.SelectSingleNode("FinalEQP")?.InnerText ?? "";
                string finalPort = dataLayer.SelectSingleNode("FinalPort")?.InnerText ?? "";
                string matType = dataLayer.SelectSingleNode("MatType")?.InnerText ?? "";
                string priority = dataLayer.SelectSingleNode("Priority")?.InnerText ?? "";
                string carrId = dataLayer.SelectSingleNode("CarrID")?.InnerText ?? "";

                detail = $"Priority: {priority}\n" +
                         $"Source: {sourceEqp} / {sourcePort}\n" +
                         $"Final: {finalEqp} / {finalPort}\n" +
                         $"MatType: {matType}, CarrID: {carrId}";
            }
            else // ACTIONCMD
            {
                string actionType = dataLayer.SelectSingleNode("ActionType")?.InnerText ?? "";
                string userId = dataLayer.SelectSingleNode("UserID")?.InnerText ?? "";

                detail = $"ActionType: {actionType}\nUserID: {userId}";
            }

            Dispatcher.UIThread.Post(() =>
            {
                LastReceivedCommand = command;
                LastReceivedJobId = jobId;
                LastReceivedDetail = detail;
            });
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] Command 파싱 실패: {ex.Message}");
        }
    }

    // ========================================================
    //  Send Client (ACS → MES: JOBREPORT 송신)
    // ========================================================

    private async Task SendConnectLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!_isSendConnected)
            {
                try
                {
                    var client = new TcpClient();
                    await client.ConnectAsync(SendHost, SendPort).ConfigureAwait(false);
                    lock (_sendLock)
                    {
                        _sendClient = client;
                        _sendStream = client.GetStream();
                        _isSendConnected = true;
                    }
                    AppendLog($"[INFO] MES 전송 연결 성공: {SendHost}:{SendPort}");
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                        AppendLog($"[INFO] MES 전송 연결 대기중 ({SendHost}:{SendPort}): {ex.Message}");
                }
            }

            try { await Task.Delay(5000, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void SendMessage(string xml)
    {
        lock (_sendLock)
        {
            if (_sendStream == null || !_isSendConnected)
            {
                AppendLog("[WARN] MES 전송 연결이 없습니다.");
                return;
            }

            try
            {
                HostMessageProtocol.WriteMessageAsync(_sendStream, xml).GetAwaiter().GetResult();
                string msgName = HostMessageProtocol.ExtractMessageName(xml);
                AppendLog($"[SEND] {msgName} ({xml.Length} bytes)");
                AppendLog(FormatXml(xml));
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 전송 실패: {ex.Message}");
                _isSendConnected = false;
                try { _sendStream?.Close(); } catch { }
                try { _sendClient?.Close(); } catch { }
                _sendStream = null;
                _sendClient = null;
            }
        }
    }

    // ========================================================
    //  Job Report 전송 (RECEIVE / START / ARRIVED / COMPLETED)
    // ========================================================

    [RelayCommand]
    private void SendReceive() => SendJobReport("RECEIVE");

    [RelayCommand]
    private void SendStart() => SendJobReport("START");

    [RelayCommand]
    private void SendArrived() => SendJobReport("ARRIVED");

    [RelayCommand]
    private void SendCompleted() => SendJobReport("COMPLETE");

    private void SendJobReport(string reportType)
    {
        string xml = HostMessageProtocol.BuildMessage(
            "JOBREPORT",
            "/HQ/MES01",
            "/HQ/ACS01",
            dataLayer =>
            {
                var doc = dataLayer.OwnerDocument;

                AddElement(doc, dataLayer, "AcsId", AcsId);
                AddElement(doc, dataLayer, "Type", reportType);
                AddElement(doc, dataLayer, "AmrId", AmrId);
                AddElement(doc, dataLayer, "JobID", JobId);
                AddElement(doc, dataLayer, "MaterialType", MaterialType);
                AddElement(doc, dataLayer, "UserId", UserId);

                if (reportType == "COMPLETE")
                    AddElement(doc, dataLayer, "Port", CompletedPort);
            });

        SendMessage(xml);
    }

    // ========================================================
    //  Log
    // ========================================================

    [RelayCommand]
    private void ClearLog()
    {
        LogText = "";
    }

    private void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";

        if (Dispatcher.UIThread.CheckAccess())
        {
            LogText += line;
        }
        else
        {
            Dispatcher.UIThread.Post(() => LogText += line);
        }
    }

    // ========================================================
    //  Helpers
    // ========================================================

    private static void AddElement(XmlDocument doc, XmlElement parent, string name, string value)
    {
        var elem = doc.CreateElement(name);
        elem.InnerText = value ?? "";
        parent.AppendChild(elem);
    }

    private static string FormatXml(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            using var sw = new System.IO.StringWriter();
            using var xw = new XmlTextWriter(sw) { Formatting = Formatting.Indented, Indentation = 2 };
            doc.WriteTo(xw);
            return sw.ToString();
        }
        catch
        {
            return xml;
        }
    }
}
