using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using System.Xml;
using Autofac;
using ACS.Framework.Logging;
namespace ACS.Biz.Trans.Common
{
    public class COMMON_SEND_TRANSFER_DEST : BaseBizJob
    {
        protected static Logger logger = Logger.GetLogger(typeof(COMMON_SEND_TRANSFER_DEST));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args) //public bool Execute(VehicleMessageEx vehicleMessage)
        {
           
            bool end = false;
            bool result = false;
            int count = 0;
            int while_linit = 5;
            VehicleMessageEx vehicleMessage = (VehicleMessageEx)args[0];

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();


            while (!end)
            {
                logger.Debug("TS COMMON_SEND_TRANSFER_DEST START========================================================");
                TransferService.CreateTransportCommandRequest(vehicleMessage);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [JobID][VehicleID][CreateTime] update ");
                logger.Debug("COMMON_SEND_TRANSFER_DEST Normal Sequence Step.1 CreateTransportCommandRequest finish");

                InterfaceService.SendTransportMessageDest(vehicleMessage);
                logger.Debug("COMMON_SEND_TRANSFER_DEST Normal Sequence Step.2 SendTransportMessageDest to ES finish");

                
                Thread.Sleep(1000);       //200508 KKH Loading Complete 후 C Code 발신. 1초 대기 후 재발신 변경

                InterfaceService.SendTransportMessageDest(vehicleMessage); 

                //if (TransferService.IsTransportCommandRequestReplied(vehicleMessage))
                {
                    result = true;
                    end = true;
                }
                //else
                //{
                //    end = DataHandlingService.Greater(count, while_linit);
                //    count++;                  
                //}
                TransferService.DeleteTransportCommandRequest(vehicleMessage);
            }


            if(result)
            {
                logger.Debug("COMMON_SEND_TRANSFER_DEST Normal Sequence Step.3 SendTransportMessageDest finish(TRUE)");
                logger.Debug("TS COMMON_SEND_TRANSFER_DEST End========================================================");
                return 0;
            }
            else
            {
                logger.Debug("COMMON_SEND_TRANSFER_DEST Abnormal Case SendTransportMessageDest finish(TRUE)");

                ResourceService.ChangeVehicleConnectionStateToDisconnect(vehicleMessage);
                logger.Debug(">>>>update NA_R_VEHICLE [connectionState] ='DISCONNECT'");
                logger.Debug(">>>>update NA_H_VEHICLEHISTORY");
                logger.Debug("COMMON_SEND_TRANSFER_DEST Abnormal Case ChangeVehicleConnectionStateToDisconnect finish");
            }
            logger.Debug("TS COMMON_SEND_TRANSFER_DEST End========================================================");
            return 0;
        }
    }
}
