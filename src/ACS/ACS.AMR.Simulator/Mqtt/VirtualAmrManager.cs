using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ACS.Communication.Mqtt.Model;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace ACS.AMR.Simulator.Mqtt;

/// <summary>
/// MQTT 가상 차량 매니저 — 공유 IMqttClient 1개로 여러 VirtualAmr 를 운영한다.
/// - {prefix}{commId}/command 구독 → 해당 VirtualAmr 에 디스패치
/// - 100ms 틱(이동 보간) + 1초 주기 status 발행 (status 수신 자체가 EI heartbeat 판정을 겸함)
/// - reply 발행은 VirtualAmr.ReplyRequested 이벤트로 위임받아 수행
/// </summary>
public class VirtualAmrManager : IAsyncDisposable
{
    // AmrStatusMessage 는 [JsonPropertyName] 이 없으므로 camelCase 정책 필수 (EI 수신 스펙)
    private static readonly JsonSerializerOptions StatusJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IMqttClient _client;
    private readonly ConcurrentDictionary<string, VirtualAmr> _amrsByCommId = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, (double X, double Y)> _nodePositions = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource _loopCts;
    private Task _tickTask;
    private Task _statusTask;

    public string BrokerIp { get; set; } = "localhost";
    public int BrokerPort { get; set; } = 1883;
    public string TopicPrefix { get; set; } = "amr/";
    public int StatusIntervalMs { get; set; } = 1000;

    public bool IsConnected => _client?.IsConnected == true;

    public event Action<string> Log;
    /// <summary>연결 상태 변화 (true=연결) — UI 표시용</summary>
    public event Action<bool> ConnectionChanged;

    public VirtualAmrManager()
    {
        _client = new MqttFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += _ =>
        {
            ConnectionChanged?.Invoke(false);
            Log?.Invoke("MQTT 연결 끊김");
            return Task.CompletedTask;
        };
    }

    /// <summary>노드 좌표 캐시 교체 (NA_R_NODE 로드는 VM 책임 — 매니저는 DB 비의존)</summary>
    public void SetNodePositions(Dictionary<string, (double X, double Y)> positions)
    {
        _nodePositions = new Dictionary<string, (double, double)>(positions, StringComparer.OrdinalIgnoreCase);
        Log?.Invoke($"노드 좌표 {_nodePositions.Count}개 로드");
    }

    public async Task ConnectAsync()
    {
        if (IsConnected) return;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerIp, BrokerPort)
            .WithClientId($"AMR_SIM_{Guid.NewGuid():N}")
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithCleanSession(true)
            .Build();

        await _client.ConnectAsync(options, CancellationToken.None);
        ConnectionChanged?.Invoke(true);
        Log?.Invoke($"MQTT 브로커 연결: {BrokerIp}:{BrokerPort}");

        // 이미 등록된 가상 차량들의 command 토픽 재구독 (재연결 대비)
        foreach (var commId in _amrsByCommId.Keys)
            await SubscribeCommandAsync(commId);

