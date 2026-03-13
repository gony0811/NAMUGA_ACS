using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Autofac;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Resource;
using ACS.Framework.Logging;
using ACS.Communication.Msb;
using ACS.Communication.Msb.Highway101;
using ACS.Control;
using ACS.Utility;
using ACS.Workflow;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using ACS.Framework.Base;
using ACS.Framework.Cache;

namespace ACS.Application
{
    public class AfterContextInitialized : AbstractManager
    {
        protected new Logger logger = Logger.GetLogger(typeof(AfterContextInitialized));

        public static string TYPE_CS = "control";
        public static string TYPE_TS = "trans";
        public static string TYPE_EI = "ei";
        public static string TYPE_DS = "daemon";
        public static string TYPE_MS = "query";
        public static string TYPE_RS = "report";
        public static string TYPE_HS = "host";
        public static string TYPE_EMULATOR = "emulator";
        public static string MESSAGENAME_COMMON_START_TS = "COMMON-START-TS";
        public int RmiPort { get; set; }

        public AfterContextInitialized()
        {
            RmiPort = 20000;
        }

        public void Init()
        {
            
        }

        public virtual void AfterContextInitComplete(Executor executor, ILifetimeScope lifetimeScope)
        {
            ACS.Framework.Application.Model.Application application = null;
            
            if (executor.Type.Equals(TYPE_TS))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, true);

                SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                SetReloadableToApplicationControlManager(lifetimeScope, executor);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);

                CreateOptions(lifetimeScope);

                UpdateMaxCapacity(lifetimeScope);

                CreateSingleNodes(lifetimeScope);

                CreateTripleNodes(lifetimeScope);
                
                DisplayDataSource(lifetimeScope);

                InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-TRANS");
            }
            else if(executor.Type.Equals(TYPE_EI))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                SetReloadableToApplicationControlManager(lifetimeScope, executor);

                StartMsb(lifetimeScope, executor);

                StartSocket(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);
                DisplayDataSource(lifetimeScope);

                logger.Info("successed in starting server");

                InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-EI");
                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_DS))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                //SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);

                DisplayDataSource(lifetimeScope);

                //InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-DAEMON");
                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_MS))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);

                DisplayDataSource(lifetimeScope);

                InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-REPORT");
                //logger.well("successed in starting server", true);
            }
            else if (executor.Type.Equals(TYPE_CS))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                SetReloadableToApplicationControlManager(lifetimeScope, executor);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                ScheduleHeartBeat(lifetimeScope);

                //ScheduleServerTimeSync(lifetimeScope);

                DisplayDataSource(lifetimeScope);

                InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-CONTROL");

                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_HS))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                SetLifetimeScopeWorkflowManager(lifetimeScope);
                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                SetReloadableToApplicationControlManager(lifetimeScope, executor);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);

                DisplayDataSource(lifetimeScope);

                InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-HOST");

                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_EMULATOR))
            {

            }
            else
            {
                string message = "please check process type, it should be(trans|ei|daemon|control|host|emulator|report|query)";
                //logger.fatal(message)
                throw new ApplicationException(message);
            }
        }

        private void ScheduleServerTimeSync(ILifetimeScope lifetimeScope)
        {
            IControlServerManagerEx controlServerManager = null;

            try
            {
                controlServerManager = lifetimeScope.Resolve<IControlServerManagerEx>();
            }
            catch (Exception e)
            {
                logger.Error("controlServerManager is not defined", e);
                //logger.fine("controlServerManager is not defined");
            }

            controlServerManager.ScheduleServerTimeSync();
        }

        protected virtual void StartLogPropertyWatchDog(ILifetimeScope lifetimeScope, Executor executor, bool useLogManager)
        {
            LogPropertyWatchDog logPropertyWatchDog = new LogPropertyWatchDog(executor.LogPath);

            if(useLogManager)
            {
                try
                {
                    ILogManager logManager = lifetimeScope.Resolve<ILogManager>();
                    logPropertyWatchDog.LogManager = logManager;
                }
                catch (Exception e)
                {
                    logger.Warn("logmanager is not defined, can not use logmanager to log to database", e);
                }
                logPropertyWatchDog.BeginInit();
                logPropertyWatchDog.EnableRaisingEvents = true;
            }

            //string LogconfigPath = "";
            //string SetupConfigPath = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_CONFIG];
            //XmlDocument docu = new XmlDocument();
            //docu.Load(SetupConfigPath);
            ////XmlNodeList SetupDefinitionList = docu.SelectNodes("applications/application/definitions/definition/logconfig");
            //string logConfigPath = XmlUtility.GetDataFromXml(docu, "applications/logconfig/path");
            //if (logConfigPath.Contains("logconfig.xml"))
            //{
            //    LogconfigPath = logConfigPath;
            //}
            //XmlConfigurator.Configure(new System.IO.FileInfo(LogconfigPath));
        }

        protected void ScheduleHeartBeat(ILifetimeScope lifetimeScope)
        {
            IControlServerManager controlServerManager = null;

            try
            {
                controlServerManager = lifetimeScope.Resolve<IControlServerManager>();
            }
            catch (Exception e)
            {
                logger.Error("controlServerManager is not defined", e);
                //logger.fine("controlServerManager is not defined");
            }

            if(controlServerManager.HeartBeatTimeout >= controlServerManager.HeartBeatInterval)
            {
                throw new IncorrectValueException("controlServerManager heartBeatTimeout{" + controlServerManager.HeartBeatTimeout + "}, heartBeatInterval{" + controlServerManager.HeartBeatInterval + "}");
            }

            long heartBeatTimeoutSec = controlServerManager.UseSecondAsTimeUnit ? controlServerManager.HeartBeatRetryTimeout : controlServerManager.HeartBeatRetryTimeout / 1000L;
            if ((heartBeatTimeoutSec > 15L) || (heartBeatTimeoutSec < 1L))
            {
                throw new IncorrectValueException("controlServerManager useSecondAsTimeUnit{" + controlServerManager.UseSecondAsTimeUnit + "}, heartBeatTimeout{" + controlServerManager.HeartBeatTimeout + "}");
            }

            controlServerManager.ScheduleHeartBeats();
            controlServerManager.ScheduleUiTransport();
            controlServerManager.ScheduleUiApplicationManager();

            //200622 Change NIO Logic About ES.exe does not restart
            controlServerManager.ScheduleUiCommand();
            //
        }

        protected virtual void StartSocket(ILifetimeScope lifetimeScope, Executor executor)
        {

        }



        protected void SetLifetimeScopeWorkflowManager(ILifetimeScope lifetimeScope)
        {
            BizProcessManager bizProcessManager = lifetimeScope.Resolve<BizProcessManager>();
            bizProcessManager.LifetimeScope = lifetimeScope;
        }

        protected void DisplayDataSource(ILifetimeScope lifetimeScope)
        {
            
        }

        protected void CreateTripleNodes(ILifetimeScope lifetimeScope)
        {

        }

        protected void CreateSingleNodes(ILifetimeScope lifetimeScope)
        {

        }

        protected void SynchronizeCache(ILifetimeScope lifetimeScope)
        {
            try
            {
                ICacheManagerEx CacheManager = lifetimeScope.Resolve<ICacheManagerEx>();
                bool result = CacheManager.Synchronize();
                if (result)
                {
                    logger.Fine("succeeded to synchronize cache");
                }
                else
                {
                    logger.Fine("failed to synchronize cache");
                }
            }
            catch (Exception e)
            {
                logger.Fine("cacheManager is not defined");
            }
        }

        protected void UpdateMaxCapacity(ILifetimeScope lifetimeScope)
        {
            IResourceManagerEx resourceManager = lifetimeScope.Resolve<IResourceManagerEx>();
        }

        protected void CreateOptions(ILifetimeScope lifetimeScope)
        {
            IApplicationManager applicationManager = lifetimeScope.Resolve<IApplicationManager>();

            applicationManager.CreateDefaultOptions();
        }

        protected ACS.Framework.Application.Model.Application CreateOrUpdateApplication(ILifetimeScope lifetimeScope, Executor executor)
        {
            ACS.Framework.Application.Model.Application application = null;

            try
            {
                IApplicationManager applicationManager = lifetimeScope.Resolve<IApplicationManager>();
                application = applicationManager.GetApplication(executor.Id);

                //NA_X_APPLICATION Table EXE Name 없음
                if (application == null)
                {
                    DateTime date = DateTime.Now;

                    application = new ACS.Framework.Application.Model.Application();
                    application.Id = executor.Id;
                    application.Name = executor.Id;
                    application.Type = executor.Type;
                    application.Creator = "admin";
                    application.CreateTime = date;
                    

                    application.Msb = executor.Msb;

                    application.Editor = "admin";
                    application.EditTime = date;

                    application.StartTime = date;
                    application.CheckTime = date;
                    application.State = "active";

                    if (!String.IsNullOrEmpty(executor.HardwareType))
                    {
                        application.RunningHardware = executor.HardwareType;
                    }

                    string defaultDestinationName = GetDefaultDestinationName(lifetimeScope);
                    
                    ////1PC 2ACS Test : Add 1Row
                    defaultDestinationName = defaultDestinationName.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);

                    //181215 Missing DestinationName Column when creating NA_X_APPLICATION
                    application.DestinationName = defaultDestinationName;

                    try
                    {
                        string hostAddress = GetHostAddress();
                        application.RunningHardwareAddress = hostAddress;
                    }
                    catch(Exception e)
                    {
                        logger.Info("failed to get hostAddress", e);
                    }
                    
                    applicationManager.CreateApplication(application);
                }
                else
                {
                    //NA_X_APPLICATION Table EXE Name 있음
                    DateTime date = DateTime.Now;

                    application.Msb = executor.Msb;
                    application.Editor = "admin";
                    application.EditTime = date;
                    application.StartTime = date;
                    application.CheckTime = date;
                    application.State = "active";


                    if (!string.IsNullOrEmpty(executor.HardwareType))
                    {
                        application.RunningHardware = executor.HardwareType;
                    }

                    ////190316 //1PC 2ACS Test : 미들웨어 Queue destination Name 변경시  NA_X_APPLICATION 테이블 DB반영
                    //if (string.IsNullOrEmpty(application.DestinationName))
                    //{
                    //    string defaultDestinationName = GetDefaultDestinationName(lifetimeScope);
                    //    application.DestinationName = defaultDestinationName;
                    //}
                    string defaultDestinationName = GetDefaultDestinationName(lifetimeScope);
                    defaultDestinationName = defaultDestinationName.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);

                    if (string.IsNullOrEmpty(application.DestinationName) || !defaultDestinationName.Equals(application.DestinationName))
                    {
                        application.DestinationName = defaultDestinationName;
                    }

                    string hostAddress = GetHostAddress();

                    if (!hostAddress.Equals(application.RunningHardwareAddress))
                    {
                        application.RunningHardwareAddress = hostAddress;
                    }

                    applicationManager.UpdateApplication(application);
                }

                return application;
            }
            catch (Exception e)
            {
                logger.Error(e);
                return null;
            }
        }

        protected string GetHostAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);

                foreach (IPAddress ip in hostAddress)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }

            }
            catch (SocketException e)
            {
                logger.Error(e);
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            return null;
        }

        protected string GetDefaultDestinationName(ILifetimeScope lifetimeScope)
        {
            string defaultDestinationName = "";

            try
            {
                ApplicationDefaultDestinationName applicationDefaultDestinationName = lifetimeScope.Resolve<ApplicationDefaultDestinationName>();
                defaultDestinationName = applicationDefaultDestinationName.DestinationName;
            }
            catch (Exception)
            {
                // ApplicationDefaultDestinationName is not registered
            }

            return defaultDestinationName;
        }

        protected void StartMsb(ILifetimeScope lifetimeScope, Executor executor)
        {
            if (executor.Msb.Equals("tibrv"))
            {
                StartTibrvListener(lifetimeScope);
            }
            else if (executor.Msb.Equals("highway101"))
            {
                StartHighway101Listener(lifetimeScope);
            }
            else if (executor.Msb.Equals("ibmmq"))
            {
                StartIbmMq(lifetimeScope);
            }
            else if (executor.Msb.Equals("activemq"))
            {
                StartActiveMq(lifetimeScope);
            }
            else if (executor.Msb.Equals("any"))
            {
                StartHighway101Listener(lifetimeScope);
            }
            else if (executor.Msb.Equals("rabbitmq"))
            {
                StartRabbitMQListener(lifetimeScope);
            }
            else if (executor.Msb.Equals("none"))
            {

            }
            else
            {
                string message = "please check msb, it should be {tibrv|highway101|ibmmq|any|none}";

                throw new ApplicationNotStartedException(message);
            }
        }

        protected void StartActiveMq(ILifetimeScope lifetimeScope)
        {
            throw new NotImplementedException();
        }

        protected void StartIbmMq(ILifetimeScope lifetimeScope)
        {
            throw new NotImplementedException();
        }

        protected void StartRabbitMQListener(ILifetimeScope lifetimeScope)
        {
            IEnumerable<IMsbControllable> msbControllables = lifetimeScope.Resolve<IEnumerable<IMsbControllable>>();

            foreach (IMsbControllable msbControllable in msbControllables)
            {
                try
                {
                    if(msbControllable.GetMsbControllerName().Equals("rabbitmq"))
                    {
                        msbControllable.Start();
                    }           
                }
                catch (MsbStartExecption e)
                {
                    throw e;
                }
            }
        }
        protected void StartHighway101Listener(ILifetimeScope lifetimeScope)
        {
            IEnumerable<IMsbControllable> msbControllables = lifetimeScope.Resolve<IEnumerable<IMsbControllable>>();

            foreach (IMsbControllable msbControllable in msbControllables)
            {
                try
                {
                    msbControllable.Start();
                }
                catch (MsbStartExecption e)
                {
                    throw e;
                }
            }
        }

        protected void StartTibrvListener(ILifetimeScope lifetimeScope)
        {
            IEnumerable<IMsbControllable> msbControllables = lifetimeScope.Resolve<IEnumerable<IMsbControllable>>();

            foreach (IMsbControllable msbControllable in msbControllables)
            {
                try
                {
                    msbControllable.Start();
                }
                catch (MsbStartExecption e)
                {
                    throw e;
                }
            }
        }

        protected void SetLifetimeScopeToApplicationControlManager(ILifetimeScope lifetimeScope)
        {
            try
            {
                IApplicationControlManager applicationControlManager = lifetimeScope.Resolve<IApplicationControlManager>();
                applicationControlManager.LifetimeScope = lifetimeScope;
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        protected void SetReloadableToApplicationControlManager(ILifetimeScope lifetimeScope, Executor executor)
        {
            try
            {
                IApplicationControlManager applicationControlManager = lifetimeScope.Resolve<IApplicationControlManager>();
                if (executor.UseService)
                {
                    //-----------BPEL Document Reload 
                    //IReloadableApplicationContextAware reloadableApplicationContextAware = (IReloadableApplicationContextAware)lifetimeScope.Resolve<IReloadableApplicationContextAware>();
                    //applicationControlManager.ReloadableApplicationContextAware = reloadableApplicationContextAware;

                    //-----------Service.dll Reload
                    applicationControlManager.ReloadableDirectory = executor.ServicePath;
                    // applicationControlManager.ReloadableAssemblyDefinitions = executor.GetServiceBeanDefinitionsAsStringArray();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        protected void InvokeStartWorkflow(ILifetimeScope lifetimeScope, ACS.Framework.Application.Model.Application application, string messageName)
        {
            try
            {
                Workflow.IWorkflowManager workflowManager = lifetimeScope.Resolve<IWorkflowManager>();
                
                Object[] args = { lifetimeScope, application };               
                workflowManager.Execute(messageName, args);
                //workflowManager.Start();
            }
            catch(Exception e)
            {
                throw e;
            }

        }
    }
}
