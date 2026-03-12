using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Data;
using ACS.Communication;
using ACS.Communication.Msb;
using ACS.Framework.Base;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Reload;
using ACS.Framework.Message.Model.Control;
using ACS.Communication.Socket;
using ACS.Workflow;
using ACS.Utility;
using Spring.Context;
using Spring.Context.Support;
using ACS.Framework.Cache;

namespace ACS.Manager.Application
{
    public class ApplicationControlManagerImplement : AbstractManager, IApplicationControlManager
    {
        public int SleepIntervalWhenStop { get; set; }
        public long RunningBizProcessCheckIntervalWhenStop { get; set; }
        public long RunningBizProcessCheckRetryCountWhenStop { get; set; }
        public IApplicationContext ApplicationContext { get; set; }
        public IReloadableApplicationContextAware ReloadableApplicationContextAware { get; set; }
        public string ReloadableDirectory { get; set; }
        public string[] ReloadableAssemblyDefinitions { get; set; }
        public IWorkflowManager WorkflowManager { get; set; }
        public IApplicationManager ApplicationManager { get; set; }
        public ICacheManagerEx CacheManagerEx { get; set; }

        public ApplicationControlManagerImplement()
        {
            SleepIntervalWhenStop = 2000;
            RunningBizProcessCheckIntervalWhenStop = 1000L;
            RunningBizProcessCheckRetryCountWhenStop = 10;
        }

        public bool Control(ControlMessage controlMessage)
        {
            string messageName = controlMessage.MessageName;

            if(messageName.Equals("CONTROL-HEARTBEAT"))
            {
                HeartBeat(controlMessage);
            }
            else if(messageName.Equals("CONTROL-RELOADSERVICE"))
            {
                ReloadService(controlMessage);
            }
            else if (messageName.Equals("CONTROL-RELOADWORKFLOW"))
            {
                ReloadWorkflow(controlMessage);
            }
            else if (messageName.Equals("CONTROL-REFRESHCACHE"))
            {
                RefreshCache(controlMessage);
            }
            else if (messageName.Equals("CONTROL-STOP"))
            {
                Stop(controlMessage);
            }
            else if (messageName.Equals("CONTROL-GC"))
            {
                Gc(controlMessage);
            }
            else
            {
                logger.Warn("please check messageName, {" + messageName + "} can not be accepted");
                return false;
            }
            return true;
        }

        public bool HeartBeat(ControlMessage controlMessage)
        {
            return InvokeHeartBeat();
        }

        public bool RefreshCache(ControlMessage controlMessage)
        {
            return this.InvokeRefreshCache();
        }

        public bool ReloadService(ControlMessage controlMessage)
        {
            return InvokeReloadService();
        }

        public bool InvokeReloadService()
        {
            logger.Info("reloadService asked");

            if(this.ReloadableApplicationContextAware != null)
            {
                AbstractApplicationContext currentApplicationContext = (AbstractApplicationContext)this.ApplicationContext;

                IApplicationContext parentApplicationContext = currentApplicationContext.ParentContext;

                CustomClassLoader serviceLoadLoad = new CustomClassLoader(ReloadableDirectory);

                AbstractApplicationContext reloadableApplicationContext = new XmlApplicationContext(false, parentApplicationContext, this.ReloadableAssemblyDefinitions);

                this.ReloadableApplicationContextAware.SetApplicationContext(reloadableApplicationContext);

                this.ApplicationContext = reloadableApplicationContext;

                currentApplicationContext.Dispose();

                logger.Info("completed reloadService");
            }

            return true;
        }

        public bool ReloadWorkflow(ControlMessage controlMessage)
        {
            return InvokeReloadWorkflow();
        }

        public bool InvokeReloadWorkflow()
        {
            logger.Info("reloadWorkflow asked");

            if(this.WorkflowManager != null)
            {
                this.WorkflowManager.Reload();
                logger.Info("completed reloadworkflow");
            }

            return true;
        }