        StartLoops();
    }

    public async Task DisconnectAsync()
    {
        StopLoops();
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        ConnectionChanged?.Invoke(false);
        Log?.Invoke("MQTT 연결 해제");
    }

    /// <summary>가상 차량 등록 + command 토픽 구독. 이미 있으면 기존 인스턴스 반환.</summary>
    public async Task<VirtualAmr> AddVirtualAmrAsync(string vehicleId, string commId, string startNodeId)
    {
        if (_amrsByCommId.TryGetValue(commId, out var existing))
            return existing;

        (double x, double y) = _nodePositions.TryGetValue(startNodeId ?? "", out var pos) ? pos : (0, 0);
        var amr = new VirtualAmr(vehicleId, commId, x, y);
        amr.ReplyRequested += (a, reply) => _ = PublishReplyAsync(a, reply);
        amr.Log += (_, msg) => Log?.Invoke(msg);

        _amrsByCommId[commId] = amr;

        if (IsConnected)
            await SubscribeCommandAsync(commId);

        Log?.Invoke($"가상 차량 시작: {vehicleId} (commId={commId}, 시작노드={startNodeId}, pose=({x:F2},{y:F2}))");
        return amr;
    }

    /// <summary>가상 차량 제거 + 구독 해제 (status 발행 중단 → EI 가 30초 후 DISCONNECT 처리)</summary>
    public async Task RemoveVirtualAmrAsync(string commId)
    {
        if (!_amrsByCommId.TryRemove(commId, out var amr)) return;
        if (IsConnected)
            await _client.UnsubscribeAsync($"{TopicPrefix}{commId}/command");
        Log?.Invoke($"가상 차량 정지: {amr.VehicleId} (commId={commId})");
    }

    private async Task SubscribeCommandAsync(string commId)
    {
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"{TopicPrefix}{commId}/command", MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        await _client.SubscribeAsync(options);
        Log?.Invoke($"구독: {TopicPrefix}{commId}/command");
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            string topic = e.ApplicationMessage.Topic ?? "";
            string payload = e.ApplicationMessage.PayloadSegment.Count > 0
                ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                : "";

            // 토픽 형식: {prefix}{commId}/command
            if (!topic.StartsWith(TopicPrefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
            string[] parts = topic.Substring(TopicPrefix.Length).Split('/');
            if (parts.Length != 2 || !parts[1].Equals("command", StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

            string commId = parts[0];
            if (!_amrsByCommId.TryGetValue(commId, out var amr)) return Task.CompletedTask;

            Log?.Invoke($"[{amr.VehicleId}] command 수신: {payload}");
            var cmd = JsonSerializer.Deserialize<AmrCommandMessage>(payload);
            if (cmd == null) return Task.CompletedTask;

            (double X, double Y)? target = _nodePositions.TryGetValue(cmd.NodeId ?? "", out var pos) ? pos : null;
            amr.OnCommand(cmd, target);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"command 처리 오류: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    private async Task PublishReplyAsync(VirtualAmr amr, AmrReplyMessage reply)
    {
        try
        {
            string payload = JsonSerializer.Serialize(reply);
            await PublishAsync($"{TopicPrefix}{amr.CommId}/reply", payload);
            Log?.Invoke($"[{amr.VehicleId}] reply 발행: status={reply.Status} cmdId={reply.CmdId} jobId={reply.JobId} jobType={reply.JobType} resultCode={reply.ResultCode} carrierSlot={reply.CarrierSlot}");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[{amr.VehicleId}] reply 발행 실패: {ex.Message}");
        }
    }

    private async Task PublishAsync(string topic, string payload)
    {
        if (!IsConnected) return;
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        await _client.PublishAsync(message, CancellationToken.None);
    }

    // ── 루프: 100ms 이동 틱 + StatusIntervalMs 주기 status 발행 ──

    private void StartLoops()
    {
        StopLoops();
        _loopCts = new CancellationTokenSource();
        var ct = _loopCts.Token;

        _tickTask = Task.Run(async () =>
        {
            const int tickMs = 100;
            while (!ct.IsCancellationRequested)
            {
                foreach (var amr in _amrsByCommId.Values)
                {
                    try { amr.Tick(tickMs / 1000.0); }
                    catch (Exception ex) { Log?.Invoke($"[{amr.VehicleId}] tick 오류: {ex.Message}"); }
                }
                try { await Task.Delay(tickMs, ct); } catch (TaskCanceledException) { break; }
            }
        }, ct);

        _statusTask = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var amr in _amrsByCommId.Values)
                {
                    try
                    {
                        string payload = JsonSerializer.Serialize(amr.BuildStatus(), StatusJson);
                        await PublishAsync($"{TopicPrefix}{amr.CommId}/status", payload);
                    }
                    catch (Exception ex) { Log?.Invoke($"[{amr.VehicleId}] status 발행 실패: {ex.Message}"); }
                }
                try { await Task.Delay(Math.Max(200, StatusIntervalMs), ct); } catch (TaskCanceledException) { break; }
            }
        }, ct);
    }

    private void StopLoops()
    {
        _loopCts?.Cancel();
        _loopCts = null;
        _tickTask = null;
        _statusTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        StopLoops();
        try { if (_client.IsConnected) await _client.DisconnectAsync(); } catch { }
        _client.Dispose();
    }
}
