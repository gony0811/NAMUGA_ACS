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
    class RAIL_VEHICLECHARGECOMPLETED : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(RAIL_VEHICLECHARGECOMPLETED));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("==========================================================================");
            logger.Debug("TS RAIL_VEHICLECHARGECOMPLETED Start");

            XmlDocument rail_vehiclechargecompleted = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehiclechargecompleted);
            if(ResourceService.CheckVehicleChargeState(vehicleMsg))
            {
                logger.Debug("RAIL_VEHICLECHARGECOMPLETED Normal Sequence Step.1 CheckVehicle (TRUE)");

                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleEvent] = VehicleEvent");

                ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);

                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                if(AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
                {
                    InterfaceService.ReportAlarmClearReport(vehicleMsg);
                    //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                    //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                    AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                    ResourceService.DeleteUIInform(vehicleMsg);
                }
                TransferService.ChangeTransportCommandStateToComplete(vehicleMsg);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = COMPLETED");

                ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = NOTASSIGNED");

                TransferService.DeleteChargeTransportCommandsByVehicle(vehicleMsg);
                logger.Debug(">>>>delete NA_T_TRANSPORTCMD");

                TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId] ='' ");

                ResourceService.UpdateVehicleLastCharge(vehicleMsg);

                if(InterfaceService.CheckTransportCommand(vehicleMsg))
                {
                    logger.Debug("RAIL_VEHICLECHARGECOMPLETED Normal Sequence Step.2 CheckTransferCommand (RETURN TRUE)");
                    return 0;
                }
                else
                {
                    logger.Debug("RAIL_VEHICLECHARGECOMPLETED Normal Sequence Step.2 CheckTransferCommand (RETURN FALSE)");

                    if (ResourceService.SearchWaitPoint(vehicleMsg))
                    {
                        InterfaceService.SendGoWaitpoint(vehicleMsg);
                        logger.Debug("RAIL_VEHICLECHARGECOMPLETED Normal Sequence Step.4 SendGoWaitpoint (TO ES)");
                        TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = destNodeId");
                    }
                    else
                    {
                        logger.Debug("RAIL_VEHICLECHARGECOMPLETED Abnormal Sequence Step.3 SearchWaitPoint (RETURN FALSE)");
                        InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                        logger.Debug("RAIL_VEHICLECHARGECOMPLETED Abnormal Sequence Step.4 SendGoWaitpoint0000 (TO ES)");
                        TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = 9000");
                    }
                    Thread.Sleep(1000);

                    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                    logger.Debug(">>>>update NA_R_VEHICLE [ProcessState] =IDLE ");
                }
            }
            else
            {
                logger.Debug("RAIL_VEHICLECHARGECOMPLETED Normal Sequence Step.1 CheckVehicle (RETURN FALSE)");
                return 0;
            }
            logger.Debug("TS RAIL_VEHICLECHARGECOMPLETED End");
            logger.Debug("==========================================================================");

            return 0;
        }
    }
}
