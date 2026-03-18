using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    public async Task<bool> CreateNodeAsync(NodeDto node)
    {
        try
        {
            var json = JsonSerializer.Serialize(node);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/nodes", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateNodeAsync(NodeDto node)
    {
        try
        {
            var json = JsonSerializer.Serialize(node);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/nodes", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteNodeAsync(string nodeId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/nodes/{nodeId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<StationDto>> GetStationsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<StationDto>>("/api/stations")
               ?? new List<StationDto>();
    }

    public async Task<bool> CreateStationAsync(StationDto station)
    {
        try
        {
            var json = JsonSerializer.Serialize(station);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/stations", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateStationAsync(StationDto station)
    {
        try
        {
            var json = JsonSerializer.Serialize(station);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/stations", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteStationAsync(string stationId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/stations/{stationId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateLinkAsync(LinkDto link)
    {
        try
        {
            var json = JsonSerializer.Serialize(link);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/links", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateLinkAsync(LinkDto link)
    {
        try
        {
            var json = JsonSerializer.Serialize(link);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/links", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteLinkAsync(string linkId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/links/{linkId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ZoneDto>> GetZonesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ZoneDto>>("/api/zones")
               ?? new List<ZoneDto>();
    }

    public async Task<bool> CreateZoneAsync(ZoneDto zone)
    {
        try
        {
            var json = JsonSerializer.Serialize(zone);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/zones", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateZoneAsync(ZoneDto zone)
    {
        try
        {
            var json = JsonSerializer.Serialize(zone);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/zones", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteZoneAsync(string zoneId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/zones/{zoneId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<BayDto>> GetBaysAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<BayDto>>("/api/bays")
               ?? new List<BayDto>();
    }

    public async Task<bool> CreateBayAsync(BayDto bay)
    {
        try
        {
            var json = JsonSerializer.Serialize(bay);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/bays", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateBayAsync(BayDto bay)
    {
        try
        {
            var json = JsonSerializer.Serialize(bay);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/bays", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteBayAsync(string bayId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/bays/{bayId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<LinkZoneDto>> GetLinkZonesByLinkIdAsync(string linkId)
    {
        return await _httpClient.GetFromJsonAsync<List<LinkZoneDto>>($"/api/linkzones/{linkId}")
               ?? new List<LinkZoneDto>();
    }

    public async Task<bool> CreateLinkZoneAsync(LinkZoneDto linkZone)
    {
        try
        {
            var json = JsonSerializer.Serialize(linkZone);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/linkzones", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteLinkZoneAsync(string linkZoneId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/linkzones/{linkZoneId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
