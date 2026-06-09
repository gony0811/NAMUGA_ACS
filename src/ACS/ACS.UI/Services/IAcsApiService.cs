using ACS.UI.Models;

namespace ACS.UI.Services;

public interface IAcsApiService
{
    Task<List<VehicleDto>> GetVehiclesAsync();
    Task<bool> ResetVehicleAsync(string vehicleId);
    Task<List<ApplicationDto>> GetApplicationsAsync();
    Task<bool> StartApplicationAsync(string name);
    Task<bool> StopApplicationAsync(string name);
    Task<bool> ForceKillApplicationAsync(string name);
    Task<List<NodeDto>> GetNodesAsync();
    Task<List<LinkDto>> GetLinksAsync();
    Task<List<TransportCommandDto>> GetTransportCommandsAsync();
    Task<bool> DeleteTransportCommandAsync(string jobId);
    Task<bool> ResetTransportCommandAsync(string jobId);
    Task<bool> SendJobReportAsync(string reportType);
    Task<bool> CreateNodeAsync(NodeDto node);
    Task<bool> UpdateNodeAsync(NodeDto node);
    Task<bool> DeleteNodeAsync(string nodeId);
    Task<List<StationDto>> GetStationsAsync();
    Task<bool> CreateStationAsync(StationDto station);
    Task<bool> UpdateStationAsync(StationDto station);
    Task<bool> DeleteStationAsync(string stationId);
    Task<bool> CreateLinkAsync(LinkDto link);
    Task<bool> UpdateLinkAsync(LinkDto link);
    Task<bool> DeleteLinkAsync(string linkId);
    Task<List<ZoneDto>> GetZonesAsync();
    Task<bool> CreateZoneAsync(ZoneDto zone);
    Task<bool> UpdateZoneAsync(ZoneDto zone);
    Task<bool> DeleteZoneAsync(string zoneId);
    Task<List<BayDto>> GetBaysAsync();
    Task<bool> CreateBayAsync(BayDto bay);
    Task<bool> UpdateBayAsync(BayDto bay);
    Task<bool> DeleteBayAsync(string bayId);
    Task<List<LocationDto>> GetLocationsAsync();
    Task<bool> CreateLocationAsync(LocationDto location);
    Task<bool> UpdateLocationAsync(LocationDto location);
    Task<bool> DeleteLocationAsync(string locationId);
    Task<List<LinkZoneDto>> GetLinkZonesAsync();
    Task<List<LinkZoneDto>> GetLinkZonesByLinkIdAsync(string linkId);
    Task<bool> CreateLinkZoneAsync(LinkZoneDto linkZone);
    Task<bool> DeleteLinkZoneAsync(string linkZoneId);
    Task<HeartbeatSettingsDto?> GetHeartbeatSettingsAsync();
    Task<bool> UpdateHeartbeatSettingsAsync(HeartbeatSettingsDto settings);

    // 로그 조회 (NA_L_LOGMESSAGE / NA_L_LARGELOGMESSAGE)
    Task<List<LogMessageDto>> GetLogsAsync(LogQueryFilter filter);
    Task<string> GetLogTextAsync(string id);

    // 히스토리 조회 (NA_T_TRANSPORTCMD_HISTORY / NA_T_VEHICLE_HISTORY)
    Task<List<TransportCommandHistoryDto>> GetTransportCmdHistoriesAsync(TransportCmdHistoryQueryFilter filter);
    Task<List<VehicleHistoryDto>> GetVehicleHistoriesAsync(VehicleHistoryQueryFilter filter);
}
