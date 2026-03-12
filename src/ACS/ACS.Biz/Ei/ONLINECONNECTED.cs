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
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using System.Xml;

namespace ACS.Biz.Ei
{
    public class ONLINECONNECTED : BaseBizJob
    {
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public HistoryServiceEx HistoryService;

        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            HistoryService = (HistoryServiceEx)ApplicationContext.GetObject("HistoryService");

            Nio nio = (Nio)args[0];
            VehicleInterfaceService.UpdateNioStateConnect(nio);

            HistoryService.CreateNioHistory(nio);
            return 0;
        }
    }
}
