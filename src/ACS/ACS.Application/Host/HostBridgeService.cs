using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ACS.Framework.Host;
using ACS.Framework.Logging;

namespace ACS.Application.Host
{
    /// <summary>
    /// Host TCP/IP ↔ RabbitMQ 브릿지 BackgroundService.
    ///
    /// Direction 1 (수신): Host TCP/IP → MessageReceived 이벤트 → 워크플로우를 통해 Trans로 전달
    /// Direction 2 (송신): Trans → RabbitMQ → ForwardToHost() → Host TCP/IP로 리포트 전송
    ///
    /// TCP/IP가 stub인 현재 단계에서는 로그만 출력.
    /// </summary>
    public class HostBridgeService : BackgroundService
    {
        private readonly IHostTcpGateway _tcpGateway;
        private readonly IHostMessageManager _hostMessageManager;
        private readonly Logger _logger = Logger.GetLogger(typeof(HostBridgeService));

        public HostBridgeService(IHostTcpGateway tcpGateway, IHostMessageManager hostMessageManager)
        {
            _tcpGateway = tcpGateway;
            _hostMessageManager = hostMessageManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _tcpGateway.MessageReceived += OnHostMessageReceived;
            _tcpGateway.Start();
            _logger.Info("HostBridgeService started - bridging TCP/IP ↔ RabbitMQ");

            try
            {
                // 취소될 때까지 대기
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            finally
            {
                _tcpGateway.MessageReceived -= OnHostMessageReceived;
                _tcpGateway.Stop();
                _logger.Info("HostBridgeService stopped.");
            }
        }

        /// <summary>
        /// Host TCP/IP에서 메시지 수신 시 호출.
        /// MOVECMD, MOVECANCEL, MOVEUPDATE 등을 RabbitMQ를 통해 Trans로 전달.
        /// </summary>
        private void OnHostMessageReceived(object sender, HostTcpMessageEventArgs e)
        {
            try
            {
                _logger.Info($"[Bridge] Received from host TCP: {e.MessageName}, body length={e.MessageBody?.Length ?? 0}");

                switch (e.MessageName?.ToUpperInvariant())
                {
                    case "MOVECMD":
                        _logger.Info($"[Bridge] Processing MOVECMD from Host");
                        // TODO: XML → TransportCommand 생성 → 워크플로우 실행
                        break;

                    case "ACTIONCMD":
                        _logger.Info($"[Bridge] Processing ACTIONCMD from Host");
                        // TODO: XML → Action 처리 → 워크플로우 실행
                        break;

                    case "LOAD_COMPLETED":
                        _logger.Info($"[Bridge] Processing LOAD_COMPLETED from Host");
                        // TODO: 로드 완료 처리
                        break;

                    case "UNLOAD_COMPLETED":
                        _logger.Info($"[Bridge] Processing UNLOAD_COMPLETED from Host");
                        // TODO: 언로드 완료 처리
                        break;

                    default:
                        _logger.Info($"[Bridge] Received host message: {e.MessageName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[Bridge] Error processing host message: {e.MessageName} - {ex.Message}");
            }
        }

        /// <summary>
        /// BIZ/워크플로우에서 호출하여 Host로 리포트 전송.
        /// TRSJOBREPORT, TRSSTATEREPORT 등.
        /// </summary>
        public void ForwardToHost(string messageName, string messageBody)
        {
            _logger.Info($"[Bridge] Forwarding to host TCP: {messageName}");
            _tcpGateway.SendToHost(messageName, messageBody);
        }
    }
}
