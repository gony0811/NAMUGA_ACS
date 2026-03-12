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
using log4net;
using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEINFOREPORT : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(RAIL_VEHICLEINFOREPORT));
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

            XmlDocument rail_vehicleinforeport = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            Boolean iRunState;
            Boolean iFullState;
            Boolean iVehicleDest;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicleinforeport);
            if(ResourceService.CheckVehicle(vehicleMsg))
            {
                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);

                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);

                ResourceService.UpdateVehicleEventTime(vehicleMsg);

                iRunState = ResourceService.UpdateVehicleRunState(vehicleMsg);

                iFullState = ResourceService.UpdateVehicleFullState(vehicleMsg);

                //리턴 값 안맞음
                //iVehicleDest = ResourceService.UpdateVehicleDestNodeId(vehicleMsg);
                ResourceService.UpdateVehicleDestNodeId(vehicleMsg);
                if (vehicleMsg.Vehicle.BayId.Equals("S0_CP")) return 0;

                if(ResourceService.CheckVehicleSCodeStationIs0000(vehicleMsg))
                {
                    logger.Debug("RAIL_VEHICLEINFOREPORT CheckVehicleSCodeStationIs0000 finish(TRUE)");
                    return 0;
                }
                else
                {         
                    if(ResourceService.CheckVehicleSCodeStationIs9999(vehicleMsg))
                    {
                        logger.Debug("RAIL_VEHICLEINFOREPORT VEHICLE STATE CheckVehicleSCodeStationIs9999 Finish(True)");
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
                        return 0;
                    }
                    else
                    {
                        if(TransferService.CheckTransportCommandsByVehicle(vehicleMsg))
                        {                       
                            if(ResourceService.CheckVehicleDestNodeByStationId(vehicleMsg))
                            {
                                return 0;
                            }
                            else
                            {

                                InterfaceService.SendTransportMessageVehicleDestNode(vehicleMsg);
                                logger.Debug("RAIL_VEHICLEINFOREPORT SendTransportMessageVehicleDestNode Finish");
                            }
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
            else
            {
                return 0;
            }
            return 0;
        }
    }
}
