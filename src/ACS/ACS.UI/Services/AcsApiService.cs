using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ACS.UI.Models;

namespace ACS.UI.Services;

public class AcsApiService : IAcsApiService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        return await _httpClient.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles", _jsonOptions)
               ?? new List<VehicleDto>();
    }

    public async Task<List<ApplicationDto>> GetApplicationsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ApplicationDto>>("/api/applications", _jsonOptions)
               ?? new List<ApplicationDto>();
    }

    public async Task<bool> StartApplicationAsync(string name)
        => await PostNoBodyAsync($"/api/applications/{name}/start");

    public async Task<bool> StopApplicationAsync(string name)
        => await PostNoBodyAsync($"/api/applications/{name}/stop");

    public async Task<bool> ForceKillApplicationAsync(string name)
        => await PostNoBodyAsync($"/api/applications/{name}/force-kill");

    private async Task<bool> PostNoBodyAsync(string url)
    {
        try
        {
            var response = await _httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HeartbeatSettingsDto?> GetHeartbeatSettingsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<HeartbeatSettingsDto>("/api/heartbeat-settings", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateHeartbeatSettingsAsync(HeartbeatSettingsDto settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/heartbeat-settings", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<NodeDto>> GetNodesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<NodeDto>>("/api/nodes", _jsonOptions)
               ?? new List<NodeDto>();
    }

    public async Task<List<LinkDto>> GetLinksAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<LinkDto>>("/api/links", _jsonOptions)
               ?? new List<LinkDto>();
    }

    public async Task<List<TransportCommandDto>> GetTransportCommandsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TransportCommandDto>>("/api/commands", _jsonOptions)
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
        return await _httpClient.GetFromJsonAsync<List<StationDto>>("/api/stations", _jsonOptions)
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
        return await _httpClient.GetFromJsonAsync<List<ZoneDto>>("/api/zones", _jsonOptions)
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
        return await _httpClient.GetFromJsonAsync<List<BayDto>>("/api/bays", _jsonOptions)
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

    public async Task<List<LocationDto>> GetLocationsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<LocationDto>>("/api/locations", _jsonOptions)
               ?? new List<LocationDto>();
    }

    public async Task<bool> CreateLocationAsync(LocationDto location)
    {
        try
        {
            var json = JsonSerializer.Serialize(location);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/locations", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateLocationAsync(LocationDto location)
    {
        try
        {
            var json = JsonSerializer.Serialize(location);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/locations", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteLocationAsync(string locationId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/locations/{locationId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<LinkZoneDto>> GetLinkZonesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<LinkZoneDto>>("/api/linkzones", _jsonOptions)
               ?? new List<LinkZoneDto>();
    }

    public async Task<List<LinkZoneDto>> GetLinkZonesByLinkIdAsync(string linkId)
    {
        return await _httpClient.GetFromJsonAsync<List<LinkZoneDto>>($"/api/linkzones/{linkId}", _jsonOptions)
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

    public async Task<List<LogMessageDto>> GetLogsAsync(LogQueryFilter filter)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<LogMessageDto>>("/api/logs" + BuildLogQuery(filter), _jsonOptions)
                   ?? new List<LogMessageDto>();
        }
        catch
        {
            return new List<LogMessageDto>();
        }
    }

    public async Task<string> GetLogTextAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return string.Empty;
        try
        {
            return await _httpClient.GetStringAsync($"/api/logs/{Uri.EscapeDataString(id)}/text");
        }
        catch
        {
            return string.Empty;
        }
    }

    // 필터를 쿼리스트링으로 변환. From/To(로컬)는 UTC ISO-8601("o")로 변환해 전송한다.
    private static string BuildLogQuery(LogQueryFilter f)
    {
        if (f == null) return string.Empty;
        var parts = new List<string>();
        if (f.FromLocal.HasValue)
            parts.Add("from=" + Uri.EscapeDataString(f.FromLocal.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)));
        if (f.ToLocal.HasValue)
            parts.Add("to=" + Uri.EscapeDataString(f.ToLocal.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)));
        if (!string.IsNullOrWhiteSpace(f.Level) && f.Level != "All")
            parts.Add("level=" + Uri.EscapeDataString(f.Level));
        if (!string.IsNullOrWhiteSpace(f.Keyword))
            parts.Add("keyword=" + Uri.EscapeDataString(f.Keyword));
        if (!string.IsNullOrWhiteSpace(f.ProcessName))
            parts.Add("process=" + Uri.EscapeDataString(f.ProcessName));
        if (!string.IsNullOrWhiteSpace(f.MessageName))
            parts.Add("messageName=" + Uri.EscapeDataString(f.MessageName));
        if (!string.IsNullOrWhiteSpace(f.TransactionId))
            parts.Add("transactionId=" + Uri.EscapeDataString(f.TransactionId));
        parts.Add("limit=" + f.Limit.ToString(CultureInfo.InvariantCulture));
        return "?" + string.Join("&", parts);
    }
}
