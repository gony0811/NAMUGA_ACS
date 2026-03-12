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
using Spring.Context;
using ACS.Biz.Trans.Common;
namespace ACS.Biz.Trans.Daemon
{
    public class SCHEDULE_CHECKVEHICLES : BaseBizJob
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
        
        //public virtual int ExecuteJob(object[] args)
        public override int ExecuteJob(object[] args)
        {
            XmlDocument SCHEDULE_CHECKVEHICLES = (XmlDocument)args[0];

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");

            VehicleMessageEx vehicleMessage = InterfaceService.CreateVehicleMessageFromDaemon(SCHEDULE_CHECKVEHICLES);
            ResourceService.CheckVehiclesEventTime(vehicleMessage);
            ResourceService.ChangeVehiclesConnectionStateToDisconnect(vehicleMessage);
           
            return 0 ;
        }

    }
}
