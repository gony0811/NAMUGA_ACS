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
    class RAIL_RECOVERAGVOUTRAILSUCCESS : BaseBizJob
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
            XmlDocument rail_recoveragvoutrailsuccess = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_recoveragvoutrailsuccess);
            if(ResourceService.CheckVehicle(vehicleMsg))
            {
                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                ResourceService.CreateVehicleHistory(vehicleMsg);
                return 0;
            }
            else
            {
                return 0;
            }

        }
    }
}
