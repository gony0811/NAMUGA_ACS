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
    /// RAIL-VEHICLEUPDATE JSON을 구독하여 POSE(X,Y,Angle)와 상태 필드(배터리/노드/런상태/연결)를
    /// VehicleHub.VehicleUpdate 이벤트로 모든 SignalR 클라이언트에 브로드캐스트한다.
    /// 같은 exchange로 forward되는 RAIL-VEHICLEALARM(SET/RESET 전이)은 messageName으로 구분하여
    /// VehicleHub.VehicleAlarm 이벤트로 브로드캐스트한다 (errorCode/errorMessage 포함).
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

        // 발행 측 vid/cid가 어떤 값으로 나가는지 UI 측 로그와 비교 진단용.
        // 1Hz × N대 텔레메트리이므로 5초 간격으로 throttle.
        private DateTime _lastBroadcastLogAt = DateTime.MinValue;
        private static readonly TimeSpan BroadcastLogInterval = TimeSpan.FromSeconds(5);

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

                // 같은 fanout exchange에 RAIL-VEHICLEALARM도 forward되므로 messageName으로 분기.
                // (알람 메시지를 VehicleUpdate로 오파싱하면 BatteryRate=0 등으로 상태가 오염된다.)
                if (string.Equals(msg.Header?.MessageName, "RAIL-VEHICLEALARM", StringComparison.OrdinalIgnoreCase))
                {
                    HandleVehicleAlarm(json);
                    return;
                }

                var d = msg.Data;

                // POSE뿐 아니라 Trans가 채운 권위 상태 스냅샷(ProcessingState/State/노드/TC/Path 등)을 함께 푸시한다.
                // POSE는 미수신 시 null로 보내며, UI는 null이면 위치를 갱신하지 않는다(상태만 변하는 메시지도 반영).
                var payload = new
                {
                    vehicleId = d.VehicleId,
                    commId = d.CommId,
                    // POSE (nullable)
                    poseX = d.PoseX,
                    poseY = d.PoseY,
                    poseAngle = d.PoseAngle,
                    // 상태 (Trans 권위 스냅샷)
                    runState = d.RunState,
                    processingState = d.ProcessingState,
                    state = d.State,
                    transferState = d.TransferState,
                    batteryRate = d.BatteryRate,
                    batteryVoltage = d.BatteryVoltage,
                    currentNodeId = d.CurrentNodeId,
                    acsDestNodeId = d.AcsDestNodeId,
                    vehicleDestNodeId = d.VehicleDestNodeId,
                    transportCommandId = d.TransportCommandId,
                    path = d.Path,
                    connectionState = d.ConnectionState,
                    eventTime = d.EventTime
                };

                // SignalR 브로드캐스트는 비동기지만 fire-and-forget으로 처리해 RabbitMQ consumer 스레드를 막지 않는다.
                _ = _hub.Clients.All.SendAsync("VehicleUpdate", payload);

                var now = DateTime.UtcNow;
                if (now - _lastBroadcastLogAt >= BroadcastLogInterval)
                {
                    _lastBroadcastLogAt = now;
                    _logger.LogInformation(
                        "VehicleUpdate broadcast vehicleId={VehicleId} commId={CommId} run={Run} proc={Proc} batt={Batt} node={Node} tc={Tc} conn={Conn} pose=({X},{Y},{A})",
                        d.VehicleId, d.CommId, d.RunState, d.ProcessingState, d.BatteryRate, d.CurrentNodeId, d.TransportCommandId, d.ConnectionState, d.PoseX, d.PoseY, d.PoseAngle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PoseTelemetrySubscriber: message processing failed.");
            }
        }

        /// <summary>
        /// Trans가 forward한 RAIL-VEHICLEALARM(SET/RESET 전이)을 SignalR "VehicleAlarm"으로 브로드캐스트.
        /// UI는 이를 받아 맵의 알람 강조 표시와 hover 팝업의 사유(errorCode, errorMessage)를 갱신한다.
        /// </summary>
        private void HandleVehicleAlarm(string json)
        {
            var alarm = JsonSerializer.Deserialize<RailVehicleAlarmMessage>(json);
            if (alarm?.Data == null) return;

            var d = alarm.Data;
            var payload = new
            {
                vehicleId = d.VehicleId,
                commId = d.CommId,
                type = d.Type,
                errorCode = d.ErrorCode,
                errorMessage = d.ErrorMessage,
                eventTime = d.EventTime
            };

            _ = _hub.Clients.All.SendAsync("VehicleAlarm", payload);

            _logger.LogInformation(
                "VehicleAlarm broadcast vehicleId={VehicleId} commId={CommId} type={Type} errorCode={ErrorCode} errorMessage={ErrorMessage}",
                d.VehicleId, d.CommId, d.Type, d.ErrorCode, d.ErrorMessage);
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
