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
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Resource.Model;
using ACS.Framework.Resource;
using ACS.Workflow;

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_CONVEYORLOADINGTIMEOUT : BaseBizJob
    {
        public ResourceServiceEx ResourceService;
        public InterfaceServiceEx InterfaceService;
        public IResourceManagerEx ResourceManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            ResourceManager = LifetimeScope.Resolve<IResourceManagerEx>();
            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();

            XmlDocument rail_conveyorloadingtimeout = (XmlDocument)args[0];            
            VehicleMessageEx vehicleMessage = InterfaceService.CreateVehicleMessageFromES(rail_conveyorloadingtimeout);
            


            String message = "VEHICLE [" + vehicleMessage.VehicleId + "] TAG=" + vehicleMessage.NodeId;
            String desc = "Dear AGV member.  Conveyor loading timeout problem has been encountered while loading source port!";
            LocationEx sourceLocation = ResourceManager.GetLocationByStationId(vehicleMessage.NodeId);
            string source = string.Empty;

            if(sourceLocation == null) 
            {
                source = vehicleMessage.VehicleId;
            }
            else
            {
                source = sourceLocation.PortId;
            }


            ResourceService.CreateUIInform(Inform.INFORM_TYPE_IMPORTANT, message, source, desc);


            return 0;
        }
    }
}
