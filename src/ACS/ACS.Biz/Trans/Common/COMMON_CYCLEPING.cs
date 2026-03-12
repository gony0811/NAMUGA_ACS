using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using System.Xml;
using Spring.Context;
namespace ACS.Biz.Trans.Common
{
    public class COMMON_CYCLEPING : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            Nio nio = (Nio)args[0];
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");

            InterfaceService.CyclePingTest(nio);
            return 0;
        }
    }
}
