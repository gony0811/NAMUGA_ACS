using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Autofac;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Resource.Model;
using ACS.Workflow;

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEOCCUPIED : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        private IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_vehicleoccupied = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            Boolean result;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicleoccupied);
            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            if(InterfaceService.CheckTransportCommand(vehicleMsg))
            {
                //2018.10.04 KSG
                // VEHICLE OCCUPIED(M08) 로직 처리 
                // SOURCE에 도착했으나 이미 VEHICLE에 CARRIER가 실려있는 경우
                //M08은 Source Position에서만 일어날 수 있음.
                if (ResourceService.CheckTransportCommandSourceNodeByVehicleCurrentNode(vehicleMsg))
                {
                    ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                    //MaterialService.DeleteCarrier(vehicleMsg);
                    //TransferService.DeleteTransportCommand(vehicleMsg);
                    TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                    TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                    TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);


                    TransferService.ChangeTransferCommandVehicleIdEmpty(vehicleMsg);
                    TransferService.ChangeTransportCommandStateToQueued(vehicleMsg);

                    ResourceService.CreateCarrierInform(vehicleMsg);

                    //InterfaceService.requestAGVJobCancel(vehicleMsg);
                }
                return 0;
            }
            else
            {
                if(ResourceService.SearchWaitPoint(vehicleMsg))
                {
                    InterfaceService.SendGoWaitpoint(vehicleMsg);
                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                }
                else
                {
                    InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                    TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                }
            }

            return 0;
        }
    }
}
