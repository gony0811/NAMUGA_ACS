using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using System.Xml;
using Spring.Context;
namespace ACS.Biz.Ei.Trans
{
    class RAIL_VEHICLELGOCOMMAND : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");


            VehicleMessageEx vehiclemsg = InterfaceService.CreateVehicleMessageFromTrans(document);
            VehicleInterfaceService.SendVehicleGoCommand(vehiclemsg);
            return 0;
        }
    }
}
