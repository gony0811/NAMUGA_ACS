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
using ACS.Framework.Logging;
using System.Xml;
using ACS.Utility;

namespace ACS.Biz.Trans
{
    class RAIL_DESTOCCUPIED : BaseBizJob
    {
        public Logger logger = Logger.GetLogger(typeof(RAIL_DESTOCCUPIED));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_destoccupied = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            Boolean result;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();
            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_destoccupied);

            logger.Debug("TS RAIL_DESTOCCUPIED START========================================================");

            if (ResourceService.CheckVehicle(vehicleMsg))
            {
                logger.Debug("RAIL_DESTOCCUPIED Normal Sequence Step.01 CheckVehicle (TRUE)");

                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");

                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [EventTime] update");

                if (InterfaceService.CheckTransferCommand(vehicleMsg))
                {
                    logger.Debug("RAIL_DESTOCCUPIED Normal Sequence Step.02 CheckTransferCommand (TRUE)");
                    if (InterfaceService.CheckTransferCommand(vehicleMsg))
                    {
                        //2018.10.03 KSG

                        //TransferService.UpdateVehicleAcsDestNodeIdToDest(vehicleMsg);
                        //logger.Debug("update NA_R_VEHICLE [AcsDestNodeId] ='destNodeId'");
                        //logger.Debug("update NA_H_VEHICLEHISTORY");
                        //logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.4 UpdateVehicleAcsDestNodeIdToDest finish");

                        //COMMON_SEND_TRANSFER_DEST의 Rebply가 False일때 처리하는 로직을 
                        //COMMON_SEND_TRANSFER_DEST 안으로 이동시킴
                        //WorkflowManager.Execute("COMMON_SEND_TRANSFER_DEST", vehicleMsg);
                        //2018.09.24 LSJ
                        //Dest Occupied에 CANCEL 처리 삭제. M16, M26, M36 
                        //S0000으로 올라오는지 확인 필요. 

                        logger.Debug("RAIL_DESTOCCUPIED Normal Sequence Step.03 CheckTransferCommand (TRUE)");
                        ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = NOTASSIGNED");

                        MaterialService.DeleteCarrier(vehicleMsg);
                        logger.Debug(">>>>delete NA_M_CARRIER");

                        TransferService.DeleteTransportCommand(vehicleMsg);
                        logger.Debug(">>>>delete NA_T_TRANSPORTCMD");

                        TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = ' '");

                        TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId] = ' '");

                        TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [Path] = ' '");

                        ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [ProcessState] = IDLE");                       

                        if (ResourceService.SearchWaitPoint(vehicleMsg))
                        {                           
                            InterfaceService.SendGoWaitpoint(vehicleMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                        }
                        else
                        {                          
                            InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                        }

                        // 2020.08.15 KSG
                        // JOB CANCEL과 동시에 MOVECMD DESTCHANGE가 HOST로부터 수신되어 C-CODE 송신 순서가 바뀌는 문제로 JOB CANCEL 보고위치를 
                        // 가장뒤로 변경
                        logger.Debug("RAIL_DESTOCCUPIED Normal Sequence Step.04 requestAGVJobCancel");
                        InterfaceService.requestAGVJobCancel(vehicleMsg);
                                    
                    }
                    else //여기로 빠질수가 없음
                    {
                        logger.Debug("RAIL_DESTOCCUPIED Abnormal Case CheckTransferCommand (FALSE)");
                        if (ResourceService.SearchSuitableStockStationFromVehicle(vehicleMsg))
                        {
                            if (ResourceService.CheckTransportCommandDestNodeByVehicleCurrentNode(vehicleMsg))
                            {
                                InterfaceService.SendTransportMessageVehicleDestNode(vehicleMsg);
                                TransferService.UpdateVehicleAcsDestNodeIdByVehicleCurrentNodeId(vehicleMsg);
                            }
                            else
                            {
                                result = ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                                MaterialService.DeleteCarrier(vehicleMsg);
                                TransferService.DeleteTransportCommand(vehicleMsg);
                                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                                TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                                TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                                
                                //InterfaceService.requestAGVJobCancel(vehicleMsg);
                                if (ResourceService.SearchWaitPoint(vehicleMsg))
                                {
                                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                                    InterfaceService.SendGoWaitpoint(vehicleMsg);                                                                                     
                                }
                                else
                                {
                                    TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                                    InterfaceService.SendGoWaitpoint0000(vehicleMsg);                                   
                                }

                                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                                InterfaceService.requestAGVJobCancel(vehicleMsg);
                            }
                        }
                        else
                        {
                            result = ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                            MaterialService.DeleteCarrier(vehicleMsg);
                            TransferService.DeleteTransportCommand(vehicleMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                            TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                            TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                            ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                            InterfaceService.requestAGVJobCancel(vehicleMsg);
                            TransferService.CreateStockStationTransportCommand(vehicleMsg);
                            TransferService.ChangeTransferCommandVehicleId(vehicleMsg);
                            ResourceService.ChangeVehicleTransportCommandId(vehicleMsg);
                            TransferService.ChangeTransportCommandStateToAssigned(vehicleMsg);
                            ResourceService.ChangeVehicleProcessStateToRun(vehicleMsg);
                            TransferService.UpdateVehicleTransportCommand(vehicleMsg);
                            TransferService.UpdateVehiclePath(vehicleMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdByDest(vehicleMsg);
                            TransferService.CreateTransportCommandRequest(vehicleMsg);
                            InterfaceService.SendTransportMessageDest(vehicleMsg);

                            Thread.Sleep(5000);

                            if (TransferService.IsTransportCommandRequestReplied(vehicleMsg))
                            {

                            }
                            else
                            {
                                TransferService.CreateTransportCommandRequest(vehicleMsg);
                                InterfaceService.SendTransportMessageDest(vehicleMsg);

                                Thread.Sleep(5000);

                                if (TransferService.IsTransportCommandRequestReplied(vehicleMsg))
                                {

                                }
                                else
                                {
                                    TransferService.CreateTransportCommandRequest(vehicleMsg);
                                    InterfaceService.SendTransportMessageDest(vehicleMsg);

                                    Thread.Sleep(5000);

                                    if (TransferService.IsTransportCommandRequestReplied(vehicleMsg))
                                    {

                                    }
                                    else
                                    {
                                        TransferService.CreateTransportCommandRequest(vehicleMsg);
                                        InterfaceService.SendTransportMessageDest(vehicleMsg);

                                        Thread.Sleep(5000);

                                        if (TransferService.IsTransportCommandRequestReplied(vehicleMsg))
                                        {

                                        }
                                        else
                                        {
                                            TransferService.CreateTransportCommandRequest(vehicleMsg);
                                            InterfaceService.SendTransportMessageDest(vehicleMsg);

                                            Thread.Sleep(5000);

                                            if (TransferService.IsTransportCommandRequestReplied(vehicleMsg))
                                            {

                                            }
                                            else
                                            {
                                                result = ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                                                MaterialService.DeleteCarrier(vehicleMsg);
                                                TransferService.DeleteTransportCommand(vehicleMsg);
                                                ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMsg);
                                                TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                                                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                                                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg); ;
                                                ResourceService.ChangeVehicleConnectionStateToDisconnect(vehicleMsg);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    logger.Debug("RAIL_DESTOCCUPIED Abnormal Case CheckTransferCommand (FALSE)");
                    if (ResourceService.CheckVehicleFullStateEmpty(vehicleMsg))
                    {
                        if (ResourceService.SearchWaitPoint(vehicleMsg))
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
                    else
                    {
                        logger.Debug("TS RAIL_DESTOCCUPIED End========================================================");
                        return 0;
                    }
                }
            }
            else
            {
                logger.Debug("RAIL_DESTOCCUPIED Abnormal Case CheckVehicle (FALSE)");
                logger.Debug("TS RAIL_DESTOCCUPIED End========================================================");
                return 0;
            }
            logger.Debug("TS RAIL_DESTOCCUPIED End========================================================");
            return 0;
        }
    }
}
