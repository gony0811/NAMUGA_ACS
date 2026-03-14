using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ACS.Framework.Host;
using ACS.Framework.Logging;

namespace ACS.Communication.Host
{
    /// <summary>
    /// Host(MES)와의 TCP/IP 통신 게이트웨이 실제 구현.
    ///
    /// 듀얼 포트 구조:
    ///   - 수신 (Host→ACS): ACS가 ListenPort(3334)를 열고 대기 → Host가 접속 → MOVECMD, ACTIONCMD 수신
    ///   - 송신 (ACS→Host): ACS가 SendHost:SendPort(3333)에 접속 → MOVECMD_REPLY, JOBREPORT 전송
    /// </summary>
    public class HostTcpGateway : IHostTcpGateway
    {
        private readonly Logger logger = Logger.GetLogger(typeof(HostTcpGateway));

        // ── 설정 (Autofac PropertiesAutowired로 주입 가능) ──
        /// <summary>수신 대기 포트 (Host가 접속해서 메시지를 보내는 포트)</summary>
        public int ListenPort { get; set; } = 3334;

        /// <summary>송신 대상 Host IP</summary>
        public string SendHost { get; set; } = "192.168.0.69";

        /// <summary>송신 대상 포트 (ACS가 Host에 접속해서 메시지를 보내는 포트)</summary>
        public int SendPort { get; set; } = 3333;

        /// <summary>재연결 간격 (밀리초)</summary>
        public int ReconnectIntervalMs { get; set; } = 5000;

        // ── 수신 측 (Server) ──
        private TcpListener _listener;
        private Task _acceptTask;

        // ── 송신 측 (Client) ──
        private TcpClient _sendClient;
        private NetworkStream _sendStream;
        private readonly object _sendLock = new object();
        private Task _sendConnectTask;

        // ── 공통 ──
        private CancellationTokenSource _cts;
        private volatile bool _isListening;
        private volatile bool _isSendConnected;

        public bool IsConnected => _isListening || _isSendConnected;

        public event EventHandler<HostTcpMessageEventArgs> MessageReceived;

        // ========================================================
        //  Start / Stop
        // ========================================================

        public void Start()
        {
            _cts = new CancellationTokenSource();

            // 수신 서버 시작
            StartListener();

            // 송신 클라이언트 연결 시작 (백그라운드)
            _sendConnectTask = Task.Run(() => SendConnectLoopAsync(_cts.Token));

            logger.Info($"[HostTcpGateway] Started - Listen:{ListenPort}, Send:{SendHost}:{SendPort}");
        }

        public void Stop()
        {
            logger.Info("[HostTcpGateway] Stopping...");

            _cts?.Cancel();

            try { _listener?.Stop(); } catch { }
            try { _sendStream?.Close(); } catch { }
            try { _sendClient?.Close(); } catch { }

            _isListening = false;
            _isSendConnected = false;

            logger.Info("[HostTcpGateway] Stopped.");
        }

        // ========================================================
        //  수신 (Server: Host → ACS)
        // ========================================================

        private void StartListener()
        {
            _listener = new TcpListener(IPAddress.Any, ListenPort);
            _listener.Start();
            _isListening = true;
            _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
            logger.Info($"[HostTcpGateway] Listening on port {ListenPort}");
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    var ep = client.Client.RemoteEndPoint as IPEndPoint;
                    logger.Info($"[HostTcpGateway] Host connected from {ep?.Address}:{ep?.Port}");

                    // 각 연결을 별도 Task로 처리
                    _ = Task.Run(() => ReceiveLoopAsync(client, ct), ct);
                }
                catch (ObjectDisposedException)
                {
                    break; // listener 종료됨
                }
                catch (SocketException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        logger.Error($"[HostTcpGateway] Accept error: {ex.Message}");
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                    }
                }
            }

            _isListening = false;
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
                            logger.Info($"[HostTcpGateway] Host disconnected: {ep?.Address}:{ep?.Port}");
                            break;
                        }

                        string msgName = HostMessageProtocol.ExtractMessageName(xml);
                        logger.Info($"[HostTcpGateway] Received: {msgName} ({xml.Length} bytes)");

                        try
                        {
                            MessageReceived?.Invoke(this, new HostTcpMessageEventArgs
                            {
                                MessageName = msgName,
                                MessageBody = xml
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"[HostTcpGateway] MessageReceived handler error: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    logger.Error($"[HostTcpGateway] Receive error from {ep?.Address}: {ex.Message}");
            }
        }

        // ========================================================
        //  송신 (Client: ACS → Host)
        // ========================================================

        public void SendToHost(string messageName, string messageBody)
        {
            if (string.IsNullOrEmpty(messageBody))
            {
                logger.Warn($"[HostTcpGateway] SendToHost - empty body for {messageName}");
                return;
            }

            lock (_sendLock)
            {
                if (_sendStream == null || !_isSendConnected)
                {
                    logger.Warn($"[HostTcpGateway] SendToHost - not connected to Host {SendHost}:{SendPort}, message '{messageName}' queued for retry");
                    return;
                }

                try
                {
                    HostMessageProtocol.WriteMessageAsync(_sendStream, messageBody).GetAwaiter().GetResult();
                    logger.Info($"[HostTcpGateway] Sent: {messageName} ({messageBody.Length} bytes)");
                }
                catch (Exception ex)
                {
                    logger.Error($"[HostTcpGateway] Send error: {ex.Message}");
                    DisconnectSendClient();
                }
            }
        }

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

                        logger.Info($"[HostTcpGateway] Connected to Host {SendHost}:{SendPort} for sending");
                    }
                    catch (Exception ex)
                    {
                        if (!ct.IsCancellationRequested)
                            logger.Debug($"[HostTcpGateway] Send connect failed ({SendHost}:{SendPort}): {ex.Message}, retrying in {ReconnectIntervalMs}ms");
                    }
                }

                try
                {
                    await Task.Delay(ReconnectIntervalMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void DisconnectSendClient()
        {
            _isSendConnected = false;
            try { _sendStream?.Close(); } catch { }
            try { _sendClient?.Close(); } catch { }
            _sendStream = null;
            _sendClient = null;
        }
    }
}
