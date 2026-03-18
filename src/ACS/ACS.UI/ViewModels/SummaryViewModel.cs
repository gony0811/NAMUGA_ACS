using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    // Site Info
    [ObservableProperty]
    private string _siteName = "NAMUGA";

    [ObservableProperty]
    private string _serverVersion = "1.0.0";

    [ObservableProperty]
    private string _clientVersion = "1.0.0";

    [ObservableProperty]
    private string _connectionState = "Disconnected";

    // Vehicle Info
    [ObservableProperty]
    private int _vehicleTotal;

    [ObservableProperty]
    private int _vehicleWorking;

    [ObservableProperty]
    private int _vehicleIdle;

    [ObservableProperty]
    private int _vehicleOnline;

    [ObservableProperty]
    private int _vehicleOffline;

    [ObservableProperty]
    private int _vehicleCharging;

    [ObservableProperty]
    private int _vehicleDown;

    // Transfer Info
    [ObservableProperty]
    private int _transferActive;

    [ObservableProperty]
    private int _transferQueued;

    [ObservableProperty]
    private int _transferCompleted;

    [ObservableProperty]
    private int _transferTotal;

    // Link Info
    [ObservableProperty]
    private int _linkTotal;

    [ObservableProperty]
    private int _linkDisabled;

    public void UpdateFromVehicles(IReadOnlyList<VehicleDto> vehicles)
    {
        VehicleTotal = vehicles.Count;

        int working = 0, idle = 0, online = 0, offline = 0, charging = 0, down = 0;

        foreach (var v in vehicles)
        {
            var state = (v.State ?? "").ToUpperInvariant();
            var conn = (v.ConnectionState ?? "").ToUpperInvariant();

            if (conn == "DISCONNECT")
            {
                offline++;
                continue;
            }

            online++;

            switch (state)
            {
                case "RUN":
                case "MANUAL":
                    working++;
                    break;
                case "CHARGE":
                    charging++;
                    break;
                case "DOWN":
                    down++;
                    break;
                default:
                    idle++;
                    break;
            }
        }

        VehicleWorking = working;
        VehicleIdle = idle;
        VehicleOnline = online;
        VehicleOffline = offline;
        VehicleCharging = charging;
        VehicleDown = down;
    }

    public void UpdateFromLinks(IReadOnlyList<LinkDto> links)
    {
        LinkTotal = links.Count;
        LinkDisabled = links.Count(l => l.Availability == "1" || l.Availability == "2");
    }

    public void UpdateFromCommands(IReadOnlyList<TransportCommandDto> commands)
    {
        TransferTotal = commands.Count;
        int active = 0, queued = 0, completed = 0;

        foreach (var c in commands)
        {
            var state = (c.State ?? "").ToUpperInvariant();
            switch (state)
            {
                case "ACTIVE" or "RUNNING" or "TRANSFERRING":
                    active++;
                    break;
                case "QUEUED" or "WAITING":
                    queued++;
                    break;
                case "COMPLETED" or "COMPLETE":
                    completed++;
                    break;
            }
        }

        TransferActive = active;
        TransferQueued = queued;
        TransferCompleted = completed;
    }

    public void UpdateConnectionState(string state)
    {
        ConnectionState = state;
    }
}
