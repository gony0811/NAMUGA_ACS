using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Core.Base.Interface;
using ACS.Core.Cache;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Message.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLELOCATIONCHANGED 워크플로우.
    ///
    /// EI 프로세스에서 AMR Pose 기반으로 노드 변경 감지 시
    /// RabbitMQ를 통해 전송된 XML 메시지를 수신하여 처리한다.
    /// Manager 인터페이스를 직접 사용하여 순환 참조를 방지.
    /// </summary>
    public class RailVehicleLocationChangedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLELOCATIONCHANGED";
            builder.Name = "RAIL-VEHICLELOCATIONCHANGED";
            builder.Description = "AMR 위치 변경 시 Vehicle 위치 업데이트 및 이벤트 처리";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new RailVehicleLocationChangedActivity(),
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLELOCATIONCHANGED 처리 Activity.
    /// Manager 인터페이스(IResourceManagerEx, ITransferManagerEx 등)를 직접 사용.
    /// </summary>
    [Activity("ACS.Trans", "Rail Vehicle Location Changed",
        "AMR 위치 변경 시 Vehicle 위치 업데이트")]
    public class RailVehicleLocationChangedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(RailVehicleLocationChangedActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                // 워크플로우 Input에서 Arguments 추출: [XmlDocument]
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("RailVehicleLocationChangedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var xmlDoc = args[0] as XmlDocument;
                if (xmlDoc == null)
                {
                    logger.Error("RailVehicleLocationChangedActivity: XmlDocument가 null입니다.");
                    return;
                }

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("RailVehicleLocationChangedActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                // Manager 인터페이스 해결 (순환 참조 없음)
                var messageManager = accessor.Resolve<IMessageManagerEx>();
                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                var persistentDao = accessor.Resolve<IPersistentDao>();

                if (messageManager == null || resourceManager == null)
                {
                    logger.Error("RailVehicleLocationChangedActivity: 필수 Manager를 찾을 수 없습니다.");
                    return;
                }

                // XML → VehicleMessageEx 변환
                VehicleMessageEx vehicleMsg = messageManager.CreateVehicleMessageFromES(xmlDoc);
                if (vehicleMsg == null)
                {
                    logger.Error("RailVehicleLocationChangedActivity: VehicleMessageEx 변환 실패.");
                    return;
                }

                logger.Info($"RailVehicleLocationChangedActivity 시작: vehicleId={vehicleMsg.VehicleId}, nodeId={vehicleMsg.NodeId}");

                // 1. Vehicle 조회
                VehicleEx vehicle = resourceManager.GetVehicle(vehicleMsg.VehicleId);
                if (vehicle == null)
                {
                    logger.Warn($"RailVehicleLocationChangedActivity: Vehicle을 찾을 수 없습니다. vehicleId={vehicleMsg.VehicleId}");
                    return;
                }
                vehicleMsg.Vehicle = vehicle;

                // 2. 연결 상태 → CONNECT
                if (!"CONNECT".Equals(vehicle.ConnectionState))
                {
                    resourceManager.UpdateVehicleConnectionState(vehicle, "CONNECT");
                    logger.Info($"Vehicle ConnectionState → CONNECT: vehicleId={vehicleMsg.VehicleId}");
                }

                // 3. 이벤트 시간 갱신
                resourceManager.UpdateVehicleEventTime(vehicle);

                // 4. 노드 확인
                var cacheManager = accessor.Resolve<ICacheManagerEx>();
                NodeEx node = cacheManager?.GetNode(vehicleMsg.NodeId);
                if (node == null)
                {
                    logger.Debug($"RailVehicleLocationChangedActivity: 등록되지 않은 노드. nodeId={vehicleMsg.NodeId}");
                    return;
                }
                vehicleMsg.Node = node;

                // 5. Vehicle 위치 업데이트 (CurrentNodeId)
                string previousNodeId = vehicle.CurrentNodeId;
                resourceManager.UpdateVehicleLocation(vehicle, vehicleMsg.NodeId);
                logger.Info($"RailVehicleLocationChangedActivity: Vehicle 위치 업데이트. " +
                            $"vehicleId={vehicleMsg.VehicleId}, 이전={previousNodeId}, 신규={vehicleMsg.NodeId}");

                logger.Info($"RailVehicleLocationChangedActivity 완료: vehicleId={vehicleMsg.VehicleId}, nodeId={vehicleMsg.NodeId}");
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleLocationChangedActivity 오류", e);
            }
        }
    }
}
