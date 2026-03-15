using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAcsApiService _apiService;
    private CancellationTokenSource _cts;

    [ObservableProperty]
    private MapViewModel _mapViewModel;

    [ObservableProperty]
    private VehicleListViewModel _vehicleListViewModel;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _lastUpdateTime = "-";

    public MainWindowViewModel(IAcsApiService apiService)
    {
        _apiService = apiService;
        _mapViewModel = new MapViewModel();
        _vehicleListViewModel = new VehicleListViewModel();
    }

    public async Task StartPollingAsync()
    {
        _cts = new CancellationTokenSource();

        // Load static data (nodes, links) once
        await LoadStaticDataAsync();

        // Start periodic polling for dynamic data
        _ = PollDynamicDataAsync(_cts.Token);
    }

    public void StopPolling()
    {
        _cts?.Cancel();
    }

    private async Task LoadStaticDataAsync()
    {
        try
        {
            var nodes = await _apiService.GetNodesAsync();
            var links = await _apiService.GetLinksAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MapViewModel.UpdateNodes(nodes);
                MapViewModel.UpdateLinks(links);
            });

            ConnectionStatus = "Connected";
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Error: " + ex.Message;
        }
    }

    private async Task PollDynamicDataAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);

                var vehicles = await _apiService.GetVehiclesAsync();
                var commands = await _apiService.GetTransportCommandsAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VehicleListViewModel.UpdateVehicles(vehicles);
                    MapViewModel.UpdateVehicles(vehicles);
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                    ConnectionStatus = "Connected";
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ConnectionStatus = "Error: " + ex.Message;
                });
            }
        }
    }
}
