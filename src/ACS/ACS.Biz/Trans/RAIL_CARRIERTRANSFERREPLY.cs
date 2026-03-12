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
    class RAIL_CARRIERTRANSFERREPLY : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(RAIL_CARRIERTRANSFERREPLY));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("TS RAIL_CARRIERTRANSFERREPLY Start======================================");
            XmlDocument rail_carriertransferreply = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            Boolean iCount;
            Boolean result;

            bool isOCodeBay = false;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_carriertransferreply);

            //LSJ TransportRequest Job 삭제 처리 ES로 이동
            /*
            TransferService.DeleteTransportCommandRequest(vehicleMsg);
            logger.Debug(">>>>delete NA_Q_TRANSPORTCMDREQUEST");
            */

            TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
            logger.Debug(">>>>update NA_T_TRANSPORTCMD VehicleEvent : " + vehicleMsg.MessageName);

            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg); 
            logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");

            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [EventTime]");

            isOCodeBay = ResourceService.CheckSpecialOrderBay(vehicleMsg);


            if(isOCodeBay)
            {
                VehicleMessageEx newOrderVehicleMessage = TransferService.CalculatePathAndTimeFirstTime(vehicleMsg);
                if(newOrderVehicleMessage != null)
                {
                    InterfaceService.SendVehicleOrderMessage(newOrderVehicleMessage);
                }
            }



            if(TransferService.CheckTransportCommandsByVehicle(vehicleMsg))
            {
                logger.Debug("RAIL_CARRIERTRANSFERREPLY Normal Sequence Step.1 CheckTransportCommandsByVehicle finish(true)");
                if(TransferService.IsNewDestTransferReply(vehicleMsg))
                {
                    logger.Debug("RAIL_CARRIERTRANSFERREPLY Normal Sequence Step.2 IsNewDestTransferReply finish(true)");

                    result = TransferService.ChangeTransportCommandDestByNewDest(vehicleMsg);
                    logger.Debug(">>>>update NA_T_TRANSPORTCMD [dest],[description] update");

                    InterfaceService.ReplyMoveUpdate(vehicleMsg);
                    logger.Debug("RAIL_CARRIERTRANSFERREPLY Normal Sequence Step.3 ReplyMoveUpdate finish");
                    TransferService.UpdateVehicleAcsDestNodeIdToDest(vehicleMsg);
                    logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] update");
                    TransferService.DeleteTransportCommandNewDest(vehicleMsg);
                    logger.Debug(">>>>update NA_T_TRANSPORTCMD [AdditionalInfo]=' '");
                }
                else
                {
                    if(TransferService.CheckIfReportAGVJOBREPORTOrNot(vehicleMsg))
                    {
                        logger.Debug("RAIL_CARRIERTRANSFERREPLY Normal Sequence Step.2 CheckIfReportAGVJOBREPORTOrNot finish(true)");

                        InterfaceService.ReportAGVReplyCommand(vehicleMsg);
                        logger.Debug("RAIL_CARRIERTRANSFERREPLY Normal Sequence Step.3 ReportAGVReplyCommand(JOBSTART) to HOST  finish");

                        TransferService.UpdateTransportCommandStartedTime(vehicleMsg);
                        logger.Debug(">>>>update NA_T_TRANSPORTCMD StartedTime : transportCommand.StartedTime");

                        iCount = ResourceService.ChangeVehicleTransferStateToAssigned(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = ASSIGNED");
                    }
                    if(ResourceService.CheckVehicleExDestNodeIdByVehicle(vehicleMsg)) //이 함수가 맞나?? java랑 함수명이 다름 resourceService checkVehicleAcsDestNodeIdByVehicle
                    {
                        logger.Debug("RAIL_CARRIERTRANSFERREPLY Abnormal Case CheckVehicleExDestNodeIdByVehicle finish(true)");
                        logger.Debug("TS RAIL_CARRIERTRANSFERREPLY End=========================================");
                        return 0;
                    }
                    else
                    {
                        //AGV의 목적지 정보가 달라져 있을 경우, 다시한번 명령
                        logger.Debug("RAIL_CARRIERTRANSFERREPLY Abnormal Case CheckVehicleExDestNodeIdByVehicle finish(false)");

                        InterfaceService.SendTransportMessageVehicleDestNode(vehicleMsg);
                        logger.Debug("RAIL_CARRIERTRANSFERREPLY Abnormal Case : One more Send TransportMessageVehicleDestNode to ES finish");
                    }
                }

            }
            else
            {
                logger.Debug("TS RAIL_CARRIERTRANSFERREPLY End========================================");
                return 0;
            }

            logger.Debug("TS RAIL_CARRIERTRANSFERREPLY End========================================");
            return 0;

        }
    }
}
