using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Transfer.Model;
using ACS.Biz.Trans.Common;
using Spring.Context;


namespace ACS.Biz.Trans.Daemon
{
    public class SCHEDULE_CALLIDLEVEHICLE : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;        

        public override int ExecuteJob(object[] args)
        {
            XmlDocument SCHEDULE_CALLIDLEVEHICLE = (XmlDocument)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");

            VehicleMessageEx vehicleMessage = InterfaceService.CreateVehicleMessageFromDaemon(SCHEDULE_CALLIDLEVEHICLE);

            if(ResourceService.GetSuitableVehicleToSWaitP(vehicleMessage))
            {
                if(ResourceService.SearchSWaitPoint(vehicleMessage))
                {
                    InterfaceService.SendGoWaitpoint(vehicleMessage);
                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMessage);
                    ResourceService.ChangeVehicleProcessingStateToParking(vehicleMessage);
                }
            }
            return 0;
        }
    }
}
