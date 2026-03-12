using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Spring.Context;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Workflow;

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEEMPTY : BaseBizJob
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
            XmlDocument rail_vehicleempty = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            Boolean result;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicleempty);
            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            if(InterfaceService.CheckTransportCommand(vehicleMsg))
            {
                result = ResourceService.CheckTransportCommandDestNodeByVehicleCurrentNode(vehicleMsg);
                ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                MaterialService.DeleteCarrier(vehicleMsg);
                TransferService.DeleteTransportCommand(vehicleMsg);
                TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                InterfaceService.requestAGVJobCancel(vehicleMsg);
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
                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
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
