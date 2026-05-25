using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ACS.Communication.Http.Models;
using ACS.Core.Logging.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Control;
using AppModel = ACS.Core.Application.Model;

namespace ACS.App.Web.Controllers
{
    /// <summary>
    /// REST 엔드포인트 모음.
    /// 기존 ACS.Communication.Http.Handlers.ApiRequestHandler의 라우팅·로직을 ASP.NET Core 컨트롤러로 1:1 이전한다.
    /// 클라이언트(ACS.UI/AcsApiService)와 호환되는 JSON 페이로드를 보장하기 위해 DTO 형태와 경로를 그대로 유지.
    /// </summary>
    [ApiController]
    [Route("api/vehicles")]
    public class VehiclesController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public VehiclesController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<VehicleDto>> Get()
        {
            var dtos = new List<VehicleDto>();
            IList vehicles = _resourceManager.GetVehicles();
            if (vehicles != null)
            {
                foreach (var item in vehicles)
                {
                    if (item is not VehicleEx v) continue;
                    dtos.Add(new VehicleDto
                    {
                        VehicleId = v.VehicleId,
                        CommId = v.CommId,
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
            return dtos;
        }
    }

    [ApiController]
    [Route("api/nodes")]
    public class NodesController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public NodesController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<NodeDto>> Get()
        {
            var dtos = new List<NodeDto>();
            IList nodes = _resourceManager.GetNodes();
            if (nodes != null)
            {
                foreach (var item in nodes)
                {
                    if (item is not NodeEx n) continue;
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
            return dtos;
        }

        [HttpPost]
        public ActionResult Create([FromBody] NodeDto dto)
        {
            var node = new NodeEx
            {
                NodeId = dto.Id,
                Type = dto.Type,
                Xpos = dto.Xpos,
                Ypos = dto.Ypos,
                Zpos = dto.Zpos
            };
            _resourceManager.CreateNode(node);
            return Ok(new { success = true });
        }

        [HttpPut]
        public ActionResult Update([FromBody] NodeDto dto)
        {
            var existing = _resourceManager.GetNode(dto.Id);
            if (existing == null)
                return BadRequest(new { error = "Node not found: " + dto.Id });
            existing.Type = dto.Type;
            existing.Xpos = dto.Xpos;
            existing.Ypos = dto.Ypos;
            existing.Zpos = dto.Zpos;
            _resourceManager.UpdateNode(existing);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            // Node 삭제 전: 관련 Link → LinkZone 연쇄 삭제 (기존 ApiRequestHandler.HandleNodeCrud DELETE와 동일)
            IList links = _resourceManager.GetLinks();
            if (links != null)
            {
                foreach (var item in links)
                {
                    if (item is not LinkEx link) continue;
                    if (link.FromNodeId != id && link.ToNodeId != id) continue;

                    IList linkZones = _resourceManager.GetLinkZonesByLinkId(link.Id);
                    if (linkZones != null)
                    {
                        foreach (var lzItem in linkZones)
                        {
                            if (lzItem is LinkZoneEx lz)
                                _resourceManager.DeleteLinkZone(lz);
                        }
                    }
                    _resourceManager.DeleteLink(link);
                }
            }

            _resourceManager.DeleteNode(id);
            return Ok(new { success = true });
        }
    }

    [ApiController]
    [Route("api/links")]
    public class LinksController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public LinksController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<LinkDto>> Get()
        {
            var dtos = new List<LinkDto>();
            IList links = _resourceManager.GetLinks();
            if (links != null)
            {
                foreach (var item in links)
                {
                    if (item is not LinkEx l) continue;
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
            return dtos;
        }

        [HttpPost]
        public ActionResult Create([FromBody] LinkDto dto)
        {
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
            return Ok(new { success = true });
        }

        [HttpPut]
        public ActionResult Update([FromBody] LinkDto dto)
        {
            var existing = _resourceManager.GetLink(dto.Id);
            if (existing == null)
                return BadRequest(new { error = "Link not found: " + dto.Id });
            existing.FromNodeId = dto.FromNodeId;
            existing.ToNodeId = dto.ToNodeId;
            existing.Availability = dto.Availability;
            existing.Length = dto.Length;
            existing.Speed = dto.Speed;
            existing.LeftBranch = dto.LeftBranch;
            existing.Load = dto.Load;
            _resourceManager.UpdateLink(existing);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            _resourceManager.DeleteLink(id);
            return Ok(new { success = true });
        }
    }

    [ApiController]
    [Route("api/stations")]
    public class StationsController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public StationsController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<StationDto>> Get()
        {
            var dtos = new List<StationDto>();
            IList stations = _resourceManager.GetStations();
            if (stations != null)
            {
                foreach (var item in stations)
                {
                    if (item is not StationEx s) continue;
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
            return dtos;
        }

        [HttpPost]
        public ActionResult Create([FromBody] StationDto dto)
        {
            var station = new StationEx
            {
                Id = dto.Id,
                LinkId = dto.LinkId,
                Type = dto.Type,
                Distance = dto.Distance,
                Direction = dto.Direction
            };
            _resourceManager.CreateStation(station);
            return Ok(new { success = true });
        }

        [HttpPut]
        public ActionResult Update([FromBody] StationDto dto)
        {
            var existing = _resourceManager.GetStation(dto.Id);
            if (existing == null)
                return BadRequest(new { error = "Station not found: " + dto.Id });
            existing.LinkId = dto.LinkId;
            existing.Type = dto.Type;
            existing.Distance = dto.Distance;
            existing.Direction = dto.Direction;
            _resourceManager.UpdateStation(existing);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            _resourceManager.DeleteStation(id);
            return Ok(new { success = true });
        }
    }

    [ApiController]
    [Route("api/bays")]
    public class BaysController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public BaysController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<BayDto>> Get()
        {
            var dtos = new List<BayDto>();
            IList bays = _resourceManager.GetBays();
            if (bays != null)
            {
                foreach (var item in bays)
                {
                    if (item is not BayEx b) continue;
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
            return dtos;
        }

        [HttpPost]
        public ActionResult Create([FromBody] BayDto dto)
        {
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
            return Ok(new { success = true });
        }

        [HttpPut]
        public ActionResult Update([FromBody] BayDto dto)
        {
            var lookupId = !string.IsNullOrEmpty(dto.OriginalId) ? dto.OriginalId : dto.Id;
            var existing = _resourceManager.GetBay(lookupId);
            if (existing == null)
                return BadRequest(new { error = "Bay not found: " + lookupId });
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
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            // Bay 삭제 전: 하위 Zone에 LinkZone이 존재하면 삭제 불가 (기존 동작 유지)
            IList zones = _resourceManager.GetZones();
            if (zones != null)
            {
                foreach (var item in zones)
                {
                    if (item is not ZoneEx zone || zone.BayId != id) continue;
                    IList zoneLinkZones = _resourceManager.GetLinkZonesByZoneId(zone.ZoneId);
                    if (zoneLinkZones != null && zoneLinkZones.Count > 0)
                        return BadRequest(new { error = $"Bay '{id}'의 Zone '{zone.ZoneId}'에 연결된 LinkZone이 {zoneLinkZones.Count}개 있어 삭제할 수 없습니다. LinkZone을 먼저 삭제해주세요." });
                }

                foreach (var item in zones)
                {
                    if (item is ZoneEx zone && zone.BayId == id)
                        _resourceManager.DeleteZone(zone);
                }
            }

            _resourceManager.DeleteBay(id);
            return Ok(new { success = true });
        }
    }

    [ApiController]
    [Route("api/zones")]
    public class ZonesController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public ZonesController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<ZoneDto>> Get()
        {
            var dtos = new List<ZoneDto>();
            IList zones = _resourceManager.GetZones();
            if (zones != null)
            {
                foreach (var item in zones)
                {
                    if (item is not ZoneEx z) continue;
                    dtos.Add(new ZoneDto
                    {
                        Id = z.ZoneId,
                        BayId = z.BayId,
                        Description = z.Description
                    });
                }
            }
            return dtos;
        }

        [HttpPost]
        public ActionResult Create([FromBody] ZoneDto dto)
        {
            var zone = new ZoneEx
            {
                ZoneId = dto.Id,
                BayId = dto.BayId,
                Description = dto.Description
            };
            _resourceManager.CreateZone(zone);
            return Ok(new { success = true });
        }

        [HttpPut]
        public ActionResult Update([FromBody] ZoneDto dto)
        {
            var lookupId = !string.IsNullOrEmpty(dto.OriginalId) ? dto.OriginalId : dto.Id;
            var existing = _resourceManager.GetZone(lookupId);
            if (existing == null)
                return BadRequest(new { error = "Zone not found: " + lookupId });
            existing.ZoneId = dto.Id;
            existing.BayId = dto.BayId;
            existing.Description = dto.Description;
            _resourceManager.UpdateZone(existing);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            IList existingLinkZones = _resourceManager.GetLinkZonesByZoneId(id);
            if (existingLinkZones != null && existingLinkZones.Count > 0)
                return BadRequest(new { error = $"Zone '{id}'에 연결된 LinkZone이 {existingLinkZones.Count}개 있어 삭제할 수 없습니다. LinkZone을 먼저 삭제해주세요." });

            _resourceManager.DeleteZone(id);
            return Ok(new { success = true });
        }
    }

    [ApiController]
    [Route("api/locations")]
    public class LocationsController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public LocationsController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<LocationDto>> Get()
        {
            var dtos = new List<LocationDto>();
            IList locations = _resourceManager.GetLocations();
            if (locations != null)
            {
                foreach (var item in locations)
                {
                    if (item is not LocationEx loc) continue;
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
            return dtos;
        }

        [HttpPost]
        public ActionResult Create([FromBody] LocationDto dto)
        {
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
            return Ok(new { success = true });
        }

        [HttpPut]
        public ActionResult Update([FromBody] LocationDto dto)
        {
            var lookupId = !string.IsNullOrEmpty(dto.OriginalLocationId) ? dto.OriginalLocationId : dto.LocationId;
            var existing = _resourceManager.GetLocationByLocationId(lookupId);
            if (existing == null)
                return BadRequest(new { error = "Location not found: " + lookupId });
            existing.LocationId = dto.LocationId;
            existing.StationId = dto.StationId;
            existing.Type = dto.Type;
            existing.CarrierType = dto.CarrierType;
            existing.State = dto.State;
            existing.Direction = dto.Direction;
            _resourceManager.UpdateLocation(existing);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            var existing = _resourceManager.GetLocationByLocationId(id);
            if (existing != null)
                _resourceManager.DeleteLocation(existing);
            return Ok(new { success = true });
        }
    }

    [ApiController]
    [Route("api/linkzones")]
    public class LinkZonesController : ControllerBase
    {
        private readonly IResourceManagerEx _resourceManager;

        public LinkZonesController(IResourceManagerEx resourceManager)
        {
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public ActionResult<List<LinkZoneDto>> GetAll()
        {
            return ToDtos(_resourceManager.GetLinkZones());
        }

        [HttpGet("{linkId}")]
        public ActionResult<List<LinkZoneDto>> GetByLink(string linkId)
        {
            return ToDtos(_resourceManager.GetLinkZonesByLinkId(linkId));
        }

        [HttpPost]
        public ActionResult Create([FromBody] LinkZoneDto dto)
        {
            var linkZone = new LinkZoneEx
            {
                Id = dto.Id,
                LinkId = dto.LinkId,
                ZoneId = dto.ZoneId,
                TransferFlag = dto.TransferFlag
            };
            _resourceManager.CreateLinkZone(linkZone);
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            _resourceManager.DeleteLinkZone(id);
            return Ok(new { success = true });
        }

        private static List<LinkZoneDto> ToDtos(IList linkZones)
        {
            var dtos = new List<LinkZoneDto>();
            if (linkZones != null)
            {
                foreach (var item in linkZones)
                {
                    if (item is not LinkZoneEx lz) continue;
                    dtos.Add(new LinkZoneDto
                    {
                        Id = lz.Id,
                        LinkId = lz.LinkId,
                        ZoneId = lz.ZoneId,
                        TransferFlag = lz.TransferFlag
                    });
                }
            }
            return dtos;
        }
    }

    [ApiController]
    [Route("api/commands")]
    public class CommandsController : ControllerBase
    {
        private readonly ITransferManagerEx _transferManager;

        public CommandsController(ITransferManagerEx transferManager)
        {
            _transferManager = transferManager;
        }

        [HttpGet]
        public ActionResult<List<TransportCommandDto>> Get()
        {
            var dtos = new List<TransportCommandDto>();
            IList commands = _transferManager.GetTransportCommands();
            if (commands != null)
            {
                foreach (var item in commands)
                {
                    if (item is not TransportCommandEx c) continue;
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
            return dtos;
        }
    }

    /// <summary>
    /// 애플리케이션(서버 프로세스) 조회 및 제어.
    /// control 프로세스(CS01_P)에서만 동작 — IControlServerManager가 같은 하드웨어의 프로세스를
    /// 기동/종료하며, 목록은 NA_X_APPLICATION(ApplicationManager)을 조회한다.
    /// </summary>
    [ApiController]
    [Route("api/applications")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IControlServerManager _control;

        public ApplicationsController(IControlServerManager control)
        {
            _control = control;
        }

        // GET /api/applications — NA_X_APPLICATION 전체 목록
        [HttpGet]
        public ActionResult<List<ApplicationDto>> Get()
        {
            var dtos = new List<ApplicationDto>();
            IList applications = _control.ApplicationManager.GetApplications();
            if (applications != null)
            {
                foreach (var item in applications)
                {
                    if (item is not AppModel.Application a) continue;
                    dtos.Add(new ApplicationDto
                    {
                        Name = a.Name,
                        Type = a.Type,
                        State = a.State,
                        RunningHardware = a.RunningHardware,
                        StartTime = a.StartTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                        CheckTime = a.CheckTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = a.Description
                    });
                }
            }
            return dtos;
        }

        // POST /api/applications/{name}/start — inactive 프로세스 실행
        [HttpPost("{name}/start")]
        public ActionResult Start(string name)
        {
            var app = _control.ApplicationManager.GetApplication(name);
            if (app == null)
                return NotFound(new { error = "Application not found: " + name });
            bool success = _control.Start(name, app.Type);
            return Ok(new { success });
        }

        // POST /api/applications/{name}/stop — active 프로세스 정지 (taskkill /F)
        [HttpPost("{name}/stop")]
        public ActionResult Stop(string name)
        {
            var app = _control.ApplicationManager.GetApplication(name);
            if (app == null)
                return NotFound(new { error = "Application not found: " + name });
            bool success = _control.Kill(name, app.Type);
            return Ok(new { success });
        }

        // POST /api/applications/{name}/force-kill — hang 프로세스 강제종료 (COREDUMP 수집 후 종료)
        [HttpPost("{name}/force-kill")]
        public ActionResult ForceKill(string name)
        {
            var app = _control.ApplicationManager.GetApplication(name);
            if (app == null)
                return NotFound(new { error = "Application not found: " + name });
            // COREDUMP 스크립트가 설정된 경우에만 덤프 수집(미설정 시 false 반환 후 그대로 종료)
            _control.ExecuteCoreDump(name, app.Type);
            bool success = _control.Kill(name, app.Type);
            return Ok(new { success });
        }
    }

    /// <summary>
    /// control 프로세스 heartbeat 설정 조회/변경. 변경 시 live 객체에 즉시 적용하고
    /// NA_X_OPTION(8001~8009)에 영구 저장한다. (control 프로세스에서만 동작.)
    /// </summary>
    [ApiController]
    [Route("api/heartbeat-settings")]
    public class HeartbeatSettingsController : ControllerBase
    {
        private readonly IControlServerManager _control;

        public HeartbeatSettingsController(IControlServerManager control)
        {
            _control = control;
        }

        // GET /api/heartbeat-settings — 현재 live 설정값
        [HttpGet]
        public ActionResult<HeartbeatSettingsDto> Get()
        {
            return new HeartbeatSettingsDto
            {
                UseHeartBeat = _control.UseHeartBeat,
                HeartBeatInterval = _control.HeartBeatInterval,
                HeartBeatStartDelay = _control.HeartBeatStartDelay,
                HeartBeatStartupGrace = _control.HeartBeatStartupGrace,
                HeartBeatTimeout = _control.HeartBeatTimeout,
                HeartBeatRetryCount = _control.HeartBeatRetryCount,
                HeartBeatRetryTimeout = _control.HeartBeatRetryTimeout,
                HeartBeatFailWhenProcessDown = _control.HeartBeatFailWhenProcessDown,
                HeartBeatFailWhenProcessHang = _control.HeartBeatFailWhenProcessHang
            };
        }

        // PUT /api/heartbeat-settings — 설정 변경 → live 적용 + 필요한 경우 재스케줄 + DB 영구 저장
        [HttpPut]
        public ActionResult Update([FromBody] HeartbeatSettingsDto dto)
        {
            if (dto == null)
                return BadRequest(new { error = "요청 본문이 필요합니다." });
            if (dto.HeartBeatInterval <= 0 || dto.HeartBeatStartDelay < 0 || dto.HeartBeatStartupGrace < 0
                || dto.HeartBeatTimeout < 0 || dto.HeartBeatRetryTimeout < 0 || dto.HeartBeatRetryCount < 0)
                return BadRequest(new { error = "값은 음수일 수 없으며 Interval은 0보다 커야 합니다." });
            if (dto.HeartBeatTimeout >= dto.HeartBeatInterval)
                return BadRequest(new { error = "HeartBeatTimeout은 HeartBeatInterval보다 작아야 합니다." });
            if (dto.HeartBeatFailWhenProcessDown < 0 || dto.HeartBeatFailWhenProcessDown > 2
                || dto.HeartBeatFailWhenProcessHang < 0 || dto.HeartBeatFailWhenProcessHang > 2)
                return BadRequest(new { error = "ProcessDown/Hang 동작 옵션은 0/1/2만 허용됩니다." });

            bool wasUsing = _control.UseHeartBeat;
            long oldInterval = _control.HeartBeatInterval;
            long oldStartDelay = _control.HeartBeatStartDelay;

            // live 적용 (Timeout/RetryCount/RetryTimeout/StartupGrace/FailDown/FailHang는 매 주기 즉시 반영)
            _control.UseHeartBeat = dto.UseHeartBeat;
            _control.HeartBeatInterval = dto.HeartBeatInterval;
            _control.HeartBeatStartDelay = dto.HeartBeatStartDelay;
            _control.HeartBeatStartupGrace = dto.HeartBeatStartupGrace;
            _control.HeartBeatTimeout = dto.HeartBeatTimeout;
            _control.HeartBeatRetryCount = dto.HeartBeatRetryCount;
            _control.HeartBeatRetryTimeout = dto.HeartBeatRetryTimeout;
            _control.HeartBeatFailWhenProcessDown = dto.HeartBeatFailWhenProcessDown;
            _control.HeartBeatFailWhenProcessHang = dto.HeartBeatFailWhenProcessHang;

            // 스케줄 반영: Interval/StartDelay는 트리거에 baked-in이라 전체 재스케줄 필요.
            if (!dto.UseHeartBeat)
            {
                if (wasUsing) _control.UnscheduleHeartBeats();
            }
            else if (!wasUsing || oldInterval != dto.HeartBeatInterval || oldStartDelay != dto.HeartBeatStartDelay)
            {
                // off→on 전환, 또는 주기/시작지연 변경 → 전체 트리거를 현재값으로 재생성
                _control.ScheduleHeartBeats();
            }

            // 영구 저장 (NA_X_OPTION upsert)
            _control.SaveHeartBeatOptions();

            return Ok(new { success = true });
        }
    }

    /// <summary>
    /// 로그 조회 엔드포인트. NA_L_LOGMESSAGE(본문) + NA_L_LARGELOGMESSAGE(4000자 초과 분할 텍스트)를
    /// 시간 범위 + 필터로 조회한다. 시간 비교/반환은 모두 UTC 기준이며, 로컬↔UTC 변환은 클라이언트(ACS.UI)가 담당한다.
    /// </summary>
    [ApiController]
    [Route("api/logs")]
    public class LogsController : ControllerBase
    {
        private readonly ACS.Database.AcsDbContext _db;

        public LogsController(ACS.Database.AcsDbContext db)
        {
            _db = db;
        }

        // GET /api/logs?from=&to=&level=&keyword=&process=&messageName=&transactionId=&limit=
        // from/to는 ISO-8601 UTC 문자열. 모든 필터는 선택값.
        [HttpGet]
        public ActionResult<List<LogMessageDto>> Get(
            [FromQuery] string from = null,
            [FromQuery] string to = null,
            [FromQuery] string level = null,
            [FromQuery] string keyword = null,
            [FromQuery] string process = null,
            [FromQuery] string messageName = null,
            [FromQuery] string transactionId = null,
            [FromQuery] int limit = 1000)
        {
            if (limit <= 0) limit = 1000;
            if (limit > 5000) limit = 5000;

            IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();

            if (TryParseUtc(from, out var fromUtc))
                q = q.Where(x => x.Time >= fromUtc);
            if (TryParseUtc(to, out var toUtc))
                q = q.Where(x => x.Time <= toUtc);
            if (!string.IsNullOrWhiteSpace(level) &&
                !string.Equals(level, "All", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.LogLevel == level);
            if (!string.IsNullOrWhiteSpace(process))
                q = q.Where(x => x.ProcessName == process);
            if (!string.IsNullOrWhiteSpace(messageName))
                q = q.Where(x => x.MessageName == messageName);
            if (!string.IsNullOrWhiteSpace(transactionId))
                q = q.Where(x => x.TransactionId == transactionId);
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(x => EF.Functions.ILike(x.Text, "%" + keyword + "%"));

            var rows = q.OrderByDescending(x => x.Time).Take(limit).ToList();

            var dtos = new List<LogMessageDto>(rows.Count);
            foreach (var x in rows)
            {
                dtos.Add(new LogMessageDto
                {
                    Id = x.Id,
                    Time = ToUtc(x.Time),
                    LogLevel = x.LogLevel,
                    ProcessName = x.ProcessName,
                    MessageName = x.MessageName,
                    CommunicationMessageName = x.CommunicationMessageName,
                    TransactionId = x.TransactionId,
                    TransportCommandId = x.TransportCommandId,
                    OperationName = x.OperationName,
                    ThreadName = x.ThreadName,
                    CarrierName = x.CarrierName,
                    MachineName = x.MachineName,
                    UnitName = x.UnitName,
                    Text = x.Text,
                    HasLargeText = string.IsNullOrEmpty(x.Text)
                });
            }
            return dtos;
        }

        // GET /api/logs/{id}/text — NA_L_LARGELOGMESSAGE를 Sequence 순으로 재조합한 전체 텍스트.
        // 분할 텍스트가 없으면 본문 Text를 반환한다.
        [HttpGet("{id}/text")]
        public ActionResult<string> GetText(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "id가 필요합니다." });

            var parts = _db.LargeLogMessages.AsNoTracking()
                .Where(l => l.LogMessageId == id)
                .OrderBy(l => l.Sequence)
                .Select(l => l.LargeText)
                .ToList();

            if (parts.Count > 0)
                return string.Concat(parts);

            var text = _db.LogMessages.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.Text)
                .FirstOrDefault();
            return text ?? string.Empty;
        }

        /// <summary>쿼리 문자열(ISO-8601)을 UTC DateTime(Kind=Utc)으로 파싱.</summary>
        private static bool TryParseUtc(string s, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dto))
            {
                utc = dto.UtcDateTime; // Kind=Utc
                return true;
            }
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dt))
            {
                utc = dt.Kind == DateTimeKind.Utc ? dt
                    : dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime()
                    : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return true;
            }
            return false;
        }

        /// <summary>읽어온 Time을 Kind에 관계없이 UTC(Kind=Utc)로 정규화. (Npgsql legacy read Kind 차이 방어.)</summary>
        private static DateTime? ToUtc(DateTime? t)
        {
            if (t is not { } v) return null;
            return v.Kind switch
            {
                DateTimeKind.Utc => v,
                DateTimeKind.Local => v.ToUniversalTime(),
                _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            };
        }
    }
}
