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
using ACS.Framework.Host;
using ACS.Framework.Logging;
using System.Xml;
using Spring.Context;
using log4net;

namespace ACS.Biz.Host
{
    public class MOVECMD : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(MOVECMD));
        protected Logger eventLogger = Logger.GetLogger("EventLogger");
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public IHostMessageManager HostMessageManager;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;


        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("MOVECMD BIZ Start===============================");

            XmlDocument MOVECMD = (XmlDocument)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            HostMessageManager = (IHostMessageManager)ApplicationContext.GetObject("HostMessageManager");

            TransferMessageEx transferMsg = InterfaceService.CreateTransferMessage(MOVECMD);

            logger.Debug("MOVECMD Normal Sequence Step.1 CreateTransferMessage finish(TRUE)");

            if (InterfaceService.CheckTransferMessage(transferMsg))
            {
                logger.Debug("MOVECMD Normal Sequence Step.2 CheckTransferMessage finish(TRUE)");

                if (InterfaceService.CheckTransferCommandSourceDest(transferMsg))
                {
                    logger.Debug("MOVECMD Normal Sequence Step.3 CheckTransferCommandSourceDest finish(TRUE)");
                    TRUE_CheckTransferCommandSourceDest1(transferMsg);
                }
                else
                {
                    logger.Debug("*MOVECMD Abnormal Sequence Step.3 CheckTransferCommandSourceDest finish(False)");
                    FALSE_CheckTransferCommandSourceDest1(transferMsg);
                }
            }
            else
            {
                logger.Debug("*MOVECMD Abnormal Sequence Step.2 CheckTransferMessage finish(False)");
                InterfaceService.ReplyMoveCmdNak3(transferMsg);
            }
            logger.Debug("MOVECMD BIZ End===============================");
            return 0;
        }

        public void FALSE_CheckTransferCommandSourceDest1(TransferMessageEx transferMsg)
        {
            if (InterfaceService.IsSourceVehicle(transferMsg))
            {
                string eventMessage = string.Format("MOVECMD jobid = [{0}], vehicleid = [{1}]", transferMsg.TransportCommandId, transferMsg.SourceUnit);
                eventLogger.Warn(eventMessage);
                if (InterfaceService.CheckTransportCommandDest(transferMsg))
                {
                    if (InterfaceService.CheckTransportCommand(transferMsg))
                    {
                        if (MaterialService.CreateCarrier(transferMsg))
                        {
                            if (TransferService.SearchPathsAsSourceVehicle(transferMsg))
                            {
                                if (TransferService.CreateTransportCommandAsSourceVehicle(transferMsg))
                                {
                                    TRUE_CreateTransportCommandAsSourceVehicle1(transferMsg);
                                }
                                else
                                {
                                    MaterialService.DeleteCarrier(transferMsg);
                                    TransferService.DeleteTransportCommand(transferMsg);
                                    InterfaceService.ReplyMoveCmdNak3(transferMsg);
                                }
                            }
                            else
                            {
                                MaterialService.DeleteCarrier(transferMsg);
                                InterfaceService.ReplyMoveCmdNak3(transferMsg);
                            }
                        }
                        else { InterfaceService.ReplyMoveCmdNak3(transferMsg); }
                    }
                    else { InterfaceService.ReplyMoveCmdNak5(transferMsg); }
                }
                else { InterfaceService.ReplyMoveCmdNak3(transferMsg); }
            }
            else { InterfaceService.ReplyMoveCmdNak3(transferMsg); }
        }

        private void TRUE_CreateTransportCommandAsSourceVehicle1(TransferMessageEx transferMsg)
        {
            InterfaceService.ReplyMoveCmd(transferMsg);
            TransferService.ChangeTransferCommandVehicleId(transferMsg);
            ResourceService.ChangeVehicleTransportCommandId(transferMsg);
            ResourceService.ChangeVehicleProcessStateToRun(transferMsg);
            TransferService.UpdateVehicleTransportCommand(transferMsg);
            //TransferService.UpdateVehiclePath(transferMsg);
            TransferService.UpdateVehicleAcsDestNodeIdToDest(transferMsg);
            ResourceService.ChangeVehicleTransferStateToAcquireComplete(transferMsg);

            TransferService.CreateTransportCommandRequest(transferMsg);
            InterfaceService.SendTransportMessageDest(transferMsg);
            System.Threading.Thread.Sleep(5000);

            if (TransferService.IsTransportCommandRequestReplied(transferMsg)) { /*end????*/ }
            else
            {
                TransferService.CreateTransportCommandRequest(transferMsg);
                InterfaceService.SendTransportMessageDest(transferMsg);
                System.Threading.Thread.Sleep(5000);

                if (TransferService.IsTransportCommandRequestReplied(transferMsg)) { /*end????*/ }
                else
                {
                    TransferService.CreateTransportCommandRequest(transferMsg);
                    InterfaceService.SendTransportMessageDest(transferMsg);
                    System.Threading.Thread.Sleep(5000);

                    if (TransferService.IsTransportCommandRequestReplied(transferMsg)) { /*end????*/ }
                    else
                    {
                        TransferService.CreateTransportCommandRequest(transferMsg);
                        InterfaceService.SendTransportMessageDest(transferMsg);
                        System.Threading.Thread.Sleep(5000);

                        if (TransferService.IsTransportCommandRequestReplied(transferMsg)) { /*end????*/ }
                        else
                        {
                            TransferService.CreateTransportCommandRequest(transferMsg);
                            InterfaceService.SendTransportMessageDest(transferMsg);
                            System.Threading.Thread.Sleep(5000);
                            if (TransferService.IsTransportCommandRequestReplied(transferMsg)) { /*end????*/ }
                            else
                            {
                                ResourceService.ChangeVehicleTransferStateToNotAssigned(transferMsg);
                                MaterialService.DeleteCarrier(transferMsg);
                                TransferService.DeleteTransportCommand(transferMsg);
                                ResourceService.ChangeVehicleTransportCommandIdEmpty(transferMsg);
                                TransferService.UpdateVehiclePathEmpty(transferMsg);
                                TransferService.UpdateVehicleAcsDestNodeIdEmpty(transferMsg);
                                ResourceService.ChangeVehicleProcessStateToIdle(transferMsg);
                                ResourceService.ChangeVehicleConnectionStateToDisconnect(transferMsg);
                            }
                        }
                    }

                }
            }
        }


        public void TRUE_CheckTransferCommandSourceDest1(TransferMessageEx transferMsg)
        {
            if (InterfaceService.CheckTransportCommand(transferMsg))
            {
                logger.Debug("MOVECMD Normal Sequence Step.4 CheckTransportCommand finish(TRUE)");

                if (MaterialService.CreateCarrier(transferMsg))
                {
                    logger.Debug(">>>>update NA_T_CARRIER");
                    logger.Debug("MOVECMD Normal Sequence Step.5 CreateCarrier finish(TRUE)");
                    if (TransferService.SearchPaths(transferMsg))
                    {
                        logger.Debug("MOVECMD Normal Sequence Step.6 SearchPaths finish(TRUE)");

                        if (TransferService.CreateTransportCommand(transferMsg))
                        {
                            logger.Debug(">>>>create NA_T_TRANSPORTCMD [JobType] = AUTOCALL");
                            logger.Debug("MOVECMD Normal Sequence Step.7 CreateTransportCommand finish(TRUE)");

                            TransferService.ChangeTransportCommandStateToQueued(transferMsg);
                            logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = QUEUED");
                            logger.Debug(">>>>update NA_T_TRANSPORTCMD [QueuedTime] = QueuedTime");
                            logger.Debug("MOVECMD Normal Sequence Step.8 ChangeTransportCommandStateToQueued finish");

                            InterfaceService.ReplyMoveCmd(transferMsg);
                            logger.Debug("MOVECMD Normal Sequence Step.9 ReplyMoveCmd to HOST finish :(MOVECMD_REP) " + transferMsg.MessageName);

                            //200819 KSG SPECIALCONFIG
                            if(ResourceService.CheckSpecialOrderBay(transferMsg))
                            {
                                //200520 LYS O-Code
                                TransferService.UpdateJobExpectedTimeWhenCreate(transferMsg);
                            }


                            logger.Debug("MOVECMD BIZ END");
                            //Terminate 펑션 확인 필요
                        }
                        else
                        {
                            logger.Debug("*MOVECMD Abnormal Sequence Step.7 CreateTransportCommand finish(FAIL)");

                            MaterialService.DeleteCarrier(transferMsg);
                            logger.Debug(">>>>delete NA_T_CARRIER");
                            logger.Debug("*MOVECMD Abnormal Sequence Step.8 DeleteCarrier finish");

                            TransferService.DeleteTransportCommand(transferMsg);
                            logger.Debug(">>>>delete NA_T_TRANSPORTCMD");
                            logger.Debug("*MOVECMD Abnormal Sequence Step.9 DeleteTransportCommand finish");

                            InterfaceService.ReplyMoveCmdNak3(transferMsg);
                            logger.Debug("*MOVECMD Abnormal Sequence Step.10 ReplyMoveCmdNak3 finish");
                        }

                    }
                    else
                    {
                        logger.Debug("*MOVECMD Abnormal Sequence Step.6 SearchPaths finish(FALSE)");

                        MaterialService.DeleteCarrier(transferMsg);
                        logger.Debug("*MOVECMD Abnormal Sequence Step.7 DeleteCarrier finish");

                        InterfaceService.ReplyMoveCmdNak3(transferMsg);
                        logger.Debug("*MOVECMD Abnormal Sequence Step.8 ReplyMoveCmdNak3 finish");
                    }
                }
                else
                {
                    InterfaceService.ReplyMoveCmdNak3(transferMsg);
                    logger.Debug("*MOVECMD Abnormal Sequence Step.5 ReplyMoveCmdNak3 finish");
                }
            }
            else
            {
                InterfaceService.ReplyMoveCmdNak5(transferMsg);
                logger.Debug("*MOVECMD Abnormal Sequence Step.5 ReplyMoveCmdNak5 finish");

            }
        }
    }
}
