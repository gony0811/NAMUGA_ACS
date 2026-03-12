using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Configuration;
using ACS.Framework.Cache;

namespace ACS.Manager.Application
{    
    public class ApplicationControlManagerExImplement : AbstractManager, IApplicationControlManager
    {
        public long SleepIntervalWhenStop { get; set; }
        public long RunningBizProcessCheckIntervalWhenStop { get; set; }

        public int RunningBizProcessCheckRetryCountWhenStop { get; set; }
        public IApplicationContext ApplicationContext { get; set; }
        public IReloadableApplicationContextAware ReloadableApplicationContextAware { get; set; }
        public string ReloadableDirectory { get; set; }
        public string[] ReloadableAssemblyDefinitions { get; set; }

        public IWorkflowManager WorkflowManager { get; set; }
        public IApplicationManager ApplicationManager { get; set; }

        //200630 REFRESHCACHE
        public ICacheManagerEx CacheManager { get; set; }
        //

        public NioInterfaceManager NioInterfaceManager { get; set; }

        public bool Control(ControlMessage controlMessage)
        {
            bool result = false;
            string messageName = controlMessage.MessageName;

            if (messageName.Equals("CONTROL-HEARTBEAT"))
            {
                result = HeartBeat(controlMessage);
            }
            else if (messageName.Equals("CONTROL-RELOADSERVICE"))
            {
                result = ReloadService(controlMessage);
            }
            else if (messageName.Equals("CONTROL-RELOADWORKFLOW"))
            {
                result = ReloadWorkflow(controlMessage);
            }
            else if (messageName.Equals("CONTROL-STOP"))
            {
                result = Stop(controlMessage);
            }
            else if (messageName.Equals("CONTROL-STARTNIOCONTROLLABLE"))
            {
                result = StartNioControllable((ControlMessageEx)controlMessage);
            }
            else if (messageName.Equals("CONTROL-STOPNIOCONTROLLABLE"))
            {
                StopNioControllable((ControlMessageEx)controlMessage);
            }
            else if (messageName.Equals("CONTROL-LOADNIOCONTROLLABLE"))
            {
                LoadNioControllable((ControlMessageEx)controlMessage);
            }
            else if (messageName.Equals("CONTROL-UNLOADNIOCONTROLLABLE"))
            {
                UnloadNioControllable((ControlMessageEx)controlMessage);
            }
            else if (messageName.Equals("CONTROL-GC"))
            {
                Gc(controlMessage);
            }
            //200630 REFRESHCACHE
            else if (messageName.Equals(ControlMessage.MESSAGENAME_CONTROL_REFRESHCACHE))
            {
                RefreshCache(controlMessage);
            }
            else if (messageName.Equals(ControlMessage.MESSAGENAME_CONTROL_RELOADWORKFLOW))
            {
                ReloadWorkflow(controlMessage);
            }
            //
            else
            {
                logger.Debug(string.Format("Please check messageName : {0}", controlMessage.MessageName));
            }

            return result;
        }

        private bool UnloadNioControllable(ControlMessageEx controlMessageEx)
        {
            return InvokeUnloadNioControllable(controlMessageEx.NioName);
        }

        private bool InvokeUnloadNioControllable(string nioName)
        {
            logger.Info("unloadNioControllable asked");
            if (this.NioInterfaceManager != null)
            {
                this.NioInterfaceManager.UnloadNioInterface(nioName);
            }
            return true;
        }

        private bool LoadNioControllable(ControlMessageEx controlMessageEx)
        {
            //200622 Change NIO Logic About ES.exe does not restart
            //return InvokeNioControllable(controlMessageEx.NioName);
            return InvokeLoadNioControllable(controlMessageEx.NioName);
            //
        }

        //200622 Change NIO Logic About ES.exe does not restart
        //private bool InvokeNioControllable(string nioName)
        private bool InvokeLoadNioControllable(string nioName)
        //
        {
            logger.Info("loadNioControllable asked");
            if (this.NioInterfaceManager != null)
            {
                try
                {
                    //200622 Change NIO Logic About ES.exe does not restart
                    //this.NioInterfaceManager.LoadNioInterface(ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE]);
                    this.NioInterfaceManager.LoadNioInterface(ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE], nioName);
                    //
                }
                catch (Exception e)
                {
                    logger.Error("", e);
                }
            }
            return true;
        }

        public bool StopNioControllable(ControlMessageEx controlMessageEx)
        {
            return InvokeStopNioControllable(controlMessageEx.NioName);
        }

        public bool InvokeStopNioControllable(string nioName)
        {
            logger.Info("stopNioControllable asked");

            bool result = true;

            if(this.NioInterfaceManager != null)
            {
                result = this.NioInterfaceManager.StopNioInterface(nioName);
            }
            else
            {
                logger.Error("nioInterfaceManager is null");
            }

            return result;
        }

