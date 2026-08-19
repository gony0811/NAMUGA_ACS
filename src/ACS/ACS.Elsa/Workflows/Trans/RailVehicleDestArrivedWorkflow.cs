using System;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEDESTARRIVED 워크플로우.
    ///
    /// RailVehicleUpdateActivity 가 EI 텔레메트리에서 currentNodeId 가
    /// vehicle.AcsDestNodeId 와 일치하는 시점(= source 도착, acquire-complete 이전)을
    /// 검출하면 이 워크플로우를 dispatch 한다. JOBREPORT(ARRIVED) 를 Host 로 송신하여
    /// HostJobReportWorkflow 가 MES 에 forward 하도록 한다.
    ///
    /// Input["Arguments"] = new object[] { vehicleId (string) }
    /// </summary>
    public class RailVehicleDestArrivedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEDESTARRIVED";
            builder.Name = "RAIL-VEHICLEDESTARRIVED";
            builder.Description = "AMR가 AcsDestNodeId(=source) 도착 시 JOBREPORT(ARRIVED)를 Host로 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new RailVehicleDestArrivedActivity(),
                }
            };
        }
    }

    [Activity("ACS.Trans", "Rail Vehicle Dest Arrived",
        "Vehicle 의 AcsDestNodeId 도착 시 JOBREPORT(ARRIVED) Host 전송")]
    public class RailVehicleDestArrivedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(RailVehicleDestArrivedActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("RailVehicleDestArrivedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var vehicleId = args[0] as string;
                if (string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("RailVehicleDestArrivedActivity: vehicleId 가 비어있습니다.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("RailVehicleDestArrivedActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                var transferManager = accessor.Resolve<ITransferManagerEx>();
                var messageManager = accessor.Resolve<IMessageManagerEx>();
                if (resourceManager == null || transferManager == null || messageManager == null)
                {
                    logger.Error("RailVehicleDestArrivedActivity: 필수 매니저 해석 실패 " +
                                 $"(rm={(resourceManager != null)}, tm={(transferManager != null)}, mm={(messageManager != null)})");
                    return;
                }

                VehicleEx vehicle = resourceManager.GetVehicle(vehicleId);
                if (vehicle == null)
                {
                    logger.Warn($"RailVehicleDestArrivedActivity: Vehicle 미존재 vehicleId={vehicleId}");
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommandByVehicleId(vehicleId);
                if (tc == null)
                {
                    logger.Warn($"RailVehicleDestArrivedActivity: TC 없음 vehicleId={vehicleId}");
                    return;
                }
                
                if (TransportCommandEx.JOBTYPE_CHARGEMOVE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info($"RailVehicleDestArrivedActivity: CHARGEMOVE 작업 — skip. " +
                                $"vehicleId={vehicleId}, tc={tc.JobId}");
                    return;
                }

                // EXCHANGE(v2) S5: STEP 기반 waypoint(origin/mid/dest) 도착 판정 + ARRIVED(step) 보고 (D4 분기)
                if (TransportCommandEx.JOBTYPE_EXCHANGE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                {
                    Activities.ExchangeTransHandlers.OnDestArrived(tc, vehicle, transferManager, resourceManager, messageManager);
                    return;
                }

                // StationId 1개에 다수의 LocationId(:LEFT/:RIGHT 등) 가 매핑되므로
                // tc.Source / tc.Dest 를 LocationId 키로 직접 조회한 뒤 그 StationId 가
                // vehicle.CurrentNodeId 와 같은지로 도착 매칭을 판정한다.
                LocationEx tcSourceLoc = !string.IsNullOrEmpty(tc.Source)
                    ? resourceManager.GetLocationByLocationId(tc.Source) : null;
                LocationEx tcDestLoc = !string.IsNullOrEmpty(tc.Dest)
                    ? resourceManager.GetLocationByLocationId(tc.Dest) : null;

                bool matchesSource = tcSourceLoc != null
                    && string.Equals(tcSourceLoc.StationId, vehicle.CurrentNodeId, StringComparison.OrdinalIgnoreCase);
                bool matchesDest = tcDestLoc != null
                    && string.Equals(tcDestLoc.StationId, vehicle.CurrentNodeId, StringComparison.OrdinalIgnoreCase);
                if (!matchesSource && !matchesDest)
                {
                    logger.Info($"RailVehicleDestArrivedActivity: ARRIVED skip — reason=not-source-dest, " +
                                $"vehicleId={vehicleId}, currentNode={vehicle.CurrentNodeId}, " +
                                $"tcSource={tc.Source} (stationId={tcSourceLoc?.StationId}), " +
                                $"tcDest={tc.Dest} (stationId={tcDestLoc?.StationId})");
                    return;
                }

                LocationEx matchedLoc = matchesSource ? tcSourceLoc : tcDestLoc;
                if (!LocationExs.LOCATION_TYPE_EQP.Equals(matchedLoc.Type, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info($"RailVehicleDestArrivedActivity: ARRIVED skip — reason=not-eqp, " +
                                $"vehicleId={vehicleId}, currentNode={vehicle.CurrentNodeId}, " +
                                $"matchedLocationId={matchedLoc.LocationId}, matchedType={matchedLoc.Type}");
                    return;
                }

                string matchedSide = matchesSource ? "source" : "dest";

                // v0.3: 도착 보고 idempotency — pose 기반 판정(RailVehicleUpdate)과 AMR ARRIVED reply(RAIL-VEHICLEARRIVED)
                // 가 같은 도착에 대해 이중 발화할 수 있으므로, TC AdditionalInfo 의 ARRIVED 마커("<nodeId>|<tcState>")로 방어한다.
                string arrivedMarker = (vehicle.CurrentNodeId ?? "") + "|" + (tc.State ?? "");
                string reportedMarker = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_ARRIVED);
                if (string.Equals(reportedMarker, arrivedMarker, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info($"RailVehicleDestArrivedActivity: ARRIVED skip — reason=already-reported ({arrivedMarker}), " +
                                $"vehicleId={vehicleId}, tc={tc.JobId}");
                    return;
                }
                tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo, ExchangeInfo.KEY_ARRIVED, arrivedMarker);
                transferManager.UpdateTransportCommand(tc);

                messageManager.SendJobReportToHost(
                    "START", tc.JobId, vehicleId, tc.JobType ?? "", tc.Description ?? "");

                messageManager.SendJobReportToHost(
                    "ARRIVED", tc.JobId, vehicleId,
                    tc.JobType ?? "",
                    tc.Description ?? "");

                logger.Info($"[RailVehicleDestArrived] JOBREPORT(ARRIVED) sent: " +
                            $"vehicleId={vehicleId}, tc={tc.JobId}, jobType={tc.JobType}, " +
                            $"currentNode={vehicle.CurrentNodeId}, acsDestNode={vehicle.AcsDestNodeId}, " +
                            $"matched={matchedSide}, matchedLocationId={matchedLoc.LocationId}, locationType=EQP");
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleDestArrivedActivity 오류", e);
            }
        }
    }
}
