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
using ACS.Communication.Mqtt.Model;

namespace ACS.App.Web.Realtime
{
    /// <summary>
    /// Trans 프로세스가 UiAgentSender(MULTICAST = fanout exchange)로 발행한
    /// RAIL-VEHICLEUPDATE JSON을 구독하여 POSE(X,Y,Angle)가 포함된 경우
    /// VehicleHub.PoseUpdate 이벤트로 모든 SignalR 클라이언트에 브로드캐스트한다.
    ///
    /// AMR 100대 × 1Hz 텔레메트리를 워크플로우 엔진을 거치지 않고 직접 처리하기 위해
    /// GenericWorkflowRabbitMQListener 대신 RabbitMQ.Client API를 직접 사용한다.
    /// </summary>
    public class PoseTelemetrySubscriber : BackgroundService
    {
        private readonly IHubContext<VehicleHub> _hub;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PoseTelemetrySubscriber> _logger;

        private IConnection _connection;
        private IModel _channel;
        private string _consumerTag;

        public PoseTelemetrySubscriber(
            IHubContext<VehicleHub> hub,
            IConfiguration configuration,
            ILogger<PoseTelemetrySubscriber> logger)
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
                _logger.LogError(ex, "PoseTelemetrySubscriber start failed.");
            }
            return Task.CompletedTask;
        }

        private void StartConsumer()
        {
            string host = _configuration["Destination:Server:Domain:ConnectUrl"] ?? "localhost";
            string user = _configuration["Destination:Server:Domain:Username"] ?? "guest";
            string pass = _configuration["Destination:Server:Domain:Password"] ?? "guest";

            string domainValue = _configuration["Destination:Server:DomainValue"] ?? "VM/DEMO";
            string exchangeName = NormalizeName($"{domainValue}/UI/SENDER");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // UiAgentSender는 fanout exchange에 발행한다 (GenericRabbitMQSender CASTOPTION_MULTICAST).
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);

            // 익명 임시 큐를 생성하고 fanout exchange에 바인딩 — UI 프로세스 인스턴스마다 고유 큐.
            string queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;

            _consumerTag = _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            _logger.LogInformation("PoseTelemetrySubscriber started. exchange={Exchange}, queue={Queue}", exchangeName, queueName);
        }

        private void OnMessageReceived(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                string json = Encoding.UTF8.GetString(args.Body.ToArray());
                var msg = JsonSerializer.Deserialize<RailVehicleUpdateMessage>(json);
                if (msg?.Data == null) return;
                if (msg.Data.PoseX == null || msg.Data.PoseY == null) return;

                var payload = new
                {
                    vehicleId = msg.Data.VehicleId,
                    commId = msg.Data.CommId,
                    x = msg.Data.PoseX.Value,
                    y = msg.Data.PoseY.Value,
                    angle = msg.Data.PoseAngle ?? 0f,
                    eventTime = msg.Data.EventTime
                };

                // SignalR 브로드캐스트는 비동기지만 fire-and-forget으로 처리해 RabbitMQ consumer 스레드를 막지 않는다.
                _ = _hub.Clients.All.SendAsync("PoseUpdate", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PoseTelemetrySubscriber: message processing failed.");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_consumerTag != null && _channel?.IsOpen == true)
                {
                    _channel.BasicCancel(_consumerTag);
                }
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

        /// <summary>
        /// GenericRabbitMQSender.Init과 동일한 정규화: '.' → '/', leading slash 보장.
        /// 동일한 exchange 이름이 publisher/subscriber 간 일치해야 fanout이 동작한다.
        /// </summary>
        private static string NormalizeName(string name)
        {
            string normalized = (name ?? string.Empty).Replace(".", "/");
            if (!normalized.StartsWith("/"))
            {
                normalized = "/" + normalized;
            }
            return normalized;
        }
    }
}