        public bool StartNioControllable(ControlMessageEx controlMessageEx)
        {
            return InvokeStartNioControllable(controlMessageEx.NioName);
        }

        private bool InvokeStartNioControllable(string nioName)
        {
            logger.Info("startNioControllable asked");

            if(this.NioInterfaceManager != null)
            {
                this.NioInterfaceManager.StartNioInterface(nioName);
            }

            return true;
        }

        public bool HeartBeat(ControlMessage paramControlMessage)
        {
            return true;
        }

        public bool InvokeHeartBeat()
        {
            return true;
        }

        public bool ReloadWorkflow(ControlMessage paramControlMessage)
        {
            return InvokeReloadWorkflow();
        }

        public bool InvokeReloadWorkflow()
        {
            if(this.WorkflowManager != null)
            {
                this.WorkflowManager.Reload();
            }

            return true;
        }

        /**
         * @deprecated
        */
        public bool ReloadService(ControlMessage controlMessage)
        {
            return InvokeReloadService();
        }


        /**
        * @deprecated
        */
        public bool InvokeReloadService()
        {
            logger.Info("reloadService asked");

            AbstractApplicationContext currentApplicationContext = (AbstractApplicationContext)this.ApplicationContext;

            IApplicationContext parentApplicationContext = currentApplicationContext.ParentContext;
            string path = SystemUtility.GetFullPathName(ReloadableDirectory);
            CustomClassLoader serviceClassLoader = new CustomClassLoader(path);
            AbstractApplicationContext reloadableApplicationContext = new XmlApplicationContext(false, parentApplicationContext, this.ReloadableAssemblyDefinitions);
            foreach (object classObject in serviceClassLoader.GetClassLoader())
            {
                reloadableApplicationContext.ConfigureObject(classObject, classObject.GetType().FullName);
            }

            reloadableApplicationContext.Refresh();

            //this.ReloadableApplicationContextAware.SetApplicationContext(reloadableApplicationContext);
            this.ApplicationContext = reloadableApplicationContext;

            currentApplicationContext.Dispose();

            logger.Info("completed reloadService");
            
            return true;
        }

        /**
        * @deprecated
        */
        public bool Stop(ControlMessage controlMessage)
        {
            return InvokeStop(controlMessage.ApplicationType, controlMessage.ApplicationName);
        }

        /**
        * @deprecated
        */
        public bool InvokeStop(string applicationType, string applicationName)
        {
            try
            {
                DisposeMsbControllables();
                logger.Info("disposed msbControllables");
            }
            catch (Exception e)
            {
                logger.Error("failed to dispose msbControllables", e);
            }

            if (applicationType.Equals("ei"))
            {
                 this.NioInterfaceManager.StopAll();
            }

            try
            {
                ACS.Framework.Application.Model.Application application = this.ApplicationManager.GetApplication(applicationName);
                if (application != null)
                {
                    application.State = "inactive";
                    InvokeStopWorkflow(application);

                    ApplicationManager.UpdateApplication(application);
                }

            }
            catch (Exception e)
            {
                logger.Error("failed to invoke stopWorkflow", e);
            }

            return true;
        }

        public void InvokeStopWorkflow(ACS.Framework.Application.Model.Application application)
        {
            if (this.WorkflowManager != null)
            {
                Object[] args = { this.ApplicationContext, application };
                this.WorkflowManager.Execute(GetStopWorkflowName(application), args);
            }
        }

        private string GetStopWorkflowName(Framework.Application.Model.Application application)
        {
            String stopWorkflowName = "";
            if (application.Type.Equals("trans"))
            {
                stopWorkflowName = "COMMON-STOP-TRANS";
            }
            else if (application.Type.Equals("ei"))
            {
                stopWorkflowName = "COMMON-STOP-EI";
            }
            else if (application.Type.Equals("daemon"))
            {
                stopWorkflowName = "COMMON-STOP-DAEMON";
            }
            else if (application.Type.Equals("control"))
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


        /**
        * @deprecated
        */

        public bool Gc(ControlMessage paramControlMessage)
        {
            return InvokeGc();
        }

        public bool InvokeGc()
        {
            System.GC.Collect();

            return true;
        }

        /**
        * @deprecated
        */
        public bool RefreshCache(ControlMessage paramControlMessage)
        {
            //200630 REFRESHCACHE
            return InvokeRefreshCache();
            //throw new NotImplementedException();
            //
        }

        /**
        * @deprecated
        */
        public bool InvokeRefreshCache()
        {
            //200630 REFRESHCACHE
            logger.Info("refreshCache asked");

            bool result = true;
            if (this.CacheManager != null)
            {
                result = this.CacheManager.Synchronize();
                //logger.well("completed refreshCache");
                logger.Info("completed refreshCache");
            }
            return result;
            //throw new NotImplementedException();
            //
        }

        /**
         * @deprecated
         */
        protected void DisposeMsbControllables()
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


    }
}
