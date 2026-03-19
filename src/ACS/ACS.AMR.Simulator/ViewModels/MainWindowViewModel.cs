using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Avalonia.Controls;
using ACS.Core.Resource.Model;
using ACS.Database;
using ACS.AMR.Simulator.Views;

namespace ACS.AMR.Simulator.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    // ── Vehicle 목록 ──
    public ObservableCollection<VehicleExs> Vehicles { get; } = new();

    [ObservableProperty] private VehicleExs _selectedVehicle;

    // ── 상태 옵션 ──
    public string[] ConnectionStates { get; } = { "CONNECT", "DISCONNECT" };
    public string[] ProcessingStates { get; } = { "IDLE", "RUN", "CHARGE", "PARK" };
    public string[] RunStates { get; } = { "RUN", "STOP" };
    public string[] AlarmStates { get; } = { "NOALARM", "ALARM" };
    public string[] TransferStates { get; } = { "NOTASSIGNED", "ASSIGNED", "ASSIGNED_ENROUTE", "ASSIGNED_PARKED", "ASSIGNED_ACQUIRING", "ASSIGNED_DEPOSITING", "ACQUIRE_COMPLETE", "DEPOSIT_COMPLETE" };
    public string[] FullStates { get; } = { "EMPTY", "FULL" };

    // ── 로그 ──
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _statusMessage = "Ready";

    // ── DB ──
    private readonly IConfiguration _configuration;

    public MainWindowViewModel()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true);
        _configuration = builder.Build();

        // DB 스키마 확인 및 마이그레이션
        try
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            RunMigrations(db);
            AppendLog("AMR Simulator started. DB connected.");
        }
        catch (Exception ex)
        {
            AppendLog($"AMR Simulator started. DB warning: {ex.Message}");
        }
    }

    private void RunMigrations(AcsDbContext db)
    {
        // NA_T_TRANSPORTCMD: jobId 컬럼 추가
        db.Database.ExecuteSqlRaw(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_T_TRANSPORTCMD' AND column_name = 'jobId'
    ) THEN
        ALTER TABLE ""NA_T_TRANSPORTCMD"" ADD COLUMN ""jobId"" VARCHAR(64);
        UPDATE ""NA_T_TRANSPORTCMD"" SET ""jobId"" = ""id""::text;
        ALTER TABLE ""NA_T_TRANSPORTCMD"" DROP CONSTRAINT IF EXISTS ""PK_NA_T_TRANSPORTCMD"";
        ALTER TABLE ""NA_T_TRANSPORTCMD"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_T_TRANSPORTCMD"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;
    END IF;
END $$;
");
        // NA_R_VEHICLE: vehicleId 컬럼 추가
        db.Database.ExecuteSqlRaw(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_R_VEHICLE' AND column_name = 'vehicleId'
    ) THEN
        ALTER TABLE ""NA_R_VEHICLE"" ADD COLUMN ""vehicleId"" VARCHAR(64);
        UPDATE ""NA_R_VEHICLE"" SET ""vehicleId"" = ""id""::text;
        ALTER TABLE ""NA_R_VEHICLE"" DROP CONSTRAINT IF EXISTS ""PK_NA_R_VEHICLE"";
        ALTER TABLE ""NA_R_VEHICLE"" DROP COLUMN ""id"";
        ALTER TABLE ""NA_R_VEHICLE"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;
    END IF;
END $$;
");
        // NA_R_LOCATION: portId → locationId + auto-increment id
        db.Database.ExecuteSqlRaw(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'NA_R_LOCATION' AND column_name = 'locationId'
    ) THEN
        ALTER TABLE ""NA_R_LOCATION"" RENAME COLUMN ""portId"" TO ""locationId"";
        ALTER TABLE ""NA_R_LOCATION"" DROP CONSTRAINT IF EXISTS ""PK_NA_R_LOCATION"";
        ALTER TABLE ""NA_R_LOCATION"" ADD COLUMN ""id"" BIGSERIAL PRIMARY KEY;
    END IF;
END $$;
");
    }

    private AcsDbContext CreateDbContext()
    {
        var options = new DbContextOptions<AcsDbContext>();
        return new AcsDbContext(options, _configuration);
    }

    [RelayCommand]
    private void LoadVehicles()
    {
        try
        {
            using var db = CreateDbContext();
            var list = db.Set<VehicleExs>().ToList();

            Vehicles.Clear();
            foreach (var v in list)
                Vehicles.Add(v);

            StatusMessage = $"{Vehicles.Count}대 Vehicle 로드 완료";
            AppendLog($"Loaded {Vehicles.Count} vehicle(s) from DB.");
        }
        catch (Exception ex)
        {
            StatusMessage = "로드 실패";
            AppendLog($"ERROR: {ex.Message}\n  Inner: {ex.InnerException?.Message}");
        }
    }

    [RelayCommand]
    private void SaveSelected()
    {
        if (SelectedVehicle == null)
        {
            StatusMessage = "Vehicle을 선택하세요";
            return;
        }

        try
        {
            using var db = CreateDbContext();
            var entity = db.Set<VehicleExs>().Find(SelectedVehicle.Seq);
            if (entity == null)
            {
                AppendLog($"ERROR: Vehicle Id={SelectedVehicle.Seq} not found in DB.");
                return;
            }

            // 상태 필드 업데이트
            entity.ConnectionState = SelectedVehicle.ConnectionState;
            entity.ProcessingState = SelectedVehicle.ProcessingState;
            entity.RunState = SelectedVehicle.RunState;
            entity.AlarmState = SelectedVehicle.AlarmState;
            entity.TransferState = SelectedVehicle.TransferState;
            entity.FullState = SelectedVehicle.FullState;
            entity.CurrentNodeId = SelectedVehicle.CurrentNodeId;
            entity.BatteryRate = SelectedVehicle.BatteryRate;
            entity.BatteryVoltage = SelectedVehicle.BatteryVoltage;
            entity.TransportCommandId = SelectedVehicle.TransportCommandId;
            entity.EventTime = DateTime.UtcNow;

            db.SaveChanges();

            StatusMessage = $"Vehicle '{entity.VehicleId}' 저장 완료";
            AppendLog($"Saved vehicle '{entity.VehicleId}': Connection={entity.ConnectionState}, Processing={entity.ProcessingState}, Node={entity.CurrentNodeId}, Battery={entity.BatteryRate}%");
        }
        catch (Exception ex)
        {
            StatusMessage = "저장 실패";
            AppendLog($"ERROR: {ex.Message}\n  Inner: {ex.InnerException?.Message}");
        }
    }

    [RelayCommand]
    private async Task AddVehicle()
    {
        var dialog = new AddVehicleWindow();

        // 현재 활성 Window를 owner로 설정
        var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        var result = await dialog.ShowDialog<bool?>(mainWindow);
        if (result != true || dialog.Result == null) return;

        var newVehicle = dialog.Result;
        try
        {
            using var db = CreateDbContext();
            db.Set<VehicleExs>().Add(newVehicle);
            db.SaveChanges();

            Vehicles.Add(newVehicle);
            SelectedVehicle = newVehicle;
            StatusMessage = $"Vehicle '{newVehicle.VehicleId}' 추가 완료";
            AppendLog($"Added vehicle '{newVehicle.VehicleId}' (Node={newVehicle.CurrentNodeId}, Bay={newVehicle.BayId})");
        }
        catch (Exception ex)
        {
            StatusMessage = "추가 실패";
            AppendLog($"ERROR: {ex.Message}\n  Inner: {ex.InnerException?.Message}");
        }
    }

    [RelayCommand]
    private void SaveAll()
    {
        try
        {
            using var db = CreateDbContext();
            int count = 0;
            foreach (var v in Vehicles)
            {
                var entity = db.Set<VehicleExs>().Find(v.Seq);
                if (entity == null) continue;

                entity.ConnectionState = v.ConnectionState;
                entity.ProcessingState = v.ProcessingState;
                entity.RunState = v.RunState;
                entity.AlarmState = v.AlarmState;
                entity.TransferState = v.TransferState;
                entity.FullState = v.FullState;
                entity.CurrentNodeId = v.CurrentNodeId;
                entity.BatteryRate = v.BatteryRate;
                entity.BatteryVoltage = v.BatteryVoltage;
                entity.TransportCommandId = v.TransportCommandId;
                entity.EventTime = DateTime.UtcNow;
                count++;
            }

            db.SaveChanges();
            StatusMessage = $"{count}대 Vehicle 전체 저장 완료";
            AppendLog($"Saved all {count} vehicle(s).");
        }
        catch (Exception ex)
        {
            StatusMessage = "전체 저장 실패";
            AppendLog($"ERROR: {ex.Message}\n  Inner: {ex.InnerException?.Message}");
        }
    }

    private void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
        if (Dispatcher.UIThread.CheckAccess())
            LogText += line;
        else
            Dispatcher.UIThread.Post(() => LogText += line);
    }
}
