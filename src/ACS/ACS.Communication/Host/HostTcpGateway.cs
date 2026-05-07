using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ACS.Communication.Host.Models;
using ACS.Core.Host;
using ACS.Core.Logging;

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
        public string SendHost { get; set; } = "172.31.112.1";

        /// <summary>송신 대상 포트 (ACS가 Host에 접속해서 메시지를 보내는 포트)</summary>
        public int SendPort { get; set; } = 3333;

        /// <summary>재연결 간격 (밀리초)</summary>
        public int ReconnectIntervalMs { get; set; } = 5000;

        // ── 수신 측 (Server) ──
        private TcpListener _listener;
        private Task _acceptTask;

        // ── 공통 ──
        private CancellationTokenSource _cts;
        private volatile bool _isListening;

        public bool IsConnected => _isListening;

        public event EventHandler<HostTcpMessageEventArgs> MessageReceived;
        public event EventHandler<HostTcpConnectionEventArgs> Connected;
        public event EventHandler<HostTcpConnectionEventArgs> Disconnected;
        public event EventHandler<HostTcpMessageSentEventArgs> MessageSent;

        // ========================================================
        //  Start / Stop
        // ========================================================

        public void Start()
        {
            _cts = new CancellationTokenSource();

            // 수신 서버 시작
            StartListener();

            logger.Info($"[HostTcpGateway] Started - Listen:{ListenPort}, Send:{SendHost}:{SendPort}");
        }

        public void Stop()
        {
            logger.Info("[HostTcpGateway] Stopping...");

            _cts?.Cancel();

            try { _listener?.Stop(); } catch { }

            _isListening = false;

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
                    string remote = $"{ep?.Address}:{ep?.Port}";
                    logger.Info($"[HostTcpGateway] Host connected from {remote}");

                    SafeRaise(Connected, new HostTcpConnectionEventArgs { RemoteEndPoint = remote, Connected = true });

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
            string remote = $"{ep?.Address}:{ep?.Port}";
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
                            logger.Info($"[HostTcpGateway] Host disconnected: {remote}");
                            break;
                        }

                        string msgName = HostMessageProtocol.ExtractMessageName(xml);
                        logger.Info($"[HostTcpGateway] Received: {msgName} ({xml.Length} bytes)");

                        try
                        {
                            MessageReceived?.Invoke(this, new HostTcpMessageEventArgs
                            {
                                MessageName = msgName,
                                MessageBody = xml,
                                RemoteEndPoint = remote
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
            finally
            {
                SafeRaise(Disconnected, new HostTcpConnectionEventArgs { RemoteEndPoint = remote, Connected = false });
            }
        }

        // ========================================================
        //  송신 (Client: ACS → Host)
        // ========================================================

        public void SendToHost(string messageName, string messageBody)
        {
            string remote = $"{SendHost}:{SendPort}";

            if (string.IsNullOrEmpty(messageBody))
            {
                logger.Warn($"[HostTcpGateway] SendToHost - empty body for {messageName}");
                SafeRaise(MessageSent, new HostTcpMessageSentEventArgs
                {
                    MessageName = messageName,
                    MessageBody = messageBody,
                    RemoteEndPoint = remote,
                    Success = false,
                    Error = "empty body"
                });
                return;
            }

            try
            {
                HostMessageProtocol.ConnectAndSendAsync(SendHost, SendPort, messageBody).GetAwaiter().GetResult();
                logger.Info($"[HostTcpGateway] Sent: {messageName} ({messageBody.Length} bytes) to {remote}");
                SafeRaise(MessageSent, new HostTcpMessageSentEventArgs
                {
                    MessageName = messageName,
                    MessageBody = messageBody,
                    RemoteEndPoint = remote,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                logger.Error($"[HostTcpGateway] Send error to {remote}: {ex.Message}");
                SafeRaise(MessageSent, new HostTcpMessageSentEventArgs
                {
                    MessageName = messageName,
                    MessageBody = messageBody,
                    RemoteEndPoint = remote,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 모델 객체를 직렬화하여 Host로 전송.
        /// </summary>
        public void SendToHost<TData>(string messageName, HostMessage<TData> message) where TData : class, new()
        {
            string xml = HostXmlSerializer.Serialize(message);
            SendToHost(messageName, xml);
        }

        private void SafeRaise<T>(EventHandler<T> handler, T args) where T : EventArgs
        {
            if (handler == null) return;
            try { handler.Invoke(this, args); }
            catch (Exception ex) { logger.Error($"[HostTcpGateway] event handler error: {ex.Message}"); }
        }
    }
}
