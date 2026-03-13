using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Service;
using ACS.Communication.Socket.Model;
using Autofac;
using ACS.Communication.Socket;
namespace ACS.Biz.Ei.Common
{
    public class COMMON_START_NIO : BaseBizJob
    {
        public VehicleInterfaceServiceEx VehicleInterfaceService;

        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }


        public override int ExecuteJob(object[] args)
        {
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();

            VehicleInterfaceService.StartNio((Nio)args[0]);

            return 0;
        }
    }
}
