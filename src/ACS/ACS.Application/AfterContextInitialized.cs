using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Resource;
using ACS.Framework.Logging;
using ACS.Framework.Logging.Database;
using ACS.Communication.Msb;
using ACS.Communication.Msb.Highway101;
using ACS.Control;
using ACS.Utility;
using ACS.Workflow;
using System.Configuration;
using Spring.Context;
using Spring.Context.Support;
using System.Net;
using System.Net.Sockets;
using log4net;
using log4net.Config;
using log4net.Repository;
using ACS.Framework.Base;
using ACS.Framework.Cache;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
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

        public virtual void AfterContextInitComplete(Executor executor, IApplicationContext applicationContext)
        {
            ACS.Framework.Application.Model.Application application = null;
            
            if (executor.Type.Equals(TYPE_TS))
            {
                StartLogPropertyWatchDog(applicationContext, executor, true);

                SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                SetReloadableToApplicationControlManager(applicationContext, executor);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                SynchronizeCache(applicationContext);

                CreateOptions(applicationContext);

                UpdateMaxCapacity(applicationContext);

                CreateSingleNodes(applicationContext);

                CreateTripleNodes(applicationContext);
                
                DisplayDataSource(applicationContext);

                InvokeStartWorkflow(applicationContext, application, "COMMON-START-TRANS");
            }
            else if(executor.Type.Equals(TYPE_EI))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                SetReloadableToApplicationControlManager(applicationContext, executor);

                StartMsb(applicationContext, executor);

                StartSocket(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                SynchronizeCache(applicationContext);
                DisplayDataSource(applicationContext);

                logger.Info("successed in starting server");

                InvokeStartWorkflow(applicationContext, application, "COMMON-START-EI");
                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_DS))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                //SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                SynchronizeCache(applicationContext);

                DisplayDataSource(applicationContext);

                //InvokeStartWorkflow(applicationContext, application, "COMMON-START-DAEMON");
                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_MS))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                SynchronizeCache(applicationContext);

                DisplayDataSource(applicationContext);

                InvokeStartWorkflow(applicationContext, application, "COMMON-START-REPORT");
                //logger.well("successed in starting server", true);
            }
            else if (executor.Type.Equals(TYPE_CS))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                SetReloadableToApplicationControlManager(applicationContext, executor);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                ScheduleHeartBeat(applicationContext);

                //ScheduleServerTimeSync(applicationContext);

                DisplayDataSource(applicationContext);

                InvokeStartWorkflow(applicationContext, application, "COMMON-START-CONTROL");

                //logger.well("successed in starting server", true);
            }
            else if(executor.Type.Equals(TYPE_HS))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                SetApplicationContextWorkflowManager(applicationContext);
                SetApplicationContextToApplicationControlManager(applicationContext);

                SetReloadableToApplicationControlManager(applicationContext, executor);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                SynchronizeCache(applicationContext);

                DisplayDataSource(applicationContext);

                InvokeStartWorkflow(applicationContext, application, "COMMON-START-HOST");

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

        private void ScheduleServerTimeSync(IApplicationContext applicationContext)
        {
            IControlServerManagerEx controlServerManager = null;

            try
            {
                controlServerManager = (IControlServerManagerEx)applicationContext.GetObject("ControlServerManager");
            }
            catch (Exception e)
            {
                logger.Error("controlServerManager is not defined", e);
                //logger.fine("controlServerManager is not defined");
            }

            controlServerManager.ScheduleServerTimeSync();
        }

        protected virtual void StartLogPropertyWatchDog(IApplicationContext applicationContext, Executor executor, bool useLogManager)
        {
            LogPropertyWatchDog logPropertyWatchDog = new LogPropertyWatchDog(executor.LogPath);

            if(useLogManager)
            {
                try
                {
                    ILogManager logManager = (ILogManager)applicationContext.GetObject("LogManager");
                    logPropertyWatchDog.LogManager = logManager;

                    log4net.Repository.ILoggerRepository loggerRepository = log4net.LogManager.GetRepository();
                    
                    foreach(log4net.Appender.IAppender appender in loggerRepository.GetAppenders())
                    {
                        if(appender is DatabaseAppender)
                        {
                            DatabaseAppender databaseAppender = (DatabaseAppender)appender;
                            databaseAppender.LogManager = logManager;
                        }
                    }
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

        protected void ScheduleHeartBeat(IApplicationContext applicationContext)
        {
            IControlServerManager controlServerManager = null;

            try
            {
                controlServerManager = (IControlServerManager)applicationContext.GetObject("ControlServerManager");
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

        protected virtual void StartSocket(IApplicationContext applicationContext, Executor executor)
        {

        }



        protected void SetApplicationContextWorkflowManager(IApplicationContext applicationContext)
        {
            BizProcessManager bizProcessManager = (BizProcessManager)applicationContext.GetObject("BizProcessManager");
            bizProcessManager.ApplicationContext = applicationContext;
        }

        protected void DisplayDataSource(IApplicationContext applicationContext)
        {
            
        }

        protected void CreateTripleNodes(IApplicationContext applicationContext)
        {

        }

        protected void CreateSingleNodes(IApplicationContext applicationContext)
        {

        }

        protected void SynchronizeCache(IApplicationContext applicationContext)
        {
            try
            {
                ICacheManagerEx CacheManager = (ICacheManagerEx)applicationContext.GetObject("CacheManager");
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

        protected void UpdateMaxCapacity(IApplicationContext applicationContext)
        {
            IResourceManagerEx resourceManager = (IResourceManagerEx)applicationContext.GetObject("ResourceManager");
        }

        protected void CreateOptions(IApplicationContext applicationContext)
        {
            IApplicationManager applicationManager = (IApplicationManager)applicationContext.GetObject("ApplicationManager");

            applicationManager.CreateDefaultOptions();
        }

        protected ACS.Framework.Application.Model.Application CreateOrUpdateApplication(IApplicationContext applicationContext, Executor executor)
        {
            ACS.Framework.Application.Model.Application application = null;

            try
            {
                IApplicationManager applicationManager = (IApplicationManager)applicationContext.GetObject("ApplicationManager");
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

                    string defaultDestinationName = GetDefaultDestinationName(applicationContext);
                    
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
                    //    string defaultDestinationName = GetDefaultDestinationName(applicationContext);
                    //    application.DestinationName = defaultDestinationName;
                    //}
                    string defaultDestinationName = GetDefaultDestinationName(applicationContext);
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

        protected string GetDefaultDestinationName(IApplicationContext applicationContext)
        {
            string defaultDestinationName = "";
            bool existDefaultDestination = false;

            IDictionary applicationDefaultDestinationNames = applicationContext.GetObjectsOfType(typeof(ApplicationDefaultDestinationName));


            foreach (object value in applicationDefaultDestinationNames.Values)
            {
                ApplicationDefaultDestinationName applicationDefaultDestinationName = (ApplicationDefaultDestinationName)value;

                defaultDestinationName = applicationDefaultDestinationName.DestinationName;
                existDefaultDestination = true;
                break;
            }

            if (!existDefaultDestination)
            {
                while (applicationContext.ParentContext != null)
                {
                    applicationContext = applicationContext.ParentContext;
                    applicationDefaultDestinationNames = applicationContext.GetObjectsOfType(typeof(ApplicationDefaultDestinationName));

                    foreach (object value in applicationDefaultDestinationNames.Values)
                    {
                        ApplicationDefaultDestinationName applicationDefaultDestinationName = (ApplicationDefaultDestinationName)value;

                        defaultDestinationName = applicationDefaultDestinationName.DestinationName;
                        existDefaultDestination = true;
                        break;
                    }
                }
            }

            return defaultDestinationName;
        }

        protected void StartMsb(IApplicationContext applicationContext, Executor executor)
        {
            if (executor.Msb.Equals("tibrv"))
            {
                StartTibrvListener(applicationContext);
            }
            else if (executor.Msb.Equals("highway101"))
            {
                StartHighway101Listener(applicationContext);
            }
            else if (executor.Msb.Equals("ibmmq"))
            {
                StartIbmMq(applicationContext);
            }
            else if (executor.Msb.Equals("activemq"))
            {
                StartActiveMq(applicationContext);
            }
            else if (executor.Msb.Equals("any"))
            {
                StartHighway101Listener(applicationContext);
            }
            else if (executor.Msb.Equals("rabbitmq"))
            {
                StartRabbitMQListener(applicationContext);
            }
            else if (executor.Msb.Equals("none"))
            {

            }
            else
            {
                string message = "please check msb, it should be {tibrv|highway101|ibmmq|any|none}";

                throw new ApplicationNotStartedException(message);
            }

            //StringBuilder sbSender = new StringBuilder();
            //StringBuilder sbReceiver = new StringBuilder();

            //IDictionary currentAbstractMsbs = applicationContext.GetObjectsOfType(typeof(AbstractMsb));

            //foreach(AbstractMsb abstractMsb in currentAbstractMsbs)
            //{
            //    if(abstractMsb is IControllable)
            //    {

            //    }
            //}
        }

        protected void StartActiveMq(IApplicationContext applicationContext)
        {
            throw new NotImplementedException();
        }

        protected void StartIbmMq(IApplicationContext applicationContext)
        {
            throw new NotImplementedException();
        }

        protected void StartRabbitMQListener(IApplicationContext applicationContext)
        {
            IDictionary currentMsbControllables = applicationContext.GetObjectsOfType(typeof(IMsbControllable));

            foreach (object obj in currentMsbControllables.Values)
            {
                try
                {
                    IMsbControllable msbControllable = (IMsbControllable)obj;
 
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

            IApplicationContext parentApplicationContext = applicationContext.ParentContext;
            if (parentApplicationContext != null)
            {
                IDictionary parentMsbControllables = parentApplicationContext.GetObjectsOfType(typeof(IMsbControllable));
                foreach (object msbControllable in parentMsbControllables.Values)
                {
                    try
                    {
                        ((IMsbControllable)msbControllable).Start();
                    }
                    catch (MsbStartExecption e)
                    {
                        throw e;
                    }
                }
            }
        }
        protected void StartHighway101Listener(IApplicationContext applicationContext)
        {
            IDictionary currentMsbControllables = applicationContext.GetObjectsOfType(typeof(IMsbControllable));

            foreach (object obj in currentMsbControllables.Values)
            {
                try
                {
                    IMsbControllable msbControllable = (IMsbControllable)obj;
                    msbControllable.Start();
                }
                catch (MsbStartExecption e)
                {
                    throw e;
                }
            }

            IApplicationContext parentApplicationContext = applicationContext.ParentContext;
            if (parentApplicationContext != null)
            {
                IDictionary parentMsbControllables = parentApplicationContext.GetObjectsOfType(typeof(IMsbControllable));
                foreach (object msbControllable in parentMsbControllables.Values)
                {
                    try
                    {
                        ((IMsbControllable)msbControllable).Start();
                    }
                    catch (MsbStartExecption e)
                    {
                        throw e;
                    }
                }
            }
        }

        protected void StartTibrvListener(IApplicationContext applicationContext)
        {
            IDictionary currentMsbControllables = applicationContext.GetObjectsOfType(typeof(IMsbControllable));

            //foreach (IMsbControllable msbControllable in currentMsbControllables)
            foreach (object obj in currentMsbControllables.Values)
            {
                try
                {
                    //msbControllable.Start();
                    IMsbControllable msbControllable = (IMsbControllable)obj;
                    msbControllable.Start();
                }
                catch (MsbStartExecption e)
                {
                    throw e;
                }
            }

            IApplicationContext parentApplicationContext = applicationContext.ParentContext;
            if (parentApplicationContext != null)
            {
                IDictionary parentMsbControllables = parentApplicationContext.GetObjectsOfType(typeof(IMsbControllable));
                foreach (object msbControllable in parentMsbControllables.Values)
                {
                    try
                    {
                        ((IMsbControllable)msbControllable).Start();
                    }
                    catch (MsbStartExecption e)
                    {
                        throw e;
                    }
                }
            }
        }

        protected void SetApplicationContextToApplicationControlManager(IApplicationContext applicationContext)
        {
            try
            {
                IApplicationControlManager applicationControlManager = (IApplicationControlManager)applicationContext.GetObject("ApplicationControlManager");
                applicationControlManager.ApplicationContext = applicationContext;
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        protected void SetReloadableToApplicationControlManager(IApplicationContext applicationContext, Executor executor)
        {
            try
            {
                IApplicationControlManager applicationControlManager = (IApplicationControlManager)applicationContext.GetObject("ApplicationControlManager");
                if (executor.UseService)
                {
                    //-----------BPEL Document Reload 
                    //IReloadableApplicationContextAware reloadableApplicationContextAware = (IReloadableApplicationContextAware)applicationContext.GetObject("bpelProcessContext");
                    //applicationControlManager.ReloadableApplicationContextAware = reloadableApplicationContextAware;

                    //-----------Service.dll Reload
                    applicationControlManager.ReloadableDirectory = executor.ServicePath;
                    applicationControlManager.ReloadableAssemblyDefinitions = executor.GetServiceBeanDefinitionsAsStringArray();
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        protected void InvokeStartWorkflow(IApplicationContext applicationContext, ACS.Framework.Application.Model.Application application, string messageName)
        {
            try
            {
                Workflow.IWorkflowManager workflowManager = (IWorkflowManager)applicationContext.GetObject("WorkflowManager");
                
                Object[] args = { applicationContext, application };               
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
