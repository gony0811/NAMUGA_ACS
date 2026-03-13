using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Autofac;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Logging;
using System.Xml;
using ACS.Utility;
using System.Xml.Serialization;

namespace ACS.Biz.Trans.Common
{
    public class COMMON_START_TS : BaseBizJob
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

        public COMMON_START_TS()
        {
            
        }

        public override int ExecuteJob(object[] args)
        {
            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();
            Logger.logManager = LifetimeScope.Resolve<ILogManager>();

            Logger.Info("TS START : ");

            return 0;


        }
    }
}
