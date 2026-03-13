using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using System.Xml;
using Autofac;


namespace ACS.Biz.Ei.Trans
{
    class RAIL_VEHICLEORDERCOMMAND : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();


            VehicleMessageEx vehiclemsg = InterfaceService.CreateVehicleMessageFromTrans(document);
            VehicleInterfaceService.SendVehicleOrderCommand(vehiclemsg);
            return 0;
        }
    }
}
