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
/// MES 시뮬레이터 ViewModel.
/// MES 관점에서 동작:
///   - Listen Server (3333): ACS로부터 JOBREPORT 수신
///   - Send Client (→3334): ACS로 MOVECMD/ACTIONCMD 송신 (connect-per-message)
/// </summary>
public partial class MesSimulatorViewModel : ObservableObject
{
    // ── Connection Settings ──
    [ObservableProperty] private string _listenPort = "3333";
    [ObservableProperty] private string _sendHost = "127.0.0.1";
    [ObservableProperty] private string _sendPort = "3334";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "Disconnected";

    /// <summary>IsConnected의 반전 값 — XAML IsEnabled 바인딩용</summary>
    public bool IsNotConnected => !IsConnected;

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotConnected));
    }

    // ── MOVECMD Fields ──
    [ObservableProperty] private string _jobId = "JOB001";
    [ObservableProperty] private string _sourceLoc = "";
    [ObservableProperty] private string _sourcePort = "";
    [ObservableProperty] private string _destLoc = "";
    [ObservableProperty] private string _destPort = "";
    [ObservableProperty] private string _actionType = "LOAD";
    [ObservableProperty] private string _materialType = "MAGAZINE";
    [ObservableProperty] private string _acsId = "ACS01";
    [ObservableProperty] private string _userId = "MES01";

    // ── ACTIONCMD Fields ──
    [ObservableProperty] private string _actionJobId = "ACT001";
    [ObservableProperty] private string _targetLoc = "";
    [ObservableProperty] private string _targetPort = "";
    [ObservableProperty] private string _actionActionType = "CHARGE";
    [ObservableProperty] private string _actionAcsId = "ACS01";
    [ObservableProperty] private string _actionUserId = "MES01";
    [ObservableProperty] private string _actionMaterialType = "";

    // ── Last Received JOBREPORT ──
    [ObservableProperty] private string _lastReceivedType = "-";
    [ObservableProperty] private string _lastReceivedJobId = "-";
    [ObservableProperty] private string _lastReceivedAmrId = "-";
    [ObservableProperty] private string _lastReceivedErrCode = "-";
    [ObservableProperty] private string _lastReceivedErrMsg = "-";
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

        // Listen 서버 시작 (ACS로부터 JOBREPORT 수신)
        try
        {
            int listenPortNum = int.Parse(ListenPort);
            _listener = new TcpListener(IPAddress.Any, listenPortNum);
            _listener.Start();
            AppendLog($"[INFO] Listen 서버 시작: port {listenPortNum} (ACS JOBREPORT 수신 대기)");
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
    //  Listen Server (ACS → MES: JOBREPORT 수신)
    // ========================================================

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var ep = client.Client.RemoteEndPoint as IPEndPoint;
                AppendLog($"[INFO] ACS 접속 수신: {ep?.Address}:{ep?.Port}");
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
                        AppendLog($"[INFO] ACS 연결 끊김: {ep?.Address}:{ep?.Port}");
                        break;
                    }

                    string msgName = HostMessageProtocol.ExtractMessageName(xml);
                    AppendLog($"[RECV] {msgName} ({xml.Length} bytes)");
                    AppendLog(FormatXml(xml));

                    if (msgName == "JOBREPORT")
                    {
                        ParseReceivedJobReport(xml);
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

    private void ParseReceivedJobReport(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var dataLayer = doc.SelectSingleNode("//DataLayer");
            if (dataLayer == null) return;

            string type = dataLayer.SelectSingleNode("Type")?.InnerText ?? "-";
            string jobId = dataLayer.SelectSingleNode("JobID")?.InnerText ?? "-";
            string amrId = dataLayer.SelectSingleNode("AmrId")?.InnerText ?? "-";
            string errCode = dataLayer.SelectSingleNode("ErrorCode")?.InnerText ?? "-";
            string errMsg = dataLayer.SelectSingleNode("ErrorMsg")?.InnerText ?? "-";
            string acsId = dataLayer.SelectSingleNode("AcsId")?.InnerText ?? "";
            string actionType = dataLayer.SelectSingleNode("ActionType")?.InnerText ?? "";
            string matType = dataLayer.SelectSingleNode("MaterialType")?.InnerText ?? "";

            string detail = $"AcsId: {acsId}, ActionType: {actionType}, MaterialType: {matType}";

            Dispatcher.UIThread.Post(() =>
            {
                LastReceivedType = type;
                LastReceivedJobId = jobId;
                LastReceivedAmrId = amrId;
                LastReceivedErrCode = errCode;
                LastReceivedErrMsg = errMsg;
                LastReceivedDetail = detail;
            });
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] JOBREPORT 파싱 실패: {ex.Message}");
        }
    }

    // ========================================================
    //  Send (MES → ACS: MOVECMD/ACTIONCMD 송신, connect-per-message)
    // ========================================================

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
    //  MOVECMD 송신
    // ========================================================

    [RelayCommand]
    private async Task SendMoveCmd()
    {
        string xml = HostMessageProtocol.BuildMessage(
            "MOVECMD",
            "/HQ/ACS01",
            "/HQ/MES01",
            dataLayer =>
            {
                var doc = dataLayer.OwnerDocument;
                AddElement(doc, dataLayer, "AcsId", AcsId);
                AddElement(doc, dataLayer, "SourceLoc", SourceLoc);
                AddElement(doc, dataLayer, "SourcePort", SourcePort);
                AddElement(doc, dataLayer, "DestLoc", DestLoc);
                AddElement(doc, dataLayer, "DestPort", DestPort);
                AddElement(doc, dataLayer, "ActionType", ActionType);
                AddElement(doc, dataLayer, "JobID", JobId);
                AddElement(doc, dataLayer, "MaterialType", MaterialType);
                AddElement(doc, dataLayer, "UserID", UserId);
            });

        await SendMessageAsync(xml);
    }

    // ========================================================
    //  ACTIONCMD 송신
    // ========================================================

    [RelayCommand]
    private async Task SendActionCmd()
    {
        string xml = HostMessageProtocol.BuildMessage(
            "ACTIONCMD",
            "/HQ/ACS01",
            "/HQ/MES01",
            dataLayer =>
            {
                var doc = dataLayer.OwnerDocument;
                AddElement(doc, dataLayer, "AcsId", ActionAcsId);
                AddElement(doc, dataLayer, "TargetLoc", TargetLoc);
                AddElement(doc, dataLayer, "TargetPort", TargetPort);
                AddElement(doc, dataLayer, "ActionType", ActionActionType);
                AddElement(doc, dataLayer, "JobID", ActionJobId);
                AddElement(doc, dataLayer, "MaterialType", ActionMaterialType);
                AddElement(doc, dataLayer, "UserID", ActionUserId);
            });

        await SendMessageAsync(xml);
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