        public bool Stop(ControlMessage controlMessage)
        {
            return InvokeStop(controlMessage.ApplicationType, controlMessage.ApplicationName);
        }

        public bool InvokeStop(string applicationType, string applicationName)
        {
            long startTime = System.DateTime.Now.Millisecond;

            logger.Info("stop asked");

            try
            {
                DisposeMsbControllables();
                logger.Info("disposed msbControllables");
            }
            catch(Exception e)
            {
                logger.Error("failed to dispose msbControllables", e);
            }

            if (applicationType.Equals("ei"))
            {
                //DisposeSecsControllables();
            }

            try
            {
                ACS.Framework.Application.Model.Application application = this.ApplicationManager.GetApplication(applicationName);

                if(application != null)
                {
                    application.State = "inactive";
                    InvokeStopWorkflow(application);

                    ApplicationManager.UpdateApplication(application);
                }
            }
            catch (Exception e)
            {
                logger.Error("failed to invoke stopworkflow", e);
            }

            return true;
        }

        private void InvokeStopWorkflow(Framework.Application.Model.Application application)
        {
            if(this.WorkflowManager != null)
            {
                object[] args = { this.ApplicationContext, application };
                this.WorkflowManager.Execute(GetStopWorkflowName(application), args);
            }
        }

        private string GetStopWorkflowName(Framework.Application.Model.Application application)
        {
            string stopWorkflowName = "";

            if(application.Type.Equals("trans"))
            {
                stopWorkflowName = "COMMON-STOP-TRANS";
            }
            else if(application.Type.Equals("ei"))
            {
                stopWorkflowName = "COMMON-STOP-EI";
            }
            else if(application.Type.Equals("daemon"))
            {
                stopWorkflowName = "COMMON-STOP-DAEMON";
            }
            else if(application.Type.Equals("control"))
            {
                stopWorkflowName = "COMMON-STOP-CONTROL";
            }
            else if (application.Type.Equals("host"))
            {
                stopWorkflowName = "COMMON-STOP-HOST";
            }
            else if (application.Type.Equals("emulator"))
            {
                stopWorkflowName = "COMMON-STOP-EMULATOR";
            }
            else if (application.Type.Equals("report"))
            {
                stopWorkflowName = "COMMON-STOP-REPORT";
            }
            else if (application.Type.Equals("query"))
            {
                stopWorkflowName = "COMMON-STOP-QUERY";
            }
            else
            {
                String message = "please check process type, it should be{trans|daemon|control|host|emulator|report}";

                logger.Fatal(message);
            }
            return stopWorkflowName;

        }

        private void DisposeMsbControllables()
        {
            IApplicationContext currentApplicationContext = this.ApplicationContext;
            IDictionary currentControllables = currentApplicationContext.GetObjectsOfType(typeof(IControllable));
            foreach (object obj in currentControllables)
            {
                IControllable controllable = (IControllable)obj;
                if ((controllable is IMsbControllable))
                {
                    controllable.Stop();
                }
            }

            while (currentApplicationContext.ParentContext != null)
            {
                currentApplicationContext = currentApplicationContext.ParentContext;
                IDictionary parentControllables = currentApplicationContext.GetObjectsOfType(typeof(IControllable));

                foreach (object obj in parentControllables.Values)
                {
                    IControllable controllable = (IControllable)obj;
                    if (controllable is IMsbControllable)
                    {
                        controllable.Stop();
                    }
                }
            }
        }


        public bool Gc(ControlMessage controlMessage)
        {
            return InvokeGc();
        }

        public bool InvokeGc()
        {
            logger.Info("gc asked");
            System.GC.Collect();

            return true;
        }

        public void Exit()
        {

        }




        public bool InvokeHeartBeat()
        {
            return true;
        }

        public bool InvokeRefreshCache()
        {
            logger.Info("refreshCache asked");

            bool result = true;
            if (this.CacheManagerEx != null)
            {
                result = this.CacheManagerEx.Synchronize();
                logger.Info("completed refreshCache");
            }

            return result;
        }
    }
}
