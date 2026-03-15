using ACS.UI.Models;

namespace ACS.UI.Services;

public interface IAcsApiService
{
    Task<List<VehicleDto>> GetVehiclesAsync();
    Task<List<NodeDto>> GetNodesAsync();
    Task<List<LinkDto>> GetLinksAsync();
    Task<List<TransportCommandDto>> GetTransportCommandsAsync();
    Task<bool> SendJobReportAsync(string reportType);
}
