using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ACS.UI.Models;

namespace ACS.UI.Services;

/// <summary>
/// ACS.App UI 프로세스의 SignalR VehicleHub(/hubs/vehicle)에 연결하여
/// 차량 POSE 텔레메트리(1Hz)를 수신한다. 자동 재연결 활성화.
/// </summary>
public class VehicleHubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    /// <summary>
    /// PoseUpdate 수신 시 발생. 핸들러는 SignalR 워커 스레드에서 호출되므로
    /// UI 갱신 시 Avalonia Dispatcher.UIThread.Post로 마샬링해야 한다.
    /// </summary>
    public event Action<PoseUpdateDto> PoseUpdated;

    public VehicleHubClient(string baseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(baseUrl.TrimEnd('/') + "/hubs/vehicle")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<PoseUpdateDto>("PoseUpdate", pose =>
        {
            PoseUpdated?.Invoke(pose);
        });
    }

    public HubConnectionState State => _connection.State;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _connection.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
