using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Spring.Context;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Workflow;

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_INFORM : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_inform = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            
            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_inform);

            ////20190906 KSB - Disconnect 발생과는 연관은 없지만...ㅠㅠ 일단 인폼 저장 막음
            //InterfaceService.CreateInformVehicleMismachNIO(vehicleMsg);

            return 0;
        }
    }
}
