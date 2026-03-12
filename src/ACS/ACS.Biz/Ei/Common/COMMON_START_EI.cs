using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Service;
using ACS.Biz.Ei.Common;
using ACS.Framework.Application.Model;
using ACS.Communication.Socket.Model;
using System.Collections;
using ACS.Workflow;
using Spring.Context;
namespace ACS.Biz.Ei.Common
{
    public class COMMON_START_EI : BaseBizJob
    {
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        private IWorkflowManager WorkflowManager;

        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args) 
        {
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");

            ArrayList nioes = (ArrayList)VehicleInterfaceService.GetNioes();

            foreach (Nio nio in nioes)
            {
                Object[] arg = { nio };
                this.WorkflowManager.Execute("COMMON-START-NIO", nio);
            }

            //this.WorkflowManager.Start();

            return 0;
        }
    }
}
///
