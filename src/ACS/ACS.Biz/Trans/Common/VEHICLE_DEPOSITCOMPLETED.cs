using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Workflow;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Transfer.Model;
using System.Xml;
using Spring.Context;
using log4net;

namespace ACS.Biz.Trans.Common
{
    public class VEHICLE_DEPOSITCOMPLETED : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(VEHICLE_DEPOSITCOMPLETED));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        private IWorkflowManager WorkflowManager;
        int RETURNS = 0;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            RETURNS = 0;
            logger.Debug("==========================================================================");
            logger.Debug("TS VEHICLE_DEPOSITCOMPLETED Start");
            VehicleMessageEx vehicleMessage = (VehicleMessageEx)args[0];
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");

            if (InterfaceService.CheckVehicle(vehicleMessage))
            {
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.1 CheckVehicle (TRUE)");
                ResourceService.ChangeVehicleTransferStateToDepositComplete(vehicleMessage);
                logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = DEPOSIT_COMPLETE");

                if (InterfaceService.CheckTransferCommand(vehicleMessage))
                {
                    logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.2 CheckTransferCommand (RETURN TRUE)");
                    TransferService.ChangeTransportCommandStateToComplete(vehicleMessage);
                    logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = COMPLETED");

                    InterfaceService.ReportAGVUnloadComplete(vehicleMessage);
                    logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.3 Report AGVUnloadComplete(TRSJOBREPORT : JOBCOMPLETE) To HOST");

                    ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = NOTASSIGNED");

                    MaterialService.DeleteCarrier(vehicleMessage);
                    logger.Debug(">>>>delete NA_M_CARRIER");

                    TransferService.DeleteTransportCommand(vehicleMessage);
                    logger.Debug(">>>>delete NA_T_TRANSPORTCMD");

                    TransferService.UpdateVehicleTransportCommandEmpty(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId] ='' ");

                    TransferService.UpdateVehiclePathEmpty(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [Path] = ''");

                    TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = ''");


                    // 검증 전까지 JOB STEAL 기능 미사용 2018.08.30 
                    TransportCommandEx StealTransportCommand = TransferService.GetPossibleStealTransportCommand(vehicleMessage);
                    if (StealTransportCommand == null)
                    {
                        //STEAL 못했을 경우
                        RETURNS = 0; //false
                    }
                    else
                    {
                        FALSE_GetPossibleStealTransportCommand1(StealTransportCommand, vehicleMessage);
                    }
                }
                else
                {
                    logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Case CheckTransferCommand (RETURN FALSE)");
                    FALSE_CheckTransferCommand1(vehicleMessage);
                }
            }
            else
            {//Terminate 함수 추가 필요

            }


            //RAIL_VEHICLEDEPOSITCOMPLETED로 부터 아래 로직 이관
            if (RETURNS == 1)
            {
                return RETURNS;
            }
            else
            {
                if (ResourceService.SearchWaitPoint(vehicleMessage))
                {
                    //InterfaceService.SendGoWaitpoint(vehicleMessage);

                    SendWaitpointToAgv(vehicleMessage);
                    logger.Debug("VEHICLE_DEPOSITCOMPLETED Normal Sequence Step.5 SendGoWaitpoint (TO ES)");

                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = destNodeId");
                }
                else
                {
                    logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Sequence Step.4 SearchWaitPoint (RETURN FALSE)");
                    InterfaceService.SendGoWaitpoint0000(vehicleMessage);
                    logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Sequence Step.5 SendGoWaitpoint0000 (TO ES)");
                    TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = 9000");
                }

                Thread.Sleep(1000);

                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
                logger.Debug(">>>>update NA_R_VEHICLE [ProcessState] =IDLE ");
            }

            logger.Debug("TS VEHICLE_DEPOSITCOMPLETED End");
            logger.Debug("==========================================================================");
            return RETURNS;

        }

        public void FALSE_GetPossibleStealTransportCommand1(TransportCommandEx stealTransportCommand, VehicleMessageEx vehicleMessage)
        {
            VehicleMessageEx oldVehicleMessage = InterfaceService.CreateVehicleMessage(stealTransportCommand);
            if (ResourceService.SearchWaitPoint(oldVehicleMessage))
            {
                InterfaceService.SendGoWaitpoint(oldVehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(oldVehicleMessage);
            }
            else
            {
                InterfaceService.SendGoWaitpoint0000(oldVehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeIdTo0000(oldVehicleMessage);
            }
            ResourceService.ChangeVehicleTransferStateToNotAssigned(oldVehicleMessage);
            ResourceService.ChangeVehicleProcessStateToIdle(oldVehicleMessage);
            TransferService.UpdateVehicleTransportCommandEmpty(oldVehicleMessage);
            TransferService.UpdateVehiclePathEmpty(oldVehicleMessage);
            VehicleMessageEx stealVehicleMessage = InterfaceService.CreateVehicleMessageByStealTransportCommand(vehicleMessage, stealTransportCommand);
            TransferService.ChangeTransferCommandVehicleId(stealVehicleMessage);
            ResourceService.ChangeVehicleTransportCommandId(stealVehicleMessage);
            TransferService.ChangeTransportCommandStateAndVehicleIdWhenStealCommand(stealVehicleMessage);
            ResourceService.ChangeVehicleProcessStateToRun(stealVehicleMessage);
            ResourceService.ChangeVehicleTransferStateToAssigned(stealVehicleMessage);
            TransferService.UpdateVehicleAcsDestNodeId(stealVehicleMessage);


            //COMMON_SEND_TRANSFER_SOURCE common_send_transfer_source = new COMMON_SEND_TRANSFER_SOURCE();


            // LSJ 
            // Workflow 실행을 함수로 대체함. workflow의 응답에 따라 처리가 다름
            COMMON_SEND_TRANSFER_SOURCE cOMMON_SEND_TRANSFER_SOURCE = new COMMON_SEND_TRANSFER_SOURCE();
            object[] args = { stealVehicleMessage, ApplicationContext };
            if (cOMMON_SEND_TRANSFER_SOURCE.ExecuteJob(args) == 1)
            //if (WorkflowManager.Execute("COMMON_SEND_TRANSFER_SOURCE", stealVehicleMessage))
            {
                TransferService.UpdateTransportCommandPath(stealVehicleMessage);
                TransferService.UpdateVehiclePath(stealVehicleMessage);
                RETURNS = 1; //true
            }
            else
            {
                FALSE_common_send_transfer_source(stealVehicleMessage);
            }
        }

        public void FALSE_common_send_transfer_source(VehicleMessageEx stealVehicleMessage)
        {
            TransferService.ChangeTransferCommandVehicleIdEmpty(stealVehicleMessage);
            ResourceService.ChangeVehicleTransportCommandIdEmpty(stealVehicleMessage);
            TransferService.ChangeTransportCommandStateToQueued(stealVehicleMessage);
            TransferService.UpdateVehicleAcsDestNodeIdEmpty(stealVehicleMessage);
            ResourceService.ChangeVehicleProcessStateToIdle(stealVehicleMessage);
            ResourceService.ChangeVehicleTransferStateToNotAssigned(stealVehicleMessage);
            ResourceService.ChangeVehicleConnectionStateToDisconnect(stealVehicleMessage);
            RETURNS = 0;
        }

        public void FALSE_CheckTransferCommand1(VehicleMessageEx vehicleMessage)
        {
            if (ResourceService.SearchWaitPoint(vehicleMessage))
            {
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Case SearchWaitPoint (RETURN TRUE)");
                InterfaceService.SendGoWaitpoint(vehicleMessage);
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Case SendGoWaitpoint : " + vehicleMessage.DestPortId);
                TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMessage);
            }
            else
            {
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Case SearchWaitPoint (RETURN FALSE)");
                InterfaceService.SendGoWaitpoint0000(vehicleMessage);
                logger.Debug("VEHICLE_DEPOSITCOMPLETED Abnormal Case SendGoWaitpoint0000 : " + vehicleMessage.DestPortId);
                TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMessage);
            }
            RETURNS = 1;
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
        }
    }
}
