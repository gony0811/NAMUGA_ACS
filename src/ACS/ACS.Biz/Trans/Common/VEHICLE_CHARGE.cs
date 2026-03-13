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
namespace ACS.Biz.Trans.Common
{
    public class VEHICLE_CHARGE : BaseBizJob
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
            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();

           if(ResourceService.SearchSuitableRechargeStation(vehicleMessage))
           {
               TransferService.CreateRechargeTransportCommand(vehicleMessage);
               TransferService.ChangeTransferCommandVehicleId(vehicleMessage);
               ResourceService.ChangeVehicleTransportCommandId(vehicleMessage);
               TransferService.ChangeTransportCommandStateToAssigned(vehicleMessage);
               TransferService.UpdateVehicleTransportCommand(vehicleMessage);
               InterfaceService.SendTransportMessageDest(vehicleMessage);
               TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);
           }
           else
           {
               //Terminate 펑션 추가 필요
           }
            return 0;

        }
    }
}
