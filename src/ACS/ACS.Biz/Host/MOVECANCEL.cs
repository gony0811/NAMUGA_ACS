using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using System.Xml;
using Spring.Context;
using log4net;
namespace ACS.Biz.Host
{
    public class MOVECANCEL : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(MOVECANCEL));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args) 
        {
            XmlDocument MOVECANCEL = (XmlDocument)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");

            logger.Debug("MOVECANCEL Normal Sequence START ------------------------------------");
            TransferMessageEx transferMsg = InterfaceService.CreateTransferMessage(MOVECANCEL);

            if (InterfaceService.CheckAndPopulateTransportCommand(transferMsg))
            {
                logger.Debug("MOVECANCEL Normal Sequence Step.01 CheckAndPopulateTransportCommand (RETURN TRUE)");
                if (TransferService.ExistTransportCommand(transferMsg))
                {
                    logger.Debug("MOVECANCEL Normal Sequence Step.02 ExistTransportCommand (RETURN TRUE)");
                    if (ResourceService.CheckDoMoveCancel(transferMsg))
                    {
                        logger.Debug("MOVECANCEL Normal Sequence Step.03 CheckDoMoveCancel (RETURN TRUE)");
                        if (ResourceService.CheckVehicleByTransportCommand(transferMsg))
                        {
                            logger.Debug("MOVECANCEL Normal Sequence Step.04 CheckVehicleByTransportCommand (RETURN TRUE)");
                            if (ResourceService.IsPossibleChangeDestLocation(transferMsg))
                            {
                                logger.Debug("MOVECANCEL Normal Sequence Step.05 IsPossibleChangeDestLocation (RETURN TRUE)");
                                TransferService.UpdateVehicleTransportCommandEmpty(transferMsg);
                                logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId]=' '");

                                TransferService.UpdateVehiclePathEmpty(transferMsg);
                                logger.Debug(">>>>update NA_R_VEHICLE [Path]=' '");

                                TransferService.UpdateVehicleAcsDestNodeIdEmpty(transferMsg);
                                logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId]=' '");

                                ResourceService.ChangeVehicleTransferStateToNotAssigned(transferMsg);
                                logger.Debug(">>>>update NA_R_VEHICLE [TransferState]='NOTASSIGN'");

                                MaterialService.DeleteCarrierByCommand(transferMsg);
                                logger.Debug(">>>>delete NA_M_CARRIER");

                                TransferService.DeleteTransportCommand(transferMsg);
                                logger.Debug(">>>>delete NA_T_TRANSPORTCMD");

                                InterfaceService.requestAGVJobCancel(transferMsg);
                                logger.Debug("MOVECANCEL Normal Sequence Step.7 requestAGVJobCancel (TRSJOBREPORT : CANCEL)");

                                if (ResourceService.SearchWaitPoint(transferMsg))
                                {
                                    logger.Debug("MOVECANCEL Normal Sequence Step.8 SearchWaitPoint (RETURN TRUE)");

                                    InterfaceService.SendGoWaitpoint(transferMsg);
                                    logger.Debug("MOVECANCEL Normal Sequence Step.9 SendGoWaitpoint (TO ES)");

                                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(transferMsg);
                                    logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId]='WAIT_P'");
                                    logger.Debug("MOVECANCEL Normal Sequence Step.10 UpdateVehicleAcsDestNodeIdToWaitPoint (AcsDestNodeId : WAIT_P)");
                                }
                                else
                                {
                                    logger.Debug("MOVECANCEL Abmormal Case SearchWaitPoint (RETURN FALSE)");
                                    InterfaceService.SendGoWaitpoint0000(transferMsg);
                                    logger.Debug("MOVECANCEL Abmormal Case SendGoWaitpoint0000 (TO ES)");
                                    TransferService.UpdateVehicleAcsDestNodeIdTo0000(transferMsg);
                                    logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId]='9000'");
                                    logger.Debug("MOVECANCEL Abmormal Case UpdateVehicleAcsDestNodeIdTo0000 (AcsDestNodeId : 9000)");
                                }

                                ResourceService.ChangeVehicleProcessStateToIdle(transferMsg);
                                logger.Debug(">>>>update NA_R_VEHICLE [ProcessingState]='IDLE'");
                            }
                            else
                            {
                                logger.Debug("MOVECANCEL Abnormal Case IsPossibleChangeDestLocation (RETURN FALSE)");
                                //Terminate Function 추가 필요
                            }
                        }
                        else
                        {
                            logger.Debug("MOVECANCEL Abnormal Case CheckVehicleByTransportCommand (RETURN FALSE)");
                            MaterialService.DeleteCarrierByCommand(transferMsg);
                            logger.Debug("MOVECANCEL Abnormal Case DeleteCarrierByCommand");
                            TransferService.DeleteTransportCommand(transferMsg);
                            logger.Debug("MOVECANCEL Abnormal Case DeleteTransportCommand");
                            InterfaceService.requestAGVJobCancel(transferMsg);
                            logger.Debug("MOVECANCEL Abnormal Case requestAGVJobCancel (TRSJOBREPORT : CANCEL)");

                        }
                    }
                    else
                    {
                        //Terminate Function 추가 필요
                    }
                }
                else
                {
                    InterfaceService.requestAGVJobCancel(transferMsg);
                    //Terminate Function 추가 필요
                }

            }
            else
            {
                InterfaceService.requestAGVJobCancel(transferMsg);

                //Terminate Function 추가 필요
            }
            logger.Debug("MOVECANCEL END ------------------------------------");
            return 0;
        }
    }
}
