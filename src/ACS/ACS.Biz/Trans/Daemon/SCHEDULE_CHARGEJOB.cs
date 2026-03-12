using System;
using System.Xml;
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
using Spring.Context;
using ACS.Biz.Trans.Common;
namespace ACS.Biz.Trans.Daemon
{
    public class SCHEDULE_CHARGEJOB : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        private IWorkflowManager WorkflowManager;
        bool RETURNS = false;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {           
            XmlDocument SCHEDULE_CHARGEJOB = (XmlDocument)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");


            VehicleMessageEx vehicleMessage = InterfaceService.CreateVehicleMessageFromDaemon(SCHEDULE_CHARGEJOB);

            //KSB
            //RGV 충전잡 실행전 해당 Bay에 Queue 잡이 있는지 확인필요
            if (vehicleMessage.BayId.Equals("AMT-LFC(A)", StringComparison.OrdinalIgnoreCase)    ||
                vehicleMessage.BayId.Equals("AMT(A)-LFC(A)", StringComparison.OrdinalIgnoreCase) ||
                vehicleMessage.BayId.Equals("AMT-LFC(B)", StringComparison.OrdinalIgnoreCase)    ||
                vehicleMessage.BayId.Equals("AMT(B)-LFC(B)", StringComparison.OrdinalIgnoreCase))
            {
                if (ResourceService.GetQueuedTransportCommandbyBayId_RGV(vehicleMessage))
                {
                    //RGV 해당 Bay의 Queue에 Job 있으면 빠져 나감
                    return 0;
                }
            }

            if (ResourceService.SearchSuitableRechargeStationWithAGV(vehicleMessage))
            {
                Logger.Debug("TS SCHEDULE_CHARGEJOB START========================================================");
                Logger.Debug("TS SCHEDULE_CHARGEJOB - SearchSuitableRechargeStation(vehicleMessage)");
                //if (ResourceService.SearchSuitableChargeVehicle(vehicleMessage))
                //{
                //    Logger.Debug("TS SCHEDULE_CHARGEJOB - SearchSuitableChargeVehicle(vehicleMessage)");
                    if (TransferService.CreateRechargeTransportCommand(vehicleMessage))
                    {
                        Logger.Debug("TS SCHEDULE_CHARGEJOB - CreateRechargeTransportCommand(vehicleMessage)");
                        TransferService.ChangeTransferCommandVehicleId(vehicleMessage);
                        ResourceService.ChangeVehicleTransportCommandId(vehicleMessage);
                        TransferService.ChangeTransportCommandStateToAssigned(vehicleMessage);
                        ResourceService.ChangeVehicleProcessStateToRun(vehicleMessage);
                        TransferService.UpdateVehicleTransportCommand(vehicleMessage);

                        //TTTTTTTTTTTTTTTTTTT
                        TransferService.UpdateVehiclePath(vehicleMessage);
                        TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);

                        WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");
                        Object[] arg = { vehicleMessage };
                        if (WorkflowManager.Execute("COMMON_SEND_TRANSFER_DEST", arg))
                        {
                            Logger.Debug("TS SCHEDULE_CHARGEJOB End========================================================");
                            //   //Terminate 함수 필요
                        }
                        else
                        {
                            Logger.Debug("TS SCHEDULE_CHARGEJOB FAIL=======================================================");
                            ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
                            TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMessage);
                            TransferService.UpdateVehiclePathEmpty(vehicleMessage);
                            ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMessage);
                            TransferService.DeleteTransportCommand(vehicleMessage);
                            ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
                            ResourceService.ChangeVehicleConnectionStateToDisconnect(vehicleMessage);
                        }
                }
                else
                {
                    //Terminate 함수 필요
                }
            //}
            //    else
            //    {
            //        //Terminate 함수 필요
            //    }
            }
            else
            {
                //Terminate 함수 필요
            }

            return 0 ;
        }

    }
}
