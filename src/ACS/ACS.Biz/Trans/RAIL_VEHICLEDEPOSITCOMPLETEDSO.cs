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
using log4net;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;


namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEDEPOSITCOMPLETEDSO : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(RAIL_VEHICLEDEPOSITCOMPLETEDSO));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        private IWorkflowManager WorkflowManager;
        private IResourceManagerEx ResourceManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("==========================================================================");
            logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETEDSO Start");
            XmlDocument rail_vehicledepositcompleted = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");
            ResourceManager = (IResourceManagerEx)ApplicationContext.GetObject("ResourceManager");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicledepositcompleted);

            LocationEx sourceLocation = ResourceManager.GetLocationByStationId(vehicleMsg.NodeId);
           
  
            logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETED Normal Sequence Step.01 CheckVehicle (TRUE)");
            TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
            logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleEvent] = vehicleEvent;");

            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");

            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [EventTime]");

            if (AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
            {
                logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETED Normal Sequence Step.02 CheckAlarmClearMessage (TRUE)");
                InterfaceService.ReportAlarmClearReport(vehicleMsg);
                //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                ResourceService.DeleteUIInform(vehicleMsg);
            }

            //if (!ResourceService.CheckVehicleStateIsTransferDest(vehicleMsg)) return 0;

            //this.WorkflowManager.Execute("VEHICLE_DEPOSITCOMPLETED", vehicleMsg);
            //logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETED Normal Sequence Step.03 MOVE to VEHICLE_DEPOSITCOMPLETED");

            logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.1 CheckVehicle (TRUE)");
            ResourceService.ChangeVehicleTransferStateToDepositComplete(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = DEPOSIT_COMPLETE");

            if (InterfaceService.CheckTransferCommand(vehicleMsg))
            {
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.2 CheckTransferCommand (RETURN TRUE)");
                TransferService.ChangeTransportCommandStateToComplete(vehicleMsg);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = COMPLETED");

                if ((vehicleMsg.TransportCommand) != null && sourceLocation != null) vehicleMsg.TransportCommand.Dest = sourceLocation.PortId;
                InterfaceService.ReportAGVUnloadComplete(vehicleMsg);
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.3 Report AGVUnloadComplete(TRSJOBREPORT : JOBCOMPLETE) To HOST");

                ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = NOTASSIGNED");

                MaterialService.DeleteCarrier(vehicleMsg);
                logger.Debug(">>>>delete NA_M_CARRIER");

                TransferService.DeleteTransportCommand(vehicleMsg);
                logger.Debug(">>>>delete NA_T_TRANSPORTCMD");

                TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId] ='' ");

                TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [Path] = ''");

                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = ''");
            }

                logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETED End");
            logger.Debug("==========================================================================");
            return 0;

        }
    }
}
