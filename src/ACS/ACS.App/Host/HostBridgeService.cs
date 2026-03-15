using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Hosting;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Core.Message.Model.Host;
using ACS.Core.Workflow;

namespace ACS.App.Host
{
    /// <summary>
    /// Host TCP/IP ↔ Workflow 브릿지 BackgroundService.
    ///
    /// 수신: Host TCP/IP → MessageReceived → 워크플로우 실행 (워크플로우 내에서 JOBREPORT 전송)
    /// 송신: Workflow/BIZ → ForwardToHost() → Host TCP/IP
    /// </summary>
    public class HostBridgeService : BackgroundService
    {
        private readonly IHostTcpGateway _tcpGateway;
        private readonly IWorkflowManager _workflowManager;
        private readonly Logger _logger = Logger.GetLogger(typeof(HostBridgeService));

        public HostBridgeService(
            IHostTcpGateway tcpGateway,
            IWorkflowManager workflowManager)
        {
            _tcpGateway = tcpGateway;
            _workflowManager = workflowManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _tcpGateway.MessageReceived += OnHostMessageReceived;
            _tcpGateway.Start();
            _logger.Info("HostBridgeService started - bridging TCP/IP ↔ Workflow");

            try
            {
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

        private void OnHostMessageReceived(object sender, HostTcpMessageEventArgs e)
        {
            try
            {
                _logger.Info($"[Bridge] Received from host: {e.MessageName}, length={e.MessageBody?.Length ?? 0}");

                var xmlDoc = ParseXml(e.MessageBody);
                if (xmlDoc == null)
                {
                    _logger.Error($"[Bridge] Failed to parse XML for {e.MessageName}");
                    return;
                }

                if (e.MessageName == "LOAD_COMPLETED")
                    e.MessageName = "MOVECMD";
                    

                switch (e.MessageName?.ToUpperInvariant())
                {
                    case "MOVECMD":
                        HandleMoveCommand(e.MessageName, xmlDoc);
                        break;

                    case "ACTIONCMD":
                        HandleActionCommand(e.MessageName, xmlDoc);
                        break;

                    case "LOAD_COMPLETED":
                    case "UNLOAD_COMPLETED":
                        HandleGenericCommand(e.MessageName, xmlDoc);
                        break;

                    default:
                        _logger.Info($"[Bridge] Unknown host message: {e.MessageName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[Bridge] Error processing host message: {e.MessageName} - {ex.Message}", ex);
            }
        }

        /// <summary>
        /// MOVECMD 처리:
        ///   워크플로우 실행 → 워크플로우 내에서 JOBREPORT(RECEIVE) 전송 + 추가 비즈니스 로직
        /// </summary>
        private void HandleMoveCommand(string messageName, XmlDocument xmlDoc)
        {
            var moveCommand = new MoveCommand
            {
                MessageName = messageName,
                Document = xmlDoc,
                TransactionId = ExtractXmlValue(xmlDoc, "//TransactionId") ?? Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                OriginatedType = "HOST"
            };

            _logger.Info($"[Bridge] MOVECMD created: TxId={moveCommand.TransactionId}");

            // 워크플로우 실행 (JOBREPORT 전송은 워크플로우 내 SendJobReportActivity에서 처리)
            try
            {
                bool result = _workflowManager.Execute(messageName, xmlDoc);
                _logger.Info($"[Bridge] MOVECMD workflow result: {result}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[Bridge] MOVECMD workflow error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ACTIONCMD 처리:
        ///   워크플로우 실행 → 워크플로우 내에서 JOBREPORT(RECEIVE) 전송 + 추가 비즈니스 로직
        /// </summary>
        private void HandleActionCommand(string messageName, XmlDocument xmlDoc)
        {
            var actionCommand = new ActionCommand
            {
                MessageName = messageName,
                Document = xmlDoc,
                TransactionId = ExtractXmlValue(xmlDoc, "//TransactionId") ?? Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                OriginatedType = "HOST"
            };

            _logger.Info($"[Bridge] ACTIONCMD created: TxId={actionCommand.TransactionId}");

            // 워크플로우 실행 (JOBREPORT 전송은 워크플로우 내 Activity에서 처리)
            try
            {
                bool result = _workflowManager.Execute(messageName, xmlDoc);
                _logger.Info($"[Bridge] ACTIONCMD workflow result: {result}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[Bridge] ACTIONCMD workflow error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 기타 Host 커맨드(LOAD_COMPLETED, UNLOAD_COMPLETED 등) → 워크플로우 실행
        /// </summary>
        private void HandleGenericCommand(string messageName, XmlDocument xmlDoc)
        {
            _logger.Info($"[Bridge] {messageName} → workflow execute");

            try
            {
                bool result = _workflowManager.Execute(messageName, xmlDoc);
                _logger.Info($"[Bridge] {messageName} workflow result: {result}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[Bridge] {messageName} workflow error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// BIZ/워크플로우에서 호출하여 Host로 리포트 전송.
        /// </summary>
        public void ForwardToHost(string messageName, string messageBody)
        {
            _logger.Info($"[Bridge] Forwarding to host: {messageName}");
            _tcpGateway.SendToHost(messageName, messageBody);
        }

        private XmlDocument ParseXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return doc;
            }
            catch (Exception ex)
            {
                _logger.Error($"[Bridge] XML parse error: {ex.Message}");
                return null;
            }
        }

        private string ExtractXmlValue(XmlDocument doc, string xpath)
        {
            try
            {
                return doc.SelectSingleNode(xpath)?.InnerText;
            }
            catch
            {
                return null;
            }
        }
    }
}
