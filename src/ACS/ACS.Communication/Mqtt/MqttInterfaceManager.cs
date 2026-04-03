using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Base.Interface;
using ACS.Core.Logging;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Workflow;
using ACS.Communication.Mqtt.Model;
using Autofac;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace ACS.Communication.Mqtt
{
    /// <summary>
    /// MQTT 기반 AMR 통신 매니저.
    /// 브로커 1개 연결로 토픽 구독/발행을 통해 모든 AMR과 통신한다.
    /// </summary>
    public class MqttInterfaceManager : AbstractManager, IControllable
    {
        private new Logger logger = Logger.GetLogger(typeof(MqttInterfaceManager));

        private IMqttClient _mqttClient;
        private MqttConfig _mqttConfig;
        private IWorkflowManager _workflowManager;
        private CancellationTokenSource _reconnectCts;
        private bool _isStarted;
        private Timer _heartbeatCheckTimer;

        // AMR별 연결 상태 추적 (vehicleId → state)
        private ConcurrentDictionary<string, AmrConnectionState> _amrStates = new();

        public ILifetimeScope LifetimeScope { get; set; }
        public IResourceManagerEx ResourceManager { get; set; }

        public MqttConfig MqttConfigData
        {
            get { return _mqttConfig; }
        }

        /// <summary>
        /// DB에서 MqttConfig를 로드하고 MQTT 클라이언트를 초기화한다.
        /// </summary>
        public virtual void Load(string applicationName)
        {
            IList configs = this.PersistentDao.FindByAttribute(typeof(MqttConfig), "ApplicationName", applicationName);
            if (configs == null || configs.Count == 0)
            {
                logger.Info("MQTT 설정이 없습니다. applicationName=" + applicationName);
                return;
            }

            _mqttConfig = (MqttConfig)configs[0];
            logger.Info("MQTT 설정 로드 완료: " + _mqttConfig);

            try
            {
                if (!string.IsNullOrEmpty(_mqttConfig.WorkflowManagerName))
                {
                    _workflowManager = LifetimeScope.ResolveNamed<IWorkflowManager>(_mqttConfig.WorkflowManagerName);
                }
            }
            catch (Exception)
            {
                logger.Info("Named workflow manager를 찾을 수 없어 기본 IWorkflowManager를 사용합니다: " + _mqttConfig.WorkflowManagerName);
            }

            // Named 해결 실패 시 기본(unnamed) IWorkflowManager 사용
            if (_workflowManager == null)
            {
                try
                {
                    _workflowManager = LifetimeScope.Resolve<IWorkflowManager>();
                    logger.Info("기본 IWorkflowManager 해결 완료");
                }
                catch (Exception e)
                {
                    logger.Warn("IWorkflowManager를 찾을 수 없습니다.", e);
                }
            }

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // 연결 끊김 시 자동 재연결
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            // 메시지 수신 핸들러
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            _mqttConfig.State = MqttConfig.STATE_LOADED;
            UpdateMqttConfigState(_mqttConfig);
        }

        #region IControllable

        public bool Start()
        {
            if (_mqttConfig == null)
            {
                logger.Warn("MQTT 설정이 로드되지 않았습니다.");
                return false;
            }

            _reconnectCts = new CancellationTokenSource();
            _isStarted = true;

            // 비동기 연결을 백그라운드에서 실행
            Task.Factory.StartNew(() => ConnectAndSubscribeAsync(_reconnectCts.Token),
                TaskCreationOptions.LongRunning);

            // AMR heartbeat 상태 체크 타이머 시작 (10초 주기)
            _heartbeatCheckTimer = new Timer(CheckAmrHeartbeats, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            return true;
        }

        public bool Stop()
        {
            _isStarted = false;
            _reconnectCts?.Cancel();
            _heartbeatCheckTimer?.Dispose();
            _heartbeatCheckTimer = null;

            try
            {
                if (_mqttClient != null && _mqttClient.IsConnected)
                {
                    _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                logger.Warn("MQTT 연결 종료 중 오류", e);
            }

            _mqttConfig.State = MqttConfig.STATE_CLOSED;
            UpdateMqttConfigState(_mqttConfig);

            logger.Info("MQTT 클라이언트 중지됨");
            return true;
        }

        public bool Open()
        {
            return _mqttClient != null && _mqttClient.IsConnected;
        }

        #endregion

        #region 연결 및 구독

        private async Task ConnectAndSubscribeAsync(CancellationToken ct)
        {
            _mqttConfig.State = MqttConfig.STATE_CONNECTING;
            UpdateMqttConfigState(_mqttConfig);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var optionsBuilder = new MqttClientOptionsBuilder()
                        .WithTcpServer(_mqttConfig.BrokerIp, _mqttConfig.BrokerPort)
                        .WithClientId(_mqttConfig.ClientId ?? $"ACS_{_mqttConfig.ApplicationName}_{Guid.NewGuid():N}")
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttConfig.KeepAliveSeconds))
                        .WithCleanSession(true);

                    if (!string.IsNullOrEmpty(_mqttConfig.UserName))
                    {
                        optionsBuilder.WithCredentials(_mqttConfig.UserName, _mqttConfig.Password);
                    }

                    var options = optionsBuilder.Build();

                    logger.Info($"MQTT 브로커 연결 시도: {_mqttConfig.BrokerIp}:{_mqttConfig.BrokerPort}");
                    await _mqttClient.ConnectAsync(options, ct);

                    if (_mqttClient.IsConnected)
                    {
                        logger.Info("MQTT 브로커 연결 성공");
                        _mqttConfig.State = MqttConfig.STATE_CONNECTED;
                        UpdateMqttConfigState(_mqttConfig);

                        await SubscribeTopicsAsync();

                        if (_workflowManager != null)
                        {
                            _workflowManager.Execute("MQTT-CONNECTED", _mqttConfig);
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    logger.Warn($"MQTT 브로커 연결 실패, {_mqttConfig.ReconnectDelayMs}ms 후 재시도: {e.Message}");
                }

                try
                {
                    await Task.Delay(_mqttConfig.ReconnectDelayMs, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SubscribeTopicsAsync()
        {
            string prefix = _mqttConfig.TopicPrefix ?? "amr/";

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(prefix + "+/status", MqttQualityOfServiceLevel.AtLeastOnce)
                .WithTopicFilter(prefix + "+/heartbeat", MqttQualityOfServiceLevel.AtMostOnce)
                .WithTopicFilter(prefix + "+/alarm", MqttQualityOfServiceLevel.AtLeastOnce)
                .WithTopicFilter(prefix + "+/response", MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);

            logger.Info($"MQTT 토픽 구독 완료: {prefix}+/status, {prefix}+/heartbeat, {prefix}+/alarm, {prefix}+/response");
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            if (!_isStarted) return;

            logger.Warn($"MQTT 브로커 연결 끊김: {e.Reason}");
            _mqttConfig.State = MqttConfig.STATE_DISCONNECTED;
            UpdateMqttConfigState(_mqttConfig);

            if (_workflowManager != null)
            {
                _workflowManager.Execute("MQTT-DISCONNECTED", _mqttConfig);
            }

            // 자동 재연결
            if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested)
            {
                logger.Info("MQTT 자동 재연결 시도...");
                await ConnectAndSubscribeAsync(_reconnectCts.Token);
            }
        }

        #endregion

        #region 메시지 수신

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string prefix = _mqttConfig.TopicPrefix ?? "amr/";

                // 토픽에서 vehicleId와 messageType 추출
                // 예: "amr/AMR01/status" → vehicleId="AMR01", messageType="status"
                string relativeTopic = topic;
                if (topic.StartsWith(prefix))
                {
                    relativeTopic = topic.Substring(prefix.Length);
                }

                string[] segments = relativeTopic.Split('/');
                if (segments.Length < 2)
                {
                    logger.Warn("잘못된 토픽 형식: " + topic);
                    return Task.CompletedTask;
                }

                string vehicleId = segments[0];
                string messageType = segments[1];
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                logger.Debug($"MQTT 수신: topic={topic}, vehicleId={vehicleId}, type={messageType}");

                // AMR 상태 업데이트 (heartbeat 역할도 겸함)
                UpdateAmrState(vehicleId);

                switch (messageType)
                {
                    case "status":
                        HandleStatusMessage(vehicleId, payload);
                        break;
                    case "heartbeat":
                        HandleHeartbeatMessage(vehicleId, payload);
                        break;
                    case "alarm":
                        HandleAlarmMessage(vehicleId, payload);
                        break;
                    case "response":
                        HandleResponseMessage(vehicleId, payload);
                        break;
                    default:
                        logger.Warn("알 수 없는 메시지 타입: " + messageType);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error("MQTT 메시지 처리 중 오류", ex);
            }

            return Task.CompletedTask;
        }

        private void HandleStatusMessage(string vehicleId, string payload)
        {
            try
            {
                var status = JsonSerializer.Deserialize<AmrStatusMessage>(payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new FlexibleStringConverter() }
                    });

                if (status != null)
                {
                    status.VehicleId = vehicleId;
                    logger.Debug($"AMR status parsed: vehicleId={vehicleId}, runState={status.State?.RunState}, " +
                                 $"workState={status.State?.WorkState}, fullState={status.State?.FullState}, " +
                                 $"errorCode={status.Error?.Code}");
                    _workflowManager?.Execute("VEHICLE-STATUS",
                        new object[] { status, vehicleId });
                }
            }
            catch (Exception e)
            {
                logger.Error("AMR 상태 메시지 파싱 오류: vehicleId=" + vehicleId, e);
            }
        }

        private void HandleHeartbeatMessage(string vehicleId, string payload)
        {
            logger.Debug($"AMR heartbeat 수신: vehicleId={vehicleId}");
            _workflowManager?.Execute("VEHICLE-HEARTBEAT",
                new object[] { payload, vehicleId });
        }

        private void HandleAlarmMessage(string vehicleId, string payload)
        {
            logger.Info($"AMR 알람 수신: vehicleId={vehicleId}, payload={payload}");
            _workflowManager?.Execute("AMR-ALARM-RECEIVED",
                new object[] { payload, vehicleId });
        }

        private void HandleResponseMessage(string vehicleId, string payload)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AmrResponseMessage>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response != null)
                {
                    response.VehicleId = vehicleId;
                    _workflowManager?.Execute("AMR-RESPONSE-RECEIVED",
                        new object[] { response, vehicleId });
                }
            }
            catch (Exception e)
            {
                logger.Error("AMR 응답 메시지 파싱 오류: vehicleId=" + vehicleId, e);
            }
        }

        #endregion

        #region 명령 발행

        /// <summary>
        /// AMR에 명령을 전송한다.
        /// </summary>
        public async Task<bool> SendCommand(string vehicleId, AmrCommandMessage command)
        {
            if (!Open())
            {
                logger.Error("MQTT 브로커에 연결되지 않아 명령 전송 불가: vehicleId=" + vehicleId);
                return false;
            }

            try
            {
                string topic = $"{_mqttConfig.TopicPrefix}{vehicleId}/command";
                command.Timestamp = DateTime.Now;
                if (string.IsNullOrEmpty(command.TransactionId))
                {
                    command.TransactionId = Guid.NewGuid().ToString("N");
                }

                string payload = JsonSerializer.Serialize(command);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.PublishAsync(message);

                logger.Info($"MQTT 명령 전송: topic={topic}, command={command.CommandType}, param={command.Parameter}");
                return true;
            }
            catch (Exception e)
            {
                logger.Error("MQTT 명령 전송 오류: vehicleId=" + vehicleId, e);
                return false;
            }
        }

        /// <summary>
        /// AMR에 목적지를 전송한다.
        /// </summary>
        public async Task<bool> SendDestination(string vehicleId, string nodeId)
        {
            var command = new AmrCommandMessage
            {
                CommandType = "MOVE",
                Parameter = nodeId,
                TransactionId = Guid.NewGuid().ToString("N"),
                Timestamp = DateTime.Now
            };

            string topic = $"{_mqttConfig.TopicPrefix}{vehicleId}/destination";
            string payload = JsonSerializer.Serialize(command);

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.PublishAsync(message);

                logger.Info($"MQTT 목적지 전송: vehicleId={vehicleId}, destination={nodeId}");
                return true;
            }
            catch (Exception e)
            {
                logger.Error("MQTT 목적지 전송 오류: vehicleId=" + vehicleId, e);
                return false;
            }
        }

        /// <summary>
        /// AMR에 액션 명령을 전송한다.
        /// </summary>
        public async Task<bool> SendAction(string vehicleId, string actionType, string parameter = null)
        {
            var command = new AmrCommandMessage
            {
                CommandType = actionType,
                Parameter = parameter,
                TransactionId = Guid.NewGuid().ToString("N"),
                Timestamp = DateTime.Now
            };

            return await SendCommand(vehicleId, command);
        }

        #endregion

        #region AMR Heartbeat 모니터링

        /// <summary>
        /// 주기적으로 AMR의 heartbeat 상태를 확인하여 연결 상태 변경 시 워크플로우를 트리거한다.
        /// </summary>
        private void CheckAmrHeartbeats(object state)
        {
            try
            {
                foreach (var kvp in _amrStates)
                {
                    var amrState = kvp.Value;
                    bool isAlive = amrState.IsAlive;

                    if (isAlive && !amrState.IsConnected)
                    {
                        // DISCONNECTED → CONNECTED 전환
                        amrState.IsConnected = true;
                        logger.Info($"AMR 연결 감지: vehicleId={amrState.VehicleId}");

                        if (_workflowManager != null)
                        {
                            _workflowManager.Execute("CONNECTED", new object[] { amrState.VehicleId });
                        }
                    }
                    else if (!isAlive && amrState.IsConnected)
                    {
                        // CONNECTED → DISCONNECTED 전환
                        amrState.IsConnected = false;
                        logger.Info($"AMR 연결 끊김 감지: vehicleId={amrState.VehicleId} (heartbeat timeout)");

                        if (_workflowManager != null)
                        {
                            _workflowManager.Execute("DISCONNECTED", new object[] { amrState.VehicleId });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("AMR heartbeat 상태 체크 중 오류", e);
            }
        }

        #endregion

        #region AMR 상태 관리

        private void UpdateAmrState(string vehicleId)
        {
            _amrStates.AddOrUpdate(vehicleId,
                new AmrConnectionState
                {
                    VehicleId = vehicleId,
                    LastHeartbeat = DateTime.Now,
                    LastStatusUpdate = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.LastHeartbeat = DateTime.Now;
                    existing.LastStatusUpdate = DateTime.Now;
                    return existing;
                });
        }

        /// <summary>
        /// AMR의 연결 상태를 조회한다.
        /// </summary>
        public AmrConnectionState GetAmrState(string vehicleId)
        {
            _amrStates.TryGetValue(vehicleId, out var state);
            return state;
        }

        /// <summary>
        /// AMR이 살아있는지 확인한다 (heartbeat 기반).
        /// </summary>
        public bool IsAmrConnected(string vehicleId)
        {
            return _amrStates.TryGetValue(vehicleId, out var state) && state.IsAlive;
        }

        /// <summary>
        /// 모든 AMR의 연결 상태를 조회한다.
        /// </summary>
        public IDictionary<string, AmrConnectionState> GetAllAmrStates()
        {
            return _amrStates;
        }

        #endregion

        #region DB 상태 관리

        public int UpdateMqttConfigState(MqttConfig config)
        {
            try
            {
                config.EditTime = DateTime.Now;
                return this.PersistentDao.Update(typeof(MqttConfig), "State", config.State, config.Id);
            }
            catch (Exception e)
            {
                logger.Warn("MQTT 설정 상태 업데이트 실패", e);
                return 0;
            }
        }

        public MqttConfig GetMqttConfig(string name)
        {
            return (MqttConfig)this.PersistentDao.FindByName(typeof(MqttConfig), name, false);
        }

        public IList GetMqttConfigs()
        {
            return this.PersistentDao.FindAll(typeof(MqttConfig));
        }

        #endregion
    }

    /// <summary>
    /// AMR별 연결 상태 추적
    /// </summary>
    public class AmrConnectionState
    {
        public string VehicleId { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime LastStatusUpdate { get; set; }

        /// <summary>
        /// 마지막으로 판단한 연결 상태. heartbeat 체크 타이머에서 상태 전환 감지에 사용.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 마지막 heartbeat 이후 30초 이내이면 alive
        /// </summary>
        public bool IsAlive => (DateTime.Now - LastHeartbeat).TotalSeconds < 30;
    }
}
