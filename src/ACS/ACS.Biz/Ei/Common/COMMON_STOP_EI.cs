using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Service;
using ACS.Framework.Application.Model;
using System.Collections;
using ACS.Communication.Socket.Model;
using ACS.Workflow;
using Spring.Context;
namespace ACS.Biz.Ei.Common
{
    public class COMMON_STOP_EI : BaseBizJob
    {
        private Dictionary<string, Tuple<Type, object>> commandJobList;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        private IWorkflowManager WorkflowManager;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        { 
            get { return commandJobList;}  
            set { commandJobList = value;} 
        }

        //public bool Execute(string command, object args, bool isAsyncMode)
        public override int ExecuteJob(object[] args)    //매개변수 재확인 필요
        {
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");
            ///로직 재확인 필요..
            ArrayList nios = (ArrayList)VehicleInterfaceService.GetNioes();

            foreach (Nio nio in nios)
            {
                Object[] arg = {nio};
                this.WorkflowManager.Execute("COMMON_START_NIO", arg);
            }
            return 0;
        }
    }
}
