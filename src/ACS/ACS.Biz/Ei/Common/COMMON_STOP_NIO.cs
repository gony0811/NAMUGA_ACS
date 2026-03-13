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
namespace ACS.Biz.Ei.Common
{
    public class COMMON_STOP_NIO : BaseBizJob
    {
        private Dictionary<string, Tuple<Type, object>> commandJobList;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> CommandJobList
        { 
            get { return commandJobList;}  
            set { commandJobList = value;} 
        }

        //public bool Execute(string command, object args, bool isAsyncMode)
        public override int ExecuteJob(object[] args)
        {
            Nio nio = (Nio)args[0];
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();

            VehicleInterfaceService.StopNio(nio);
            return 0;
        }
    }
}
