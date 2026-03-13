using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Autofac;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Workflow;

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_DESTPIOCONNECTERROR : BaseBizJob
    {
        private IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_destoccupied = (XmlDocument)args[0];

            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            this.WorkflowManager.Execute("RAIL_DESTOCCUPIED", rail_destoccupied);

            return 0;
        }
    }
}
