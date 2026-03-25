using ACS.UI.Models;

namespace ACS.UI.Services;

public interface IAcsApiService
{
    Task<List<VehicleDto>> GetVehiclesAsync();
    Task<List<NodeDto>> GetNodesAsync();
    Task<List<LinkDto>> GetLinksAsync();
    Task<List<TransportCommandDto>> GetTransportCommandsAsync();
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
}
