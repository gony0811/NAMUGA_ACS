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
using System.Xml;

namespace ACS.Biz.Trans.Common
{
    public class COMMON_SENDPERMIT : BaseBizJob
    {
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

        public override int ExecuteJob(object[] args)
        {
            VehicleMessageEx vehicleMessage = (VehicleMessageEx)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");


            InterfaceService.SendVehiclePermitMessage(vehicleMessage);
            System.Threading.Thread.Sleep(1000);//1초 간격으로 명령 여러번 내리기

            InterfaceService.SendVehiclePermitMessage(vehicleMessage);
            System.Threading.Thread.Sleep(1000);//1초 간격으로 명령 여러번 내리기

            InterfaceService.SendVehiclePermitMessage(vehicleMessage);
            System.Threading.Thread.Sleep(1000);//1초 간격으로 명령 여러번 내리기

            InterfaceService.SendVehiclePermitMessage(vehicleMessage);
            System.Threading.Thread.Sleep(1000);//1초 간격으로 명령 여러번 내리기

            InterfaceService.SendVehiclePermitMessage(vehicleMessage);

            //Terminate function 추가 필요
            return 0;

        }
    }
}
