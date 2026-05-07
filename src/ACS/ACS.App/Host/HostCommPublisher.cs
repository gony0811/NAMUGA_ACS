using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ACS.Core.Host;

namespace ACS.App.Host
{
    /// <summary>
    /// host 프로세스(HS01_P) 전용 BackgroundService.
    /// IHostTcpGateway의 Connected/Disconnected/MessageReceived/MessageSent 이벤트를 구독해
    /// RabbitMQ fanout exchange(${domainValue}/UI/HOSTCOMM)에 JSON 발행한다.
    /// UI 프로세스의 HostCommSubscriber가 이를 구독하여 SignalR로 브로드캐스트한다.
    /// </summary>
    public class HostCommPublisher : BackgroundService
    {
        private readonly IHostTcpGateway _gateway;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HostCommPublisher> _logger;

        private IConnection _connection;
        private IModel _channel;
        private string _exchangeName;

        // 메시지 본문이 너무 길 경우 잘라내는 임계값. UI 로그창은 진단 용도이므로 8KB면 충분.
        private const int MaxBodyBytes = 8 * 1024;

        public HostCommPublisher(
            IHostTcpGateway gateway,
            IConfiguration configuration,
            ILogger<HostCommPublisher> logger)
        {
            _gateway = gateway;
            _configuration = configuration;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                StartPublisher();
                _gateway.Connected += OnConnected;
                _gateway.Disconnected += OnDisconnected;
                _gateway.MessageReceived += OnMessageReceived;
                _gateway.MessageSent += OnMessageSent;
                _logger.LogInformation("HostCommPublisher started. exchange={Exchange}", _exchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HostCommPublisher start failed.");
            }
            return Task.CompletedTask;
        }

        private void StartPublisher()
        {
            string host = _configuration["Destination:Server:Domain:ConnectUrl"] ?? "localhost";
            string user = _configuration["Destination:Server:Domain:Username"] ?? "guest";
            string pass = _configuration["Destination:Server:Domain:Password"] ?? "guest";
            string domainValue = _configuration["Destination:Server:DomainValue"] ?? "VM/DEMO";
            _exchangeName = NormalizeName($"{domainValue}/UI/HOSTCOMM");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Fanout);
        }

        private void OnConnected(object sender, HostTcpConnectionEventArgs e)
        {
            Publish(new
            {
                type = "Connection",
                connected = true,
                remoteEndPoint = e.RemoteEndPoint,
                eventTime = DateTime.UtcNow
            });
        }

        private void OnDisconnected(object sender, HostTcpConnectionEventArgs e)
        {
            Publish(new
            {
                type = "Connection",
                connected = false,
                remoteEndPoint = e.RemoteEndPoint,
                eventTime = DateTime.UtcNow
            });
        }

        private void OnMessageReceived(object sender, HostTcpMessageEventArgs e)
        {
            Publish(new
            {
                type = "Receive",
                messageName = e.MessageName,
                remoteEndPoint = e.RemoteEndPoint,
                length = e.MessageBody?.Length ?? 0,
                body = Truncate(e.MessageBody),
                eventTime = DateTime.UtcNow
            });
        }

        private void OnMessageSent(object sender, HostTcpMessageSentEventArgs e)
        {
            Publish(new
            {
                type = "Send",
                messageName = e.MessageName,
                remoteEndPoint = e.RemoteEndPoint,
                length = e.MessageBody?.Length ?? 0,
                body = Truncate(e.MessageBody),
                success = e.Success,
                error = e.Error,
                eventTime = DateTime.UtcNow
            });
        }

        private void Publish(object payload)
        {
            try
            {
                if (_channel?.IsOpen != true) return;
                string json = JsonSerializer.Serialize(payload);
                byte[] body = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(exchange: _exchangeName, routingKey: string.Empty, basicProperties: null, body: body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HostCommPublisher: publish failed.");
            }
        }

        private static string Truncate(string body)
        {
            if (string.IsNullOrEmpty(body)) return body;
            if (body.Length <= MaxBodyBytes) return body;
            return body.Substring(0, MaxBodyBytes) + "…";
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _gateway.Connected -= OnConnected;
                _gateway.Disconnected -= OnDisconnected;
                _gateway.MessageReceived -= OnMessageReceived;
                _gateway.MessageSent -= OnMessageSent;
            }
            catch { }

            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }

            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }

        private static string NormalizeName(string name)
        {
            string normalized = (name ?? string.Empty).Replace(".", "/");
            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;
            return normalized;
        }
    }
}
