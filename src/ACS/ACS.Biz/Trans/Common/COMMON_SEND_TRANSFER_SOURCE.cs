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
using Spring.Context;
namespace ACS.Biz.Trans.Common
{
    public class COMMON_SEND_TRANSFER_SOURCE : BaseBizJob
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

        //
        public override int ExecuteJob(object[] args)  
        {
            bool end = false;
            int count=0;
            int while_linit=10;  //1019 LSJ 3->10
            VehicleMessageEx vehicleMessage = (VehicleMessageEx)args[0];
            if (this.ApplicationContext == null) { this.ApplicationContext = (IApplicationContext)args[1]; }
           
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");

            while (!end)
            {
                TransferService.CreateTransportCommandRequest(vehicleMessage);
                InterfaceService.SendTransportMessageSource(vehicleMessage);

                System.Threading.Thread.Sleep(2000);

                if(TransferService.IsTransportCommandRequestReplied(vehicleMessage))
                {
                    //Expression 식 확인 필요
                    end = true;
                    return 1;
                }
                else
                {
                    //Expression 식 확인 필요  while_linit  ?????????????
                    end = DataHandlingService.Greater(count, while_linit);
                    count++;
                }
            }            
            return 0;
        }
    }
}
