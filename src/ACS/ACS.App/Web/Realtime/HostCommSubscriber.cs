using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ACS.App.Web.Hubs;

namespace ACS.App.Web.Realtime
{
    /// <summary>
    /// host 프로세스의 HostCommPublisher가 fanout exchange(${domainValue}/UI/HOSTCOMM)로 발행한
    /// JSON 이벤트를 구독하여 SignalR HostCommHub로 브로드캐스트한다.
    ///
    /// 발행되는 SignalR 이벤트 이름:
    ///   - "Connection"  : { connected, remoteEndPoint, eventTime }
    ///   - "Log"         : { direction("Send"|"Receive"), messageName, remoteEndPoint, length, body, eventTime, success?, error? }
    /// </summary>
    public class HostCommSubscriber : BackgroundService
    {
        private readonly IHubContext<HostCommHub> _hub;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HostCommSubscriber> _logger;

        private IConnection _connection;
        private IModel _channel;
        private string _consumerTag;

        public HostCommSubscriber(
            IHubContext<HostCommHub> hub,
            IConfiguration configuration,
            ILogger<HostCommSubscriber> logger)
        {
            _hub = hub;
            _configuration = configuration;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                StartConsumer();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HostCommSubscriber start failed.");
            }
            return Task.CompletedTask;
        }

        private void StartConsumer()
        {
            string host = _configuration["Destination:Server:Domain:ConnectUrl"] ?? "localhost";
            string user = _configuration["Destination:Server:Domain:Username"] ?? "guest";
            string pass = _configuration["Destination:Server:Domain:Password"] ?? "guest";
            string domainValue = _configuration["Destination:Server:DomainValue"] ?? "VM/DEMO";
            string exchangeName = NormalizeName($"{domainValue}/UI/HOSTCOMM");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);

            string queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;
            _consumerTag = _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            _logger.LogInformation("HostCommSubscriber started. exchange={Exchange}, queue={Queue}", exchangeName, queueName);
        }

        private void OnMessageReceived(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                string json = Encoding.UTF8.GetString(args.Body.ToArray());
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() : null;

                if (type == "Connection")
                {
                    var payload = new
                    {
                        connected = root.TryGetProperty("connected", out var c) && c.GetBoolean(),
                        remoteEndPoint = root.TryGetProperty("remoteEndPoint", out var r) ? r.GetString() : "",
                        eventTime = root.TryGetProperty("eventTime", out var e) ? e.GetDateTime() : DateTime.UtcNow
                    };
                    _ = _hub.Clients.All.SendAsync("Connection", payload);
                }
                else if (type == "Receive" || type == "Send")
                {
                    var payload = new
                    {
                        direction = type,
                        messageName = root.TryGetProperty("messageName", out var m) ? m.GetString() : "",
                        remoteEndPoint = root.TryGetProperty("remoteEndPoint", out var r) ? r.GetString() : "",
                        length = root.TryGetProperty("length", out var l) && l.TryGetInt32(out var li) ? li : 0,
                        body = root.TryGetProperty("body", out var b) ? b.GetString() : "",
                        success = root.TryGetProperty("success", out var s) ? (bool?)s.GetBoolean() : null,
                        error = root.TryGetProperty("error", out var er) ? er.GetString() : null,
                        eventTime = root.TryGetProperty("eventTime", out var e) ? e.GetDateTime() : DateTime.UtcNow
                    };
                    _ = _hub.Clients.All.SendAsync("Log", payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HostCommSubscriber: message processing failed.");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_consumerTag != null && _channel?.IsOpen == true)
                    _channel.BasicCancel(_consumerTag);
                _channel?.Close();
                _connection?.Close();
            }
            catch { }
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
