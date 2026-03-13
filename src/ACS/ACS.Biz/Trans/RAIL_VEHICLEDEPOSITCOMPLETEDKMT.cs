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
using ACS.Workflow;

using System.Xml;
using ACS.Framework.Logging;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;


namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEDEPOSITCOMPLETEDKMT : BaseBizJob
    {
        protected static Logger logger = Logger.GetLogger(typeof(RAIL_VEHICLEDEPOSITCOMPLETEDKMT));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        private IWorkflowManager WorkflowManager;
        private IResourceManagerEx ResourceManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        //
        public DataHandlingServiceEx DataHandlingService;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("==========================================================================");
            logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETEDKMT Start");
            XmlDocument rail_vehicledepositcompletedkmt = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();
            ResourceManager = LifetimeScope.Resolve<IResourceManagerEx>();
            //
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();


            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicledepositcompletedkmt);

            LocationEx sourceLocation = ResourceManager.GetLocationByStationId(vehicleMsg.NodeId);
           
  
            logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETEDKMT Normal Sequence Step.01 CheckVehicle (TRUE)");
            TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
            logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleEvent] = vehicleEvent;");

            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");

            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [EventTime]");

            if (AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
            {
                logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETEDKMT Normal Sequence Step.02 CheckAlarmClearMessage (TRUE)");
                InterfaceService.ReportAlarmClearReport(vehicleMsg);
                //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                ResourceService.DeleteUIInform(vehicleMsg);
            }

            //if (!ResourceService.CheckVehicleStateIsTransferDest(vehicleMsg)) return 0;

            //this.WorkflowManager.Execute("VEHICLE_DEPOSITCOMPLETEDKMT", vehicleMsg);
            //logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETEDKMT Normal Sequence Step.03 MOVE to VEHICLE_DEPOSITCOMPLETEDKMT");

            logger.Debug("VEHICLE_DEPOSITCOMPLETEDKMT Normal Sequence Step.1 CheckVehicle (TRUE)");
            ResourceService.ChangeVehicleTransferStateToDepositComplete(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = DEPOSIT_COMPLETE");

            if (InterfaceService.CheckTransferCommand(vehicleMsg))
            {
                logger.Debug("VEHICLE_DEPOSITCOMPLETEDKMT Normal Sequence Step.2 CheckTransferCommand (RETURN TRUE)");
                TransferService.ChangeTransportCommandStateToComplete(vehicleMsg);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = COMPLETED");

                if ((vehicleMsg.TransportCommand) != null && sourceLocation != null) vehicleMsg.TransportCommand.Dest = sourceLocation.PortId;
                InterfaceService.ReportAGVUnloadComplete(vehicleMsg);
                logger.Debug("VEHICLE_DEPOSITCOMPLETEDKMT Normal Sequence Step.3 Report AGVUnloadComplete(TRSJOBREPORT : JOBCOMPLETE) To HOST");

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
            //
            if (ResourceService.SearchWaitPoint(vehicleMsg))
            {
                //InterfaceService.SendGoWaitpoint(vehicleMessage);

                SendWaitpointToAgv(vehicleMsg);
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.5 SendGoWaitpoint (TO ES)");

                TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = destNodeId");
            }
            else
            {
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Sequence Step.4 SearchWaitPoint (RETURN FALSE)");
                InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Sequence Step.5 SendGoWaitpoint0000 (TO ES)");
                TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = 9000");
            }

            Thread.Sleep(1000);

            ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [ProcessState] =IDLE ");
            //

            logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETEDKMT End");
            logger.Debug("==========================================================================");
            return 0;

        }

        private void SendWaitpointToAgv(VehicleMessageEx vehicleMessage)
        {
            bool end = false;
            int count = 0;
            int count_limit = 30;

            //181019 Send wait point 30 times (While → Do-While)  

            //TransferService.CreateTransportCommandRequest(vehicleMessage);

            //while (!TransferService.IsTransportCommandRequestReplied(vehicleMessage))
            //{
            //    if (count > count_limit)
            //    {
            //        TransferService.DeleteTransportCommandRequest(vehicleMessage);
            //        return;
            //    }

            //    InterfaceService.SendGoWaitpoint(vehicleMessage);
            //    count++;
            //    Thread.Sleep(1000);
            //}

            while (!end)
            {
                TransferService.CreateTransportCommandRequest(vehicleMessage);
                InterfaceService.SendGoWaitpoint(vehicleMessage);

                System.Threading.Thread.Sleep(3000);

                if (TransferService.IsTransportCommandRequestReplied(vehicleMessage))
                {
                    //Expression 식 확인 필요
                    end = true;
                    return;
                }
                else
                {
                    //Expression 식 확인 필요  while_linit  ?????????????
                    end = DataHandlingService.Greater(count, count_limit);
                    count++;
                }
            }
            TransferService.DeleteTransportCommandRequest(vehicleMessage);
        }//

    }
}
