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
using Autofac;
using ACS.Biz.Trans.Common;
namespace ACS.Biz.Trans.Daemon
{
    public class SCHEDULE_CHECKCROSSNODE : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        bool RETURNS = false;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument SCHEDULE_CHECKCROSSNODE = (XmlDocument)args[0];

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();

            VehicleMessageEx vehicleMessage = InterfaceService.CreateVehicleMessageFromDaemon(SCHEDULE_CHECKCROSSNODE);
            if(ResourceService.CheckVehicleCrossWaitToGO(vehicleMessage))
            {
                ResourceService.ChangeVehiclesCrossWaitToGOING(vehicleMessage);
                InterfaceService.SendPermitMessageToWaitVehicles(vehicleMessage);

                System.Threading.Thread.Sleep(1000);
                InterfaceService.SendPermitMessageToWaitVehicles(vehicleMessage);
            }
            else
            {
                //Terminate 함수 필요
            }
            return 0 ;
        }

    }
}
