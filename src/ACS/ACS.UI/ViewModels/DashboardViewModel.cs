using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // System
    [ObservableProperty]
    private int _alarmCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private bool _isAlarmActive;

    // Transfer
    [ObservableProperty]
    private int _transferActiveCount;

    [ObservableProperty]
    private int _transferQueuedCount;

    [ObservableProperty]
    private int _transferCompletedCount;

    // Vehicle
    [ObservableProperty]
    private int _vehicleTotal;

    [ObservableProperty]
    private int _vehicleWorking;

    [ObservableProperty]
    private int _vehicleIdle;

    [ObservableProperty]
    private int _vehicleWarning;

    [ObservableProperty]
    private int _vehicleOnline;

    [ObservableProperty]
    private int _vehicleOffline;

    [ObservableProperty]
    private int _vehicleCharging;

    [ObservableProperty]
    private int _vehicleDown;

    // Layout
    [ObservableProperty]
    private int _linkDisabledCount;

    [ObservableProperty]
    private int _linkTotalCount;

    // Map
    [ObservableProperty]
    private string _mapVersion = "1.0.0";

    [ObservableProperty]
    private string _mapReleaseDate = "-";

    public void UpdateFromVehicles(IReadOnlyList<VehicleDto> vehicles)
    {
        VehicleTotal = vehicles.Count;

        int idle = 0, working = 0, warning = 0, online = 0, offline = 0, charging = 0, down = 0;

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
                case "IDLE":
                    idle++;
                    break;
                case "RUN":
                    working++;
                    break;
                case "CHARGE":
                    charging++;
                    break;
                case "DOWN":
                    down++;
                    warning++;
                    break;
                case "MANUAL":
                    warning++;
                    working++;
                    break;
                default:
                    idle++;
                    break;
            }
        }

        VehicleIdle = idle;
        VehicleWorking = working;
        VehicleWarning = warning;
        VehicleOnline = online;
        VehicleOffline = offline;
        VehicleCharging = charging;
        VehicleDown = down;

        AlarmCount = down;
        WarningCount = warning;
        IsAlarmActive = down > 0;
    }

    public void UpdateFromLinks(IReadOnlyList<LinkDto> links)
    {
        LinkTotalCount = links.Count;
        LinkDisabledCount = links.Count(l => l.Availability == "1" || l.Availability == "2");
    }

    public void UpdateFromCommands(IReadOnlyList<TransportCommandDto> commands)
    {
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

        TransferActiveCount = active;
        TransferQueuedCount = queued;
        TransferCompletedCount = completed;
    }
}
