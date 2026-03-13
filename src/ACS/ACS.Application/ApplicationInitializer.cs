using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Autofac;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Resource;
using ACS.Framework.Logging;
using ACS.Framework.Cache;
using ACS.Framework.DependencyInjection;
using ACS.Communication.Msb;
using ACS.Control;
using ACS.Workflow;
using ACS.Utility;

namespace ACS.Application
{
    /// <summary>
    /// Autofac DI 기반 애플리케이션 초기화.
    /// Spring.NET의 AfterContextInitialized / ApplicationEventListener를 대체.
    /// 생성자 주입을 통해 필요한 의존성을 수신하며, Initialize()에서 초기화 로직 실행.
    /// </summary>
    public class ApplicationInitializer
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ApplicationInitializer));

        public const string TYPE_CS = "control";
        public const string TYPE_TS = "trans";
        public const string TYPE_EI = "ei";
        public const string TYPE_DS = "daemon";
        public const string TYPE_MS = "query";
        public const string TYPE_RS = "report";
        public const string TYPE_HS = "host";
        public const string TYPE_EMULATOR = "emulator";

        private readonly ILogManager _logManager;
        private readonly IApplicationManager _applicationManager;
        private readonly IApplicationControlManager _applicationControlManager;
        private readonly IEnumerable<IMsbControllable> _msbControllables;
        private readonly Lazy<IWorkflowManager> _workflowManager;
        private readonly ICacheManagerEx _cacheManager;
        private readonly IControlServerManager _controlServerManager;
        private readonly IServiceLocator _serviceLocator;
        private readonly ILifetimeScope _scope;

        public ApplicationInitializer(
            ILifetimeScope scope,
            ILogManager logManager,
            IApplicationManager applicationManager,
            IApplicationControlManager applicationControlManager,
            IEnumerable<IMsbControllable> msbControllables,
            IServiceLocator serviceLocator,
            Lazy<IWorkflowManager> workflowManager = null,
            ICacheManagerEx cacheManager = null,
            IControlServerManager controlServerManager = null)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _logManager = logManager;
            _applicationManager = applicationManager;
            _applicationControlManager = applicationControlManager;
            _msbControllables = msbControllables ?? Enumerable.Empty<IMsbControllable>();
            _serviceLocator = serviceLocator;
            _workflowManager = workflowManager;
            _cacheManager = cacheManager;
            _controlServerManager = controlServerManager;
        }

        /// <summary>
        /// 메인 초기화 진입점. Executor.Start()에서 컨테이너 빌드 후 호출.
        /// AfterContextInitialized.AfterContextInitComplete()의 로직을 Autofac DI로 재구현.
        /// </summary>
        public void Initialize(Executor executor)
        {
            Framework.Application.Model.Application application = null;

            if (executor.Type.Equals(TYPE_TS))
            {
                StartLogPropertyWatchDog(executor, useLogManager: true);
                SetApplicationContextWorkflowManager();
                SetApplicationContextToApplicationControlManager();
                SetReloadableToApplicationControlManager(executor);
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                CreateOptions();
                UpdateMaxCapacity();
                CreateSingleNodes();
                CreateTripleNodes();
                DisplayDataSource();
                InvokeStartWorkflow(application, "COMMON-START-TRANS");
            }
            else if (executor.Type.Equals(TYPE_EI))
            {
                StartLogPropertyWatchDog(executor, useLogManager: false);
                SetApplicationContextWorkflowManager();
                SetApplicationContextToApplicationControlManager();
                SetReloadableToApplicationControlManager(executor);
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
                logger.Info("succeeded in starting server");
                InvokeStartWorkflow(application, "COMMON-START-EI");
            }
            else if (executor.Type.Equals(TYPE_DS))
            {
                StartLogPropertyWatchDog(executor, useLogManager: false);
                SetApplicationContextToApplicationControlManager();
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
            }
            else if (executor.Type.Equals(TYPE_MS))
            {
                StartLogPropertyWatchDog(executor, useLogManager: false);
                SetApplicationContextWorkflowManager();
                SetApplicationContextToApplicationControlManager();
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
                InvokeStartWorkflow(application, "COMMON-START-REPORT");
            }
            else if (executor.Type.Equals(TYPE_CS))
            {
                StartLogPropertyWatchDog(executor, useLogManager: false);
                SetApplicationContextWorkflowManager();
                SetApplicationContextToApplicationControlManager();
                SetReloadableToApplicationControlManager(executor);
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                ScheduleHeartBeat();
                DisplayDataSource();
                InvokeStartWorkflow(application, "COMMON-START-CONTROL");
            }
            else if (executor.Type.Equals(TYPE_HS))
            {
                StartLogPropertyWatchDog(executor, useLogManager: false);
                SetApplicationContextWorkflowManager();
                SetApplicationContextToApplicationControlManager();
                SetReloadableToApplicationControlManager(executor);
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
                InvokeStartWorkflow(application, "COMMON-START-HOST");
            }
            else if (executor.Type.Equals(TYPE_EMULATOR))
            {
                // emulator: no initialization needed
            }
            else
            {
                string message = "please check process type, it should be(trans|ei|daemon|control|host|emulator|report|query)";
                throw new ApplicationException(message);
            }
        }

        #region Initialization Steps

        private void StartLogPropertyWatchDog(Executor executor, bool useLogManager)
        {
            LogPropertyWatchDog logPropertyWatchDog = new LogPropertyWatchDog(executor.LogPath);

            if (useLogManager)
            {
                try
                {
                    logPropertyWatchDog.LogManager = _logManager;
                }
                catch (Exception e)
                {
                    logger.Warn("logmanager is not defined, can not use logmanager to log to database", e);
                }

                logPropertyWatchDog.BeginInit();
                logPropertyWatchDog.EnableRaisingEvents = true;
            }
        }

        private void SetApplicationContextWorkflowManager()
        {
            try
            {
                // BizProcessManager에 IServiceLocator 설정 (Spring ApplicationContext 대체)
                if (_workflowManager?.Value is BizProcessManager bizProcessManager)
                {
                    // BizProcessManager.ApplicationContext는 레거시 — ServiceLocator로 대체
                    // bizProcessManager.ServiceLocator = _serviceLocator;
                }
            }
            catch (Exception e)
            {
                logger.Error("failed to set workflow manager context", e);
            }
        }

        private void SetApplicationContextToApplicationControlManager()
        {
            try
            {
                // ApplicationControlManager에 ServiceLocator 기반 컨텍스트 설정
                // 레거시: applicationControlManager.ApplicationContext = applicationContext;
                // Autofac 전환 후에도 IApplicationControlManager.ApplicationContext가
                // Spring IApplicationContext를 참조하므로, 과도기 호환을 위해 null 허용
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        private void SetReloadableToApplicationControlManager(Executor executor)
        {
            try
            {
                if (executor.UseService)
                {
                    _applicationControlManager.ReloadableDirectory = executor.ServicePath;
                    // _applicationControlManager.ReloadableAssemblyDefinitions = executor.GetServiceBeanDefinitionsAsStringArray();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void StartMsb(Executor executor)
        {
            if (executor.Msb.Equals("none"))
            {
                return;
            }

            // Autofac IEnumerable<IMsbControllable>로 모든 MSB 제어 가능 객체를 순회하여 시작
            foreach (IMsbControllable msbControllable in _msbControllables)
            {
                try
                {
                    // rabbitmq의 경우 MSB 컨트롤러 이름을 확인
                    if (executor.Msb.Equals("rabbitmq"))
                    {
                        if (msbControllable.GetMsbControllerName().Equals("rabbitmq"))
                        {
                            msbControllable.Start();
                        }
                    }
                    else
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

        private Framework.Application.Model.Application CreateOrUpdateApplication(Executor executor)
        {
            Framework.Application.Model.Application application = null;

            try
            {
                application = _applicationManager.GetApplication(executor.Id);

                if (application == null)
                {
                    DateTime date = DateTime.Now;

                    application = new Framework.Application.Model.Application();
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

                    if (!string.IsNullOrEmpty(executor.HardwareType))
                    {
                        application.RunningHardware = executor.HardwareType;
                    }

                    string defaultDestinationName = GetDefaultDestinationName();
                    defaultDestinationName = defaultDestinationName.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                    application.DestinationName = defaultDestinationName;

                    try
                    {
                        string hostAddress = GetHostAddress();
                        application.RunningHardwareAddress = hostAddress;
                    }
                    catch (Exception e)
                    {
                        logger.Info("failed to get hostAddress", e);
                    }

                    _applicationManager.CreateApplication(application);
                }
                else
                {
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

                    string defaultDestinationName = GetDefaultDestinationName();
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

                    _applicationManager.UpdateApplication(application);
                }

                return application;
            }
            catch (Exception e)
            {
                logger.Error(e);
                return null;
            }
        }

        private void SynchronizeCache()
        {
            try
            {
                if (_cacheManager != null)
                {
                    bool result = _cacheManager.Synchronize();
                    if (result)
                    {
                        logger.Fine("succeeded to synchronize cache");
                    }
                    else
                    {
                        logger.Fine("failed to synchronize cache");
                    }
                }
                else
                {
                    logger.Fine("cacheManager is not defined");
                }
            }
            catch (Exception)
            {
                logger.Fine("cacheManager is not defined");
            }
        }

        private void CreateOptions()
        {
            _applicationManager.CreateDefaultOptions();
        }

        private void UpdateMaxCapacity()
        {
            try
            {
                var resourceManager = _scope.Resolve<IResourceManagerEx>();
                // ResourceManager resolved for max capacity update
            }
            catch (Exception e)
            {
                logger.Error("failed to resolve ResourceManager for UpdateMaxCapacity", e);
            }
        }

        private void CreateSingleNodes()
        {
            // placeholder - site-specific implementation
        }

        private void CreateTripleNodes()
        {
            // placeholder - site-specific implementation
        }

        private void ScheduleHeartBeat()
        {
            if (_controlServerManager == null)
            {
                logger.Error("controlServerManager is not defined");
                return;
            }

            if (_controlServerManager.HeartBeatTimeout >= _controlServerManager.HeartBeatInterval)
            {
                throw new IncorrectValueException(
                    "controlServerManager heartBeatTimeout{" + _controlServerManager.HeartBeatTimeout +
                    "}, heartBeatInterval{" + _controlServerManager.HeartBeatInterval + "}");
            }

            long heartBeatTimeoutSec = _controlServerManager.UseSecondAsTimeUnit
                ? _controlServerManager.HeartBeatRetryTimeout
                : _controlServerManager.HeartBeatRetryTimeout / 1000L;

            if ((heartBeatTimeoutSec > 15L) || (heartBeatTimeoutSec < 1L))
            {
                throw new IncorrectValueException(
                    "controlServerManager useSecondAsTimeUnit{" + _controlServerManager.UseSecondAsTimeUnit +
                    "}, heartBeatTimeout{" + _controlServerManager.HeartBeatTimeout + "}");
            }

            _controlServerManager.ScheduleHeartBeats();
            _controlServerManager.ScheduleUiTransport();
            _controlServerManager.ScheduleUiApplicationManager();
            _controlServerManager.ScheduleUiCommand();
        }

        private void InvokeStartWorkflow(Framework.Application.Model.Application application, string messageName)
        {
            try
            {
                if (_workflowManager?.Value != null)
                {
                    object[] args = { _serviceLocator, application };
                    _workflowManager.Value.Execute(messageName, args);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void DisplayDataSource()
        {
            // placeholder - displays data source info for diagnostics
        }

        #endregion

        #region Helper Methods

        private string GetHostAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);

                foreach (IPAddress ip in hostAddress)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
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

        private string GetDefaultDestinationName()
        {
            string defaultDestinationName = "";

            try
            {
                // Autofac에서 등록된 모든 ApplicationDefaultDestinationName을 해결
                var destinations = _scope.Resolve<IEnumerable<ApplicationDefaultDestinationName>>();

                foreach (ApplicationDefaultDestinationName dest in destinations)
                {
                    defaultDestinationName = dest.DestinationName;
                    break;
                }
            }
            catch (Exception)
            {
                // ApplicationDefaultDestinationName이 등록되지 않은 경우 무시
            }

            return defaultDestinationName;
        }

        #endregion
    }
}
