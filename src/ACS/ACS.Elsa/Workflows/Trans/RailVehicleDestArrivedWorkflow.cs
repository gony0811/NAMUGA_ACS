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

                LocationEx arrivedLocation = resourceManager.GetLocationByStationId(vehicle.CurrentNodeId);
                if (arrivedLocation == null)
                {
                    logger.Warn($"RailVehicleDestArrivedActivity: ARRIVED skip — reason=location-not-found, " +
                                $"vehicleId={vehicleId}, currentNode={vehicle.CurrentNodeId}, " +
                                $"tcSource={tc.Source}, tcDest={tc.Dest}");
                    return;
                }

                bool matchesSource = !string.IsNullOrEmpty(tc.Source)
                    && string.Equals(arrivedLocation.LocationId, tc.Source, StringComparison.OrdinalIgnoreCase);
                bool matchesDest = !string.IsNullOrEmpty(tc.Dest)
                    && string.Equals(arrivedLocation.LocationId, tc.Dest, StringComparison.OrdinalIgnoreCase);
                if (!matchesSource && !matchesDest)
                {
                    logger.Info($"RailVehicleDestArrivedActivity: ARRIVED skip — reason=not-source-dest, " +
                                $"vehicleId={vehicleId}, currentNode={vehicle.CurrentNodeId}, " +
                                $"arrivedLocationId={arrivedLocation.LocationId}, arrivedType={arrivedLocation.Type}, " +
                                $"tcSource={tc.Source}, tcDest={tc.Dest}");
                    return;
                }

                if (!LocationExs.LOCATION_TYPE_EQP.Equals(arrivedLocation.Type, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info($"RailVehicleDestArrivedActivity: ARRIVED skip — reason=not-eqp, " +
                                $"vehicleId={vehicleId}, currentNode={vehicle.CurrentNodeId}, " +
                                $"arrivedLocationId={arrivedLocation.LocationId}, arrivedType={arrivedLocation.Type}");
                    return;
                }

                string matchedSide = matchesSource ? "source" : "dest";

                messageManager.SendJobReportToHost(
                    "ARRIVED",
                    tc.JobId,
                    vehicleId,
                    tc.JobType ?? "",
                    tc.Description ?? "");

                logger.Info($"[RailVehicleDestArrived] JOBREPORT(ARRIVED) sent: " +
                            $"vehicleId={vehicleId}, tc={tc.JobId}, jobType={tc.JobType}, " +
                            $"currentNode={vehicle.CurrentNodeId}, acsDestNode={vehicle.AcsDestNodeId}, " +
                            $"matched={matchedSide}, arrivedLocationId={arrivedLocation.LocationId}, locationType=EQP");
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleDestArrivedActivity 오류", e);
            }
        }
    }
}
