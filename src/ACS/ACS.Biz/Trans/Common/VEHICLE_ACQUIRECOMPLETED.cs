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
using System.Xml;
using Autofac;
using ACS.Framework.Logging;
namespace ACS.Biz.Trans.Common
{
    public class VEHICLE_ACQUIRECOMPLETED : BaseBizJob
    {
        protected static Logger logger = Logger.GetLogger(typeof(VEHICLE_ACQUIRECOMPLETED));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        private IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("TS VEHICLEACQUIRECOMPLETED START==========================================");
            VehicleMessageEx vehicleMessage = (VehicleMessageEx)args[0];

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            if(InterfaceService.CheckVehicle(vehicleMessage))
            {
                logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.1 CheckVehicle finish(True)");

                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMessage);
                logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] =''");
                logger.Debug(">>>>update NA_H_VEHICLEHISTORY");

                ResourceService.ChangeVehicleTransferStateToAcquireComplete(vehicleMessage);
                logger.Debug(">>>>update NA_R_VEHICLE [TransferState] ='ACQUIRE_COMPLETE'");
                logger.Debug(">>>>update NA_H_VEHICLEHISTORY");

                TransferService.ChangeTransportCommandStateToTransferingDest(vehicleMessage);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] ='TRANSFERRING_DEST'");
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [LoadedTime] ='LoadedTime'");


                if(InterfaceService.CheckTransferCommand(vehicleMessage))
                {
                    logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.2 CheckTransferCommand finish(True)");

                    InterfaceService.ReportAGVloadComplete(vehicleMessage);
                    logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.3 ReportAGVloadComplete(PICKUP) to HOST finish");

                    TransferService.UpdateVehicleAcsDestNodeIdToDest(vehicleMessage);
                    logger.Debug("update NA_R_VEHICLE [AcsDestNodeId] ='destNodeId'");
                    logger.Debug("update NA_H_VEHICLEHISTORY");
                    logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.4 UpdateVehicleAcsDestNodeIdToDest finish");

                    //COMMON_SEND_TRANSFER_DEST의 Rebply가 False일때 처리하는 로직을 
                    //COMMON_SEND_TRANSFER_DEST 안으로 이동시킴
                    WorkflowManager.Execute("COMMON_SEND_TRANSFER_DEST", vehicleMessage);
                    logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.5 MOVE TO COMMON_SEND_TRANSFER_DEST finish");
                }
                else
                {
                    logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case CheckTransferCommand finish(False)");
                    if(ResourceService.SearchWaitPoint(vehicleMessage))
                    {
                        logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case SearchWaitPoint finish(true)");

                        InterfaceService.SendGoWaitpoint(vehicleMessage);
                        logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case SendGoWaitpoint to AGV finish");

                        TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMessage);
                        logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] ='destNodeId");
                        logger.Debug(">>>>update NA_H_VEHICLEHISTORY");
                        logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case UpdateVehicleAcsDestNodeIdToWaitPoint finish");
                    }
                    else
                    {
                        logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case SearchWaitPoint finish(False)");

                        InterfaceService.SendGoWaitpoint0000(vehicleMessage);
                        logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case SendGoWaitpoint0000 to AGV finish");

                        TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMessage);
                        logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] =''9000'");
                        logger.Debug(">>>>update NA_H_VEHICLEHISTORY");
                        logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case UpdateVehicleAcsDestNodeIdTo0000 finish");
                    }
                    ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [TransferState] ='NOTASSIGNED'");
                    logger.Debug(">>>>update NA_H_VEHICLEHISTORY");
                    logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case ChangeVehicleTransferStateToNotAssigned finish");

                    //1023 LSJ
                    ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);

                    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
                    logger.Debug(">>>>update NA_R_VEHICLE [ProcessingState] ='IDLE'");
                    logger.Debug(">>>>update NA_H_VEHICLEHISTORY");
                    logger.Debug("VEHICLEACQUIRECOMPLETED Abnormal Case ChangeVehicleProcessStateToIdle finish");
                }
            }
            else
            {
                //Terminate function 추가 필요
            }

            logger.Debug("TS VEHICLEACQUIRECOMPLETED End==========================================");
            return 0;

        }
    }
}
