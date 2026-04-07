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
/// Host Test (ACS Simulator) ViewModel.
/// ACS 관점에서 동작:
///   - Listen Server (3334): Host로부터 MOVECMD/ACTIONCMD 수신
///   - Send Client (→3333): Host로 JOBREPORT 송신 (connect-per-message)
/// </summary>
public partial class HostTestViewModel : ObservableObject
{
    // ── Connection Settings ──
    [ObservableProperty] private string _listenPort = "3334";
    [ObservableProperty] private string _sendHost = "127.0.0.1";
    [ObservableProperty] private string _sendPort = "3333";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "Disconnected";

    /// <summary>IsConnected의 반전 값 — XAML IsEnabled 바인딩용</summary>
    public bool IsNotConnected => !IsConnected;

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotConnected));
    }

    // ── JOBREPORT 송신 Fields ──
    [ObservableProperty] private string _reportType = "RECEIVE";
    [ObservableProperty] private string _acsId = "ACS01";
    [ObservableProperty] private string _amrId = "AMR01";
    [ObservableProperty] private string _actionType = "LOAD";
    [ObservableProperty] private string _jobId = "JOB001";
    [ObservableProperty] private string _materialType = "MAGAZINE";
    [ObservableProperty] private string _userId = "ACS01";
    [ObservableProperty] private string _errorCode = "0";
    [ObservableProperty] private string _errorMsg = "ACK";

    // ── Available JOBREPORT Types ──
    public string[] ReportTypes { get; } = { "RECEIVE", "START", "ARRIVED", "ACTION", "COMPLETE", "CANCEL" };

    // ── Last Received Command ──
    [ObservableProperty] private string _lastReceivedCommand = "-";
    [ObservableProperty] private string _lastReceivedJobId = "-";
    [ObservableProperty] private string _lastReceivedAcsId = "-";
    [ObservableProperty] private string _lastReceivedSourceLoc = "-";
    [ObservableProperty] private string _lastReceivedSourcePort = "-";
    [ObservableProperty] private string _lastReceivedDestLoc = "-";
    [ObservableProperty] private string _lastReceivedDestPort = "-";
    [ObservableProperty] private string _lastReceivedDetail = "-";

    // ── Log ──
    [ObservableProperty] private string _logText = "";

    // ── TCP ──
    private TcpListener _listener;
    private CancellationTokenSource _cts;

    // ========================================================
    //  Connect / Disconnect
    // ========================================================

    [RelayCommand]
    private void Connect()
    {
        if (IsConnected) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            int listenPortNum = int.Parse(ListenPort);
            _listener = new TcpListener(IPAddress.Any, listenPortNum);
            _listener.Start();
            AppendLog($"[INFO] Listen 서버 시작: port {listenPortNum} (Host MOVECMD/ACTIONCMD 수신 대기)");
            _ = Task.Run(() => AcceptLoopAsync(ct), ct);
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR] Listen 서버 시작 실패: {ex.Message}");
            return;
        }

        IsConnected = true;
        ConnectionStatus = "Connected";
    }

    [RelayCommand]
    private void Disconnect()
    {
        if (!IsConnected) return;

        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }

        IsConnected = false;
        ConnectionStatus = "Disconnected";
        AppendLog("[INFO] 연결 종료");
    }

    // ========================================================
    //  Listen Server (Host → ACS: MOVECMD/ACTIONCMD 수신)
    // ========================================================

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var ep = client.Client.RemoteEndPoint as IPEndPoint;
                AppendLog($"[INFO] Host 접속 수신: {ep?.Address}:{ep?.Port}");
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
                        AppendLog($"[INFO] Host 연결 끊김: {ep?.Address}:{ep?.Port}");
                        break;
                    }

                    string msgName = HostMessageProtocol.ExtractMessageName(xml);
                    AppendLog($"[RECV] {msgName} ({xml.Length} bytes)");
                    AppendLog(FormatXml(xml));

                    if (msgName == "MOVECMD")
                        ParseReceivedMoveCmd(xml);
                    else if (msgName == "ACTIONCMD")
                        ParseReceivedActionCmd(xml);
                    else
                        ParseReceivedGeneric(msgName, xml);
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

    private void ParseReceivedMoveCmd(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var dl = doc.SelectSingleNode("//DataLayer");
            if (dl == null) return;

            string jobId = dl.SelectSingleNode("JobID")?.InnerText ?? "-";
            string acsId = dl.SelectSingleNode("AcsId")?.InnerText ?? "-";
            string srcLoc = dl.SelectSingleNode("SourceLoc")?.InnerText ?? "-";
            string srcPort = dl.SelectSingleNode("SourcePort")?.InnerText ?? "-";
            string dstLoc = dl.SelectSingleNode("DestLoc")?.InnerText ?? "-";
            string dstPort = dl.SelectSingleNode("DestPort")?.InnerText ?? "-";
            string actType = dl.SelectSingleNode("ActionType")?.InnerText ?? "";
            string matType = dl.SelectSingleNode("MaterialType")?.InnerText ?? "";
            string userId = dl.SelectSingleNode("UserID")?.InnerText ?? "";

            string detail = $"ActionType: {actType}, MaterialType: {matType}, UserID: {userId}";

            Dispatcher.UIThread.Post(() =>
            {
                LastReceivedCommand = "MOVECMD";
                LastReceivedJobId = jobId;
                LastReceivedAcsId = acsId;
                LastReceivedSourceLoc = srcLoc;
                LastReceivedSourcePort = srcPort;
                LastReceivedDestLoc = dstLoc;
                LastReceivedDestPort = dstPort;
                LastReceivedDetail = detail;

                // 수신한 JobID를 JOBREPORT 응답 필드에 자동 반영
                JobId = jobId;
            });
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] MOVECMD 파싱 실패: {ex.Message}");
        }
    }

    private void ParseReceivedActionCmd(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var dl = doc.SelectSingleNode("//DataLayer");
            if (dl == null) return;

            string jobId = dl.SelectSingleNode("JobID")?.InnerText ?? "-";
            string acsId = dl.SelectSingleNode("AcsId")?.InnerText ?? "-";
            string tgtLoc = dl.SelectSingleNode("TargetLoc")?.InnerText ?? "-";
            string tgtPort = dl.SelectSingleNode("TargetPort")?.InnerText ?? "-";
            string actType = dl.SelectSingleNode("ActionType")?.InnerText ?? "";
            string matType = dl.SelectSingleNode("MaterialType")?.InnerText ?? "";
            string userId = dl.SelectSingleNode("UserID")?.InnerText ?? "";

            string detail = $"ActionType: {actType}, MaterialType: {matType}, UserID: {userId}";

            Dispatcher.UIThread.Post(() =>
            {
                LastReceivedCommand = "ACTIONCMD";
                LastReceivedJobId = jobId;
                LastReceivedAcsId = acsId;
                LastReceivedSourceLoc = "-";
                LastReceivedSourcePort = "-";
                LastReceivedDestLoc = tgtLoc;
                LastReceivedDestPort = tgtPort;
                LastReceivedDetail = detail;

                // 수신한 JobID를 JOBREPORT 응답 필드에 자동 반영
                JobId = jobId;
            });
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] ACTIONCMD 파싱 실패: {ex.Message}");
        }
    }

    private void ParseReceivedGeneric(string msgName, string xml)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LastReceivedCommand = msgName;
            LastReceivedDetail = $"(미지원 명령: {msgName})";
        });
    }

    // ========================================================
    //  JOBREPORT 송신 (ACS → Host)
    // ========================================================

    [RelayCommand]
    private async Task SendJobReport()
    {
        string xml = HostMessageProtocol.BuildMessage(
            "JOBREPORT",
            "/HQ/MES01",
            "/HQ/ACS01",
            dataLayer =>
            {
                var doc = dataLayer.OwnerDocument;
                AddElement(doc, dataLayer, "AcsId", AcsId);
                AddElement(doc, dataLayer, "Type", ReportType);
                AddElement(doc, dataLayer, "AmrId", AmrId);
                AddElement(doc, dataLayer, "ActionType", ActionType);
                AddElement(doc, dataLayer, "JobID", JobId);
                AddElement(doc, dataLayer, "MaterialType", MaterialType);
                AddElement(doc, dataLayer, "UserID", UserId);
                AddElement(doc, dataLayer, "ErrorCode", ErrorCode);
                AddElement(doc, dataLayer, "ErrorMsg", ErrorMsg);
            });

        await SendMessageAsync(xml);
    }

    private async Task SendMessageAsync(string xml)
    {
        try
        {
            using var client = new TcpClient();
            int sendPortNum = int.Parse(SendPort);
            await client.ConnectAsync(SendHost, sendPortNum).ConfigureAwait(false);
            using var stream = client.GetStream();
            await HostMessageProtocol.WriteMessageAsync(stream, xml).ConfigureAwait(false);

            string msgName = HostMessageProtocol.ExtractMessageName(xml);
            AppendLog($"[SEND] {msgName} ({xml.Length} bytes) → {SendHost}:{SendPort}");
            AppendLog(FormatXml(xml));
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR] 전송 실패 ({SendHost}:{SendPort}): {ex.Message}");
        }
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
