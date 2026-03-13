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
using Autofac;


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

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();

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
