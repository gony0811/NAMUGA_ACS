using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Context;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using System.Xml;
using log4net;
namespace ACS.Biz.Ei.Trans
{
    class RAIL_CARRIERTRANSFER : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(RAIL_CARRIERTRANSFER));
        public InterfaceServiceEx InterfaceService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public ResourceServiceEx ResourceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            logger.Debug("ES RAIL_CARRIERTRANSFER Start=============================================");
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");

            VehicleMessageEx vehiclemsg = InterfaceService.CreateVehicleMessageFromTrans(document);
            logger.Debug("RAIL_CARRIERTRANSFER Normal Sequence Step.1 CreateVehicleMessageFromTrans");

            ResourceService.CheckCCodeTypeByNodeId(vehiclemsg);

            VehicleInterfaceService.SendTransferCommand(vehiclemsg);
            logger.Debug("RAIL_CARRIERTRANSFER Normal Sequence Step.2 SendTransferCommand(CODE_C) to AGV");
            logger.Debug("ES RAIL_CARRIERTRANSFER End=============================================");
            return 0;
        }
    }
}
