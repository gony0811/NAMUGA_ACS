using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            // Only handle GET requests
            if (request.Method != HttpMethods.Get)
            {
                // Handle CORS preflight
                if (request.Method == HttpMethods.Options)
                {
                    context.Response = StringHttpResponse.Create("", HttpResponseCode.Ok, "text/plain");
                    return Task.CompletedTask;
                }
                return nextHandler();
            }

            // Route: /api/{resource}
            string resource = request.RequestParameters.Length > 1 ? request.RequestParameters[1] : "";

            string json;
            try
            {
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
                        Id = v.Id,
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
                        Id = n.Id,
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
                        Speed = l.Speed
                    });
                }
            }

            return JsonConvert.SerializeObject(dtos, JsonSettings);
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
    }
}
