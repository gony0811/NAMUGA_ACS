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
    public class MOVEUPDATE : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(MOVEUPDATE));
        public InterfaceServiceEx InterfaceService ;
        public ResourceServiceEx ResourceService ;
        public MaterialServiceEx MaterialService  ;
        public TransferServiceEx TransferService ;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument MOVEUPDATE = (XmlDocument)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");

            TransferMessageEx transferMsg = InterfaceService.CreateTransferMessage(MOVEUPDATE);

            logger.Debug("MOVEUPDATE Normal Sequence START ------------------------------------");
            if (InterfaceService.CheckTransferMessage(transferMsg))
            {
                logger.Debug("MOVEUPDATE Normal Sequence Step.01 CheckTransferMessage (TRUE)");
                if(InterfaceService.IsSourceVehicle(transferMsg))
                {
                    logger.Debug("MOVEUPDATE Normal Sequence Step.02 IsSourceVehicle (TRUE)");
                    if (InterfaceService.CheckTransportCommandDest(transferMsg))
                    {
                        logger.Debug("MOVEUPDATE Normal Sequence Step.03 CheckTransportCommandDest (TRUE)");
                        if (InterfaceService.CheckTransportCommand(transferMsg))
                        {
                            logger.Debug("MOVEUPDATE Normal Sequence Step.04 CheckTransportCommand (TRUE)");
                            if (TransferService.SearchPathsAsSourceVehicle(transferMsg))
                            {
                                logger.Debug("MOVEUPDATE Normal Sequence Step.05 SearchPathsAsSourceVehicle (TRUE)");
                                if (ResourceService.IsPossibleChangeDestLocation(transferMsg))
                                {
                                    // Add 20190813
                                    if (!ResourceService.CheckVehicleTransferStateIsAccqureComplete(transferMsg))
                                    {
                                        TransferService.UpdateTransportCommandDest(transferMsg); //200218 LYS Transfercommand is "Assigned~SourceArrived", ReplyMoveupdate change Dest
                                        logger.Debug(">>>>update NA_T_TRANSPORTCMD [AdditionalInfo] update");
                                        InterfaceService.ReplyMoveUpdate(transferMsg);
                                        logger.Debug("MOVEUPDATE Normal Sequence Step.06-1 ReplyMoveUpdate finish");
                                    }
                                    else
                                    {
                                        logger.Debug("MOVEUPDATE Normal Sequence Step.06 IsPossibleChangeDestLocation (TRUE)");

                                        TransferService.UpdateTransportCommandNewDest(transferMsg);
                                        logger.Debug(">>>>update NA_T_TRANSPORTCMD [AdditionalInfo] update");
                                        TransferService.CreateTransportCommandRequest(transferMsg);
                                        logger.Debug(">>>>update NA_Q_TRANSPORTCMDREQUEST add");
                                        InterfaceService.SendTransportMessageNewDest(transferMsg);
                                        logger.Debug("MOVEUPDATE Normal Sequence Step.07 SendTransportMessageNewDest");

                                        //SJP 
                                        //System.Threading.Thread.Sleep(5000);

                                        //if(TransferService.IsTransportCommandRequestReplied(transferMsg)) 
                                        //{
                                        //    /*Terminate 함수 추가*/
                                        //    logger.Debug("MOVEUPDATE Normal Sequence Step.08 IsTransportCommandRequestReplied (TRUE)");
                                        //}
                                        //else
                                        //{
                                        //    TransferService.DeleteTransportCommandNewDest(transferMsg);
                                        //    InterfaceService.ReplyMoveUpdateNak99(transferMsg);
                                        //    logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak99");
                                        //}
                                    }
                                }
                                else
                                {
                                    InterfaceService.ReplyMoveUpdateNak99(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak99");
                                }
                            }
                            else
                            { InterfaceService.ReplyMoveUpdateNak3(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak3"); }
                        }
                        else
                        { InterfaceService.ReplyMoveUpdateNak5(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak5"); }
                    }
                    else
                    { InterfaceService.ReplyMoveUpdateNak3(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak3"); }
                }
                else
                { InterfaceService.ReplyMoveUpdateNak3(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak3"); }
            }
            //200218 LYS Transfercommand is "Queued" 
            else if (ResourceService.IsPossibleChangeDestLocation(transferMsg))
            {
                if (!ResourceService.CheckVehicleTransferStateIsAccqureComplete(transferMsg))
                {
                    TransferService.UpdateTransportCommandDest(transferMsg);
                    logger.Debug(">>>>update NA_T_TRANSPORTCMD [AdditionalInfo] update");
                    InterfaceService.ReplyMoveUpdate(transferMsg);
                    logger.Debug("MOVEUPDATE Normal Sequence Step.06-1 ReplyMoveUpdate finish");
                }
                else
                { InterfaceService.ReplyMoveUpdateNak3(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak3"); }
            }
            else
            { InterfaceService.ReplyMoveUpdateNak3(transferMsg); logger.Debug("MOVEUPDATE Abnormal Case ReplyMoveUpdateNak3"); }
            return 0;
        }
    }
}
