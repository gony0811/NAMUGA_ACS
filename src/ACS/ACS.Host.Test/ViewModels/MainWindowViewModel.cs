using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ACS.Communication.Host;
using ACS.Communication.Host.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS.Host.Test.ViewModels;

/// <summary>
/// MES 시뮬레이터 ViewModel.
/// MES 관점에서 동작:
///   - Send Client (3334): ACS로 MOVECMD/MOVECANCEL/ACTIONCMD 송신
///   - Listen Server (3333): ACS로부터 JOBREPORT 수신
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    // ── Connection Settings ──
    [ObservableProperty] private int _listenPort = 3333;
    [ObservableProperty] private string _sendHost = "127.0.0.1";
    [ObservableProperty] private int _sendPort = 3334;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "Disconnected";

    // ── Command Type 선택 ──
    public List<string> CommandTypes { get; } = new() { "MOVECMD", "MOVECANCEL", "ACTIONCMD" };
    [ObservableProperty] private string _selectedCommandType = "MOVECMD";

    // ── Command DataLayer 필드 (전체 통합) ──
    [ObservableProperty] private string _cmdAcsId = "ACS01";
    [ObservableProperty] private string _cmdJobId = "JOB001";
    [ObservableProperty] private string _cmdSourceLoc = "";
    [ObservableProperty] private string _cmdSourcePort = "";
    [ObservableProperty] private string _cmdDestLoc = "";
    [ObservableProperty] private string _cmdDestPort = "";
    [ObservableProperty] private string _cmdTargetLoc = "";
    [ObservableProperty] private string _cmdTargetPort = "";
    [ObservableProperty] private string _cmdActionType = "";
    [ObservableProperty] private string _cmdMaterialType = "MAGAZINE";
    [ObservableProperty] private string _cmdUserId = "MES01";

    // ── 필드 가시성 (Command Type에 따라) ──
    [ObservableProperty] private bool _showMoveFields = true;
    [ObservableProperty] private bool _showActionFields;

    // ── Last Received Report ──
    [ObservableProperty] private string _lastReceivedCommand = "-";
    [ObservableProperty] private string _lastReceivedJobId = "-";
    [ObservableProperty] private string _lastReceivedDetail = "-";

    // ── Log ──
    [ObservableProperty] private string _logText = "";

    // ── TCP ──
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private volatile bool _isListening;

    partial void OnSelectedCommandTypeChanged(string value)
    {
        // MOVECMD / MOVECANCEL → Source/Dest 필드, ACTIONCMD → Target 필드
        ShowMoveFields = value == "MOVECMD" || value == "MOVECANCEL";
        ShowActionFields = value == "ACTIONCMD";
    }

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
            _listener = new TcpListener(IPAddress.Any, ListenPort);
            _listener.Start();
            _isListening = true;
            AppendLog($"[INFO] Listen 서버 시작: port {ListenPort} (ACS JOBREPORT 수신 대기)");
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

        _isListening = false;

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
                var client = await _listener!.AcceptTcpClientAsync().ConfigureAwait(false);
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
                    string? xml = await HostMessageProtocol.ReadMessageAsync(stream, ct).ConfigureAwait(false);
                    if (xml == null)
                    {
                        AppendLog($"[INFO] ACS 연결 끊김: {ep?.Address}:{ep?.Port}");
                        break;
                    }

                    string msgName = HostMessageProtocol.ExtractMessageName(xml);
                    AppendLog($"[RECV] {msgName} ({xml.Length} bytes)");
                    AppendLog(FormatXml(xml));

                    ParseReceivedMessage(msgName, xml);
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

    private void ParseReceivedMessage(string msgName, string xml)
    {
        try
        {
            string jobId = "-";
            string detail = "-";

            if (msgName == "JOBREPORT")
            {
                var msg = HostXmlSerializer.Deserialize<HostMessage<JobReportData>>(xml);
                var d = msg.DataLayer;
                jobId = d.JobID ?? "-";

                detail = $"AcsId: {d.AcsId}\n" +
                         $"Type: {d.Type}\n" +
                         $"AmrId: {d.AmrId}\n" +
                         $"ActionType: {d.ActionType}\n" +
                         $"MaterialType: {d.MaterialType}\n" +
                         $"UserID: {d.UserID}";
            }
            else
            {
                detail = $"(알 수 없는 메시지: {msgName})";
            }

            Dispatcher.UIThread.Post(() =>
            {
                LastReceivedCommand = msgName;
                LastReceivedJobId = jobId;
                LastReceivedDetail = detail;
            });
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] 메시지 파싱 실패: {ex.Message}");
        }
    }

    // ========================================================
    //  Command 송신 (MES → ACS: MOVECMD / MOVECANCEL / ACTIONCMD)
    // ========================================================

    [RelayCommand]
    private void SendCommand()
    {
        switch (SelectedCommandType)
        {
            case "MOVECMD":
                SendMoveCommand();
                break;
            case "MOVECANCEL":
                SendMoveCancel();
                break;
            case "ACTIONCMD":
                SendActionCommand();
                break;
        }
    }

    private async void SendMoveCommand()
    {
        var message = new HostMessage<MoveCommandData>
        {
            Command = "MOVECMD",
            Header = new HostHeader
            {
                DestSubject = "/HQ/ACS01",
                ReplySubject = "/HQ/MES01"
            },
            DataLayer = new MoveCommandData
            {
                AcsId = CmdAcsId,
                DestLoc = CmdDestLoc,
                DestPort = CmdDestPort,
                ActionType = CmdActionType,
                SourceLoc = CmdSourceLoc,
                SourcePort = CmdSourcePort,
                JobID = CmdJobId,
                MaterialType = CmdMaterialType,
                UserID = CmdUserId
            }
        };

        await SendMessageAsync("MOVECMD", message);
    }

    private async void SendMoveCancel()
    {
        var message = new HostMessage<MoveCancelData>
        {
            Command = "MOVECANCEL",
            Header = new HostHeader
            {
                DestSubject = "/HQ/ACS01",
                ReplySubject = "/HQ/MES01"
            },
            DataLayer = new MoveCancelData
            {
                AcsId = CmdAcsId,
                DestLoc = CmdDestLoc,
                SourceLoc = CmdSourceLoc,
                JobId = CmdJobId,
                MaterialType = CmdMaterialType,
                UserId = CmdUserId
            }
        };

        await SendMessageAsync("MOVECANCEL", message);
    }

    private async void SendActionCommand()
    {
        var message = new HostMessage<ActionCommandData>
        {
            Command = "ACTIONCMD",
            Header = new HostHeader
            {
                DestSubject = "/HQ/ACS01",
                ReplySubject = "/HQ/MES01"
            },
            DataLayer = new ActionCommandData
            {
                AcsId = CmdAcsId,
                TargetLoc = CmdTargetLoc,
                TargetPort = CmdTargetPort,
                JobID = CmdJobId,
                MaterialType = CmdMaterialType,
                ActionType = CmdActionType,
                UserID = CmdUserId
            }
        };

        await SendMessageAsync("ACTIONCMD", message);
    }

    private async Task SendMessageAsync<T>(string commandName, HostMessage<T> message) where T : class, new()
    {
        string xml = HostXmlSerializer.Serialize(message);

        try
        {
            await HostMessageProtocol.ConnectAndSendAsync(SendHost, SendPort, xml);
            AppendLog($"[SEND] {commandName} ({xml.Length} bytes) → {SendHost}:{SendPort}");
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
