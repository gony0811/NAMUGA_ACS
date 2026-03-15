using System.Net.Http;
using System.Net.Http.Json;
using ACS.UI.Models;

namespace ACS.UI.Services;

public class AcsApiService : IAcsApiService
{
    private readonly HttpClient _httpClient;

    public AcsApiService(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<List<VehicleDto>> GetVehiclesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles")
               ?? new List<VehicleDto>();
    }

    public async Task<List<NodeDto>> GetNodesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<NodeDto>>("/api/nodes")
               ?? new List<NodeDto>();
    }

    public async Task<List<LinkDto>> GetLinksAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<LinkDto>>("/api/links")
               ?? new List<LinkDto>();
    }

    public async Task<List<TransportCommandDto>> GetTransportCommandsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TransportCommandDto>>("/api/commands")
               ?? new List<TransportCommandDto>();
    }

    public async Task<bool> SendJobReportAsync(string reportType)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/host/job-report",
                new { ReportType = reportType, Timestamp = DateTime.Now });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
