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
using System.Threading;
namespace ACS.Biz.Ei.Trans
{
    class RAIL_RESTARTNIO : BaseBizJob
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
            VehicleInterfaceService.SendNioHeartCode(vehiclemsg);

            Thread.Sleep(2000);//HeartCode timeout

            if(VehicleInterfaceService.IsExistTransactionId(vehiclemsg))
            {
                VehicleInterfaceService.DeleteTransactionId(vehiclemsg);
                InterfaceService.SendVehicleMessageInformHeartCodeTimeout(vehiclemsg);
                VehicleInterfaceService.RestartNio(vehiclemsg);
            }

            //Terminate funcgtion 추가 필요

            return 0;
        }
    }
}
