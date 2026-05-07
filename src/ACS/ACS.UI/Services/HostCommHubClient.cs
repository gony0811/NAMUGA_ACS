using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ACS.UI.Models;

namespace ACS.UI.Services;

/// <summary>
/// ACS.App UI 프로세스의 SignalR HostCommHub(/hubs/hostcomm)에 연결하여
/// Host(MES) TCP 통신 로그(수신/송신/연결상태)를 실시간 수신한다. 자동 재연결 활성화.
/// </summary>
public class HostCommHubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    /// <summary>Host로부터 메시지 수신/송신 시 발생.</summary>
    public event Action<HostCommLogDto> LogReceived;

    /// <summary>Host TCP 연결/단절 시 발생.</summary>
    public event Action<HostCommConnectionDto> ConnectionChanged;

    public HostCommHubClient(string baseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(baseUrl.TrimEnd('/') + "/hubs/hostcomm")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<HostCommLogDto>("Log", log => LogReceived?.Invoke(log));
        _connection.On<HostCommConnectionDto>("Connection", conn => ConnectionChanged?.Invoke(conn));
    }

    public HubConnectionState State => _connection.State;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State == HubConnectionState.Disconnected)
            await _connection.StartAsync(cancellationToken);
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
