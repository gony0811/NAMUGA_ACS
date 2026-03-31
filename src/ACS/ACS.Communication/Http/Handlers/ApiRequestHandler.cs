using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Http.Models;
using ACS.Communication.Http.uHttpSharp;
using ACS.Core.Logging;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using Newtonsoft.Json;

namespace ACS.Communication.Http.Handlers
{
    public class ApiRequestHandler : IHttpRequestHandler
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ApiRequestHandler));

        private readonly IResourceManagerEx _resourceManager;
        private readonly ITransferManagerEx _transferManager;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public ApiRequestHandler(IResourceManagerEx resourceManager, ITransferManagerEx transferManager)
        {
            _resourceManager = resourceManager;
            _transferManager = transferManager;
        }

        public Task Handle(IHttpContext context, Func<Task> nextHandler)
        {
            var request = context.Request;

            // Handle CORS preflight
            if (request.Method == HttpMethods.Options)
            {
                context.Response = StringHttpResponse.Create("", HttpResponseCode.Ok, "text/plain");
                return Task.CompletedTask;
            }

            // Route: /api/{resource}
            string resource = request.RequestParameters.Length > 1 ? request.RequestParameters[1] : "";
            // /api/{resource}/{id}
            string resourceId = request.RequestParameters.Length > 2 ? request.RequestParameters[2] : null;

            string json;
            try
            {
                // Node CRUD (POST/PUT/DELETE)
                if (resource.ToLowerInvariant() == "nodes" && request.Method != HttpMethods.Get)
                {
                    json = HandleNodeCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // Link CRUD (POST/PUT/DELETE)
                if (resource.ToLowerInvariant() == "links" && request.Method != HttpMethods.Get)
                {
                    json = HandleLinkCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // Station CRUD (POST/PUT/DELETE)
                if (resource.ToLowerInvariant() == "stations" && request.Method != HttpMethods.Get)
                {
                    json = HandleStationCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // Bay CRUD (POST/PUT/DELETE)
                if (resource.ToLowerInvariant() == "bays" && request.Method != HttpMethods.Get)
                {
                    json = HandleBayCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // Zone CRUD (POST/PUT/DELETE)
                if (resource.ToLowerInvariant() == "zones" && request.Method != HttpMethods.Get)
                {
                    json = HandleZoneCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // Location CRUD (POST/PUT/DELETE)
                if (resource.ToLowerInvariant() == "locations" && request.Method != HttpMethods.Get)
                {
                    json = HandleLocationCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // LinkZone CRUD (POST/DELETE)
                if (resource.ToLowerInvariant() == "linkzones" && request.Method != HttpMethods.Get)
                {
                    json = HandleLinkZoneCrud(request, resourceId);
                    context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
                    return Task.CompletedTask;
                }

                // GET-only for other resources
                if (request.Method != HttpMethods.Get)
                {
                    return nextHandler();
                }

                switch (resource.ToLowerInvariant())
                {
                    case "vehicles":
                        json = GetVehiclesJson();
                        break;
                    case "nodes":
                        json = GetNodesJson();
                        break;
                    case "links":
                        json = GetLinksJson();
                        break;
                    case "stations":
                        json = GetStationsJson();
                        break;
                    case "zones":
                        json = GetZonesJson();
                        break;
                    case "bays":
                        json = GetBaysJson();
                        break;
                    case "locations":
                        json = GetLocationsJson();
                        break;
                    case "linkzones":
                        json = GetLinkZonesJson(resourceId);
                        break;
                    case "commands":
                        json = GetTransportCommandsJson();
                        break;
                    default:
                        json = JsonConvert.SerializeObject(new { error = "Unknown resource: " + resource });
                        context.Response = StringHttpResponse.Create(json, HttpResponseCode.NotFound, "application/json");
                        return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                logger.Error("API error for resource: " + resource, ex);
                json = JsonConvert.SerializeObject(new { error = ex.Message });
                context.Response = StringHttpResponse.Create(json, HttpResponseCode.InternalServerError, "application/json");
                return Task.CompletedTask;
            }

            context.Response = StringHttpResponse.Create(json, HttpResponseCode.Ok, "application/json");
            return Task.CompletedTask;
        }

        private string HandleNodeCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<NodeDto>(request);
                    var node = new NodeEx
                    {
                        NodeId = dto.Id,
                        Type = dto.Type,
                        Xpos = dto.Xpos,
                        Ypos = dto.Ypos,
                        Zpos = dto.Zpos
                    };
                    _resourceManager.CreateNode(node);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Put:
                {
                    var dto = ParseBody<NodeDto>(request);
                    var existing = _resourceManager.GetNode(dto.Id);
                    if (existing == null)
                        throw new Exception("Node not found: " + dto.Id);
                    existing.Type = dto.Type;
                    existing.Xpos = dto.Xpos;
                    existing.Ypos = dto.Ypos;
                    existing.Zpos = dto.Zpos;
                    _resourceManager.UpdateNode(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Node ID is required for delete");

                    // Node 삭제 전: 관련 Link → LinkZone 연쇄 삭제
                    IList links = _resourceManager.GetLinks();
                    if (links != null)
                    {
                        foreach (var item in links)
                        {
                            var link = item as LinkEx;
                            if (link != null && (link.FromNodeId == id || link.ToNodeId == id))
                            {
                                // Link에 속한 LinkZone 먼저 삭제
                                IList linkZones = _resourceManager.GetLinkZonesByLinkId(link.Id);
                                if (linkZones != null)
                                {
                                    foreach (var lzItem in linkZones)
                                    {
                                        var lz = lzItem as LinkZoneEx;
                                        if (lz != null)
                                            _resourceManager.DeleteLinkZone(lz);
                                    }
                                }
                                _resourceManager.DeleteLink(link);
                            }
                        }
                    }

                    _resourceManager.DeleteNode(id);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }

        private string HandleStationCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<StationDto>(request);
                    var station = new StationEx
                    {
                        Id = dto.Id,
                        LinkId = dto.LinkId,
                        Type = dto.Type,
                        Distance = dto.Distance,
                        Direction = dto.Direction
                    };
                    _resourceManager.CreateStation(station);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Put:
                {
                    var dto = ParseBody<StationDto>(request);
                    var existing = _resourceManager.GetStation(dto.Id);
                    if (existing == null)
                        throw new Exception("Station not found: " + dto.Id);
                    existing.LinkId = dto.LinkId;
                    existing.Type = dto.Type;
                    existing.Distance = dto.Distance;
                    existing.Direction = dto.Direction;
                    _resourceManager.UpdateStation(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Station ID is required for delete");
                    _resourceManager.DeleteStation(id);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }

        private string GetStationsJson()
        {
            IList stations = _resourceManager.GetStations();
            var dtos = new List<StationDto>();

            if (stations != null)
            {
                foreach (var item in stations)
                {
                    var s = item as StationEx;
                    if (s == null) continue;
                    dtos.Add(new StationDto
                    {
                        Id = s.Id,
                        LinkId = s.LinkId,
                        Type = s.Type,
                        Distance = s.Distance,
                        Direction = s.Direction
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private T ParseBody<T>(IHttpRequest request)
        {
            if (request.Post?.Raw == null || request.Post.Raw.Length == 0)
                throw new Exception("Request body is empty");
            var bodyStr = Encoding.UTF8.GetString(request.Post.Raw);
            return JsonConvert.DeserializeObject<T>(bodyStr);
        }

        private string GetVehiclesJson()
        {
            IList vehicles = _resourceManager.GetVehicles();
            var dtos = new List<VehicleDto>();

            if (vehicles != null)
            {
                foreach (var item in vehicles)
                {
                    var v = item as VehicleEx;
                    if (v == null) continue;
                    dtos.Add(new VehicleDto
                    {
                        VehicleId = v.VehicleId,
                        State = v.State,
                        ConnectionState = v.ConnectionState,
                        ProcessingState = v.ProcessingState,
                        RunState = v.RunState,
                        AlarmState = v.AlarmState,
                        TransferState = v.TransferState,
                        BatteryRate = v.BatteryRate,
                        BatteryVoltage = v.BatteryVoltage,
                        CurrentNodeId = v.CurrentNodeId,
                        AcsDestNodeId = v.AcsDestNodeId,
                        VehicleDestNodeId = v.VehicleDestNodeId,
                        TransportCommandId = v.TransportCommandId,
                        BayId = v.BayId,
                        CarrierType = v.CarrierType
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string GetNodesJson()
        {
            IList nodes = _resourceManager.GetNodes();
            var dtos = new List<NodeDto>();

            if (nodes != null)
            {
                foreach (var item in nodes)
                {
                    var n = item as NodeEx;
                    if (n == null) continue;
                    dtos.Add(new NodeDto
                    {
                        Id = n.NodeId,
                        Type = n.Type,
                        Xpos = n.Xpos,
                        Ypos = n.Ypos,
                        Zpos = n.Zpos
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string GetLinksJson()
        {
            IList links = _resourceManager.GetLinks();
            var dtos = new List<LinkDto>();

            if (links != null)
            {
                foreach (var item in links)
                {
                    var l = item as LinkEx;
                    if (l == null) continue;
                    dtos.Add(new LinkDto
                    {
                        Id = l.Id,
                        FromNodeId = l.FromNodeId,
                        ToNodeId = l.ToNodeId,
                        Availability = l.Availability,
                        Length = l.Length,
                        Speed = l.Speed,
                        LeftBranch = l.LeftBranch,
                        Load = l.Load
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string HandleLinkCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<LinkDto>(request);
                    var link = new LinkEx
                    {
                        Id = dto.Id,
                        FromNodeId = dto.FromNodeId,
                        ToNodeId = dto.ToNodeId,
                        Availability = dto.Availability,
                        Length = dto.Length,
                        Speed = dto.Speed,
                        LeftBranch = dto.LeftBranch,
                        Load = dto.Load
                    };
                    _resourceManager.CreateLink(link);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Put:
                {
                    var dto = ParseBody<LinkDto>(request);
                    var existing = _resourceManager.GetLink(dto.Id);
                    if (existing == null)
                        throw new Exception("Link not found: " + dto.Id);
                    existing.FromNodeId = dto.FromNodeId;
                    existing.ToNodeId = dto.ToNodeId;
                    existing.Availability = dto.Availability;
                    existing.Length = dto.Length;
                    existing.Speed = dto.Speed;
                    existing.LeftBranch = dto.LeftBranch;
                    existing.Load = dto.Load;
                    _resourceManager.UpdateLink(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Link ID is required for delete");
                    _resourceManager.DeleteLink(id);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }

        private string GetZonesJson()
        {
            IList zones = _resourceManager.GetZones();
            var dtos = new List<ZoneDto>();

            if (zones != null)
            {
                foreach (var item in zones)
                {
                    var z = item as ZoneEx;
                    if (z == null) continue;
                    dtos.Add(new ZoneDto
                    {
                        Id = z.ZoneId,
                        BayId = z.BayId,
                        Description = z.Description
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string HandleBayCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<BayDto>(request);
                    var bay = new BayEx
                    {
                        BayId = dto.Id,
                        Floor = dto.Floor,
                        Description = dto.Description,
                        AgvType = dto.AgvType,
                        ChargeVoltage = dto.ChargeVoltage,
                        LimitVoltage = dto.LimitVoltage,
                        IdleTime = dto.IdleTime,
                        ZoneMove = dto.ZoneMove,
                        Traffic = dto.Traffic,
                        StopOut = dto.StopOut
                    };
                    _resourceManager.CreateBay(bay);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Put:
                {
                    var dto = ParseBody<BayDto>(request);
                    var lookupId = !string.IsNullOrEmpty(dto.OriginalId) ? dto.OriginalId : dto.Id;
                    var existing = _resourceManager.GetBay(lookupId);
                    if (existing == null)
                        throw new Exception("Bay not found: " + lookupId);
                    existing.BayId = dto.Id;
                    existing.Floor = dto.Floor;
                    existing.Description = dto.Description;
                    existing.AgvType = dto.AgvType;
                    existing.ChargeVoltage = dto.ChargeVoltage;
                    existing.LimitVoltage = dto.LimitVoltage;
                    existing.IdleTime = dto.IdleTime;
                    existing.ZoneMove = dto.ZoneMove;
                    existing.Traffic = dto.Traffic;
                    existing.StopOut = dto.StopOut;
                    _resourceManager.UpdateBay(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Bay ID is required for delete");

                    // Bay 삭제 전: 하위 Zone에 LinkZone이 존재하면 삭제 불가
                    IList zones = _resourceManager.GetZones();
                    if (zones != null)
                    {
                        foreach (var item in zones)
                        {
                            var zone = item as ZoneEx;
                            if (zone != null && zone.BayId == id)
                            {
                                IList zoneLinkZones = _resourceManager.GetLinkZonesByZoneId(zone.ZoneId);
                                if (zoneLinkZones != null && zoneLinkZones.Count > 0)
                                    throw new Exception($"Bay '{id}'의 Zone '{zone.ZoneId}'에 연결된 LinkZone이 {zoneLinkZones.Count}개 있어 삭제할 수 없습니다. LinkZone을 먼저 삭제해주세요.");
                            }
                        }

                        // LinkZone이 없으면 Zone 연쇄 삭제
                        foreach (var item in zones)
                        {
                            var zone = item as ZoneEx;
                            if (zone != null && zone.BayId == id)
                            {
                                _resourceManager.DeleteZone(zone);
                            }
                        }
                    }

                    _resourceManager.DeleteBay(id);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }

        private string HandleZoneCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<ZoneDto>(request);
                    var zone = new ZoneEx
                    {
                        ZoneId = dto.Id,
                        BayId = dto.BayId,
                        Description = dto.Description
                    };
                    _resourceManager.CreateZone(zone);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Put:
                {
                    var dto = ParseBody<ZoneDto>(request);
                    var lookupId = !string.IsNullOrEmpty(dto.OriginalId) ? dto.OriginalId : dto.Id;
                    var existing = _resourceManager.GetZone(lookupId);
                    if (existing == null)
                        throw new Exception("Zone not found: " + lookupId);
                    existing.ZoneId = dto.Id;
                    existing.BayId = dto.BayId;
                    existing.Description = dto.Description;
                    _resourceManager.UpdateZone(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Zone ID is required for delete");

                    // Zone 삭제 전: LinkZone이 존재하면 삭제 불가
                    IList existingLinkZones = _resourceManager.GetLinkZonesByZoneId(id);
                    if (existingLinkZones != null && existingLinkZones.Count > 0)
                        throw new Exception($"Zone '{id}'에 연결된 LinkZone이 {existingLinkZones.Count}개 있어 삭제할 수 없습니다. LinkZone을 먼저 삭제해주세요.");

                    _resourceManager.DeleteZone(id);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }

        private string GetBaysJson()
        {
            IList bays = _resourceManager.GetBays();
            var dtos = new List<BayDto>();

            if (bays != null)
            {
                foreach (var item in bays)
                {
                    var b = item as BayEx;
                    if (b == null) continue;
                    dtos.Add(new BayDto
                    {
                        Id = b.BayId,
                        Floor = b.Floor,
                        Description = b.Description,
                        AgvType = b.AgvType,
                        ChargeVoltage = b.ChargeVoltage,
                        LimitVoltage = b.LimitVoltage,
                        IdleTime = b.IdleTime,
                        ZoneMove = b.ZoneMove,
                        Traffic = b.Traffic,
                        StopOut = b.StopOut
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string GetLinkZonesJson(string linkId)
        {
            var dtos = new List<LinkZoneDto>();

            // linkId가 있으면 특정 Link의 LinkZone, 없으면 전체 LinkZone 목록
            IList linkZones = string.IsNullOrEmpty(linkId)
                ? _resourceManager.GetLinkZones()
                : _resourceManager.GetLinkZonesByLinkId(linkId);

            if (linkZones != null)
            {
                foreach (var item in linkZones)
                {
                    var lz = item as LinkZoneEx;
                    if (lz == null) continue;
                    dtos.Add(new LinkZoneDto
                    {
                        Id = lz.Id,
                        LinkId = lz.LinkId,
                        ZoneId = lz.ZoneId,
                        TransferFlag = lz.TransferFlag
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string HandleLinkZoneCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<LinkZoneDto>(request);
                    var linkZone = new LinkZoneEx
                    {
                        Id = dto.Id,
                        LinkId = dto.LinkId,
                        ZoneId = dto.ZoneId,
                        TransferFlag = dto.TransferFlag
                    };
                    _resourceManager.CreateLinkZone(linkZone);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("LinkZone ID is required for delete");
                    _resourceManager.DeleteLinkZone(id);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }

        private string GetTransportCommandsJson()
        {
            IList commands = _transferManager.GetTransportCommands();
            var dtos = new List<TransportCommandDto>();

            if (commands != null)
            {
                foreach (var item in commands)
                {
                    var c = item as TransportCommandEx;
                    if (c == null) continue;
                    dtos.Add(new TransportCommandDto
                    {
                        Id = c.Id,
                        JobId = c.JobId,
                        Priority = c.Priority,
                        State = c.State,
                        VehicleId = c.VehicleId,
                        CarrierId = c.CarrierId,
                        Source = c.Source,
                        Dest = c.Dest,
                        Path = c.Path,
                        CreateTime = c.CreateTime,
                        AssignedTime = c.AssignedTime,
                        CompletedTime = c.CompletedTime,
                        BayId = c.BayId,
                        JobType = c.JobType
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string GetLocationsJson()
        {
            IList locations = _resourceManager.GetLocations();
            var dtos = new List<LocationDto>();

            if (locations != null)
            {
                foreach (var item in locations)
                {
                    var loc = item as LocationEx;
                    if (loc == null) continue;
                    dtos.Add(new LocationDto
                    {
                        LocationId = loc.LocationId,
                        StationId = loc.StationId,
                        Type = loc.Type,
                        CarrierType = loc.CarrierType,
                        State = loc.State,
                        Direction = loc.Direction
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
        }

        private string HandleLocationCrud(IHttpRequest request, string resourceId)
        {
            switch (request.Method)
            {
                case HttpMethods.Post:
                {
                    var dto = ParseBody<LocationDto>(request);
                    var location = new LocationEx
                    {
                        LocationId = dto.LocationId,
                        StationId = dto.StationId,
                        Type = dto.Type,
                        CarrierType = dto.CarrierType,
                        State = dto.State,
                        Direction = dto.Direction
                    };
                    _resourceManager.CreateLocation(location);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Put:
                {
                    var dto = ParseBody<LocationDto>(request);
                    var lookupId = !string.IsNullOrEmpty(dto.OriginalLocationId) ? dto.OriginalLocationId : dto.LocationId;
                    var existing = _resourceManager.GetLocationByLocationId(lookupId);
                    if (existing == null)
                        throw new Exception("Location not found: " + lookupId);
                    existing.LocationId = dto.LocationId;
                    existing.StationId = dto.StationId;
                    existing.Type = dto.Type;
                    existing.CarrierType = dto.CarrierType;
                    existing.State = dto.State;
                    existing.Direction = dto.Direction;
                    _resourceManager.UpdateLocation(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                case HttpMethods.Delete:
                {
                    var id = resourceId;
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Location ID is required for delete");
                    var existing = _resourceManager.GetLocationByLocationId(id);
                    if (existing != null)
                        _resourceManager.DeleteLocation(existing);
                    return JsonConvert.SerializeObject(new { success = true }, JsonSettings);
                }
                default:
                    throw new Exception("Unsupported method: " + request.Method);
            }
        }
    }
}
