using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ACS.Application;
using Spring.Context;

namespace ACS.StartUp
{
    public partial class ACSControlServer : ServiceBase
    {
        private Executor executor = null;

        public ACSControlServer()
        {
            InitializeComponent();            
        }

        protected override void OnStart(string[] args)
        {
            //Release below comments when need to debug service mode
            System.Diagnostics.Debugger.Launch();
            executor = new Executor();
            executor.Start();

        }

        protected override void OnStop()
        {
            executor.Stop();
            executor = null;
        }

        protected override void OnPause()
        {
            base.OnPause();
        }
    }
}
