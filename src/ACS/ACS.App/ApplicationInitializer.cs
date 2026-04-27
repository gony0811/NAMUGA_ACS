using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Autofac;
using ACS.Core.Application;
using ACS.Core.Application.Model;
using ACS.Core.Resource;
using ACS.Core.Logging;
using ACS.Core.Cache;
using ACS.Core.DependencyInjection;
using ACS.Communication.Msb;
using ACS.Communication.Http;
using ACS.Core.Message;
using ACS.Control;
using ACS.Core.Workflow;
using ACS.Utility;

namespace ACS.App
{
    /// <summary>
    /// Autofac DI 기반 애플리케이션 초기화.
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
        public const string TYPE_UI = "ui";

        private readonly ILogManager _logManager;
        private readonly IApplicationManager _applicationManager;
        private readonly IApplicationControlManager _applicationControlManager;
        private readonly IEnumerable<IMsbControllable> _msbControllables;
        private readonly IEnumerable<IMessageAgent> _messageAgents;
        private readonly Lazy<IWorkflowManager> _workflowManager;
        private readonly ICacheManagerEx _cacheManager;
        private readonly IControlServerManager _controlServerManager;
        private readonly IServiceLocator _serviceLocator;
        private readonly ILifetimeScope _scope;
        private readonly IConfiguration _configuration;

        public ApplicationInitializer(
            ILifetimeScope scope,
            IConfiguration configuration,
            IEnumerable<IMsbControllable> msbControllables,
            IServiceLocator serviceLocator,
            ILogManager logManager = null,
            IApplicationManager applicationManager = null,
            IApplicationControlManager applicationControlManager = null,
            Lazy<IWorkflowManager> workflowManager = null,
            ICacheManagerEx cacheManager = null,
            IControlServerManager controlServerManager = null,
            IEnumerable<IMessageAgent> messageAgents = null)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logManager = logManager;
            _applicationManager = applicationManager;
            _applicationControlManager = applicationControlManager;
            _msbControllables = msbControllables ?? Enumerable.Empty<IMsbControllable>();
            _messageAgents = messageAgents ?? Enumerable.Empty<IMessageAgent>();
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
            Core.Application.Model.Application application = null;

            if (executor.Type.Equals(TYPE_TS))
            {

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
                SetApplicationContextToApplicationControlManager();
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
            }
            else if (executor.Type.Equals(TYPE_MS))
            {

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

                SetApplicationContextWorkflowManager();
                SetApplicationContextToApplicationControlManager();
                SetReloadableToApplicationControlManager(executor);
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
                InvokeStartWorkflow(application, "COMMON-START-HOST");
            }
            else if (executor.Type.Equals(TYPE_UI))
            {
                SetApplicationContextToApplicationControlManager();
                StartMsb(executor);
                application = CreateOrUpdateApplication(executor);
                SynchronizeCache();
                DisplayDataSource();
                // HTTP/SignalR 서버는 ASP.NET Core(Kestrel)가 호스팅하므로 여기서는 시작하지 않는다.
            }
            else if (executor.Type.Equals(TYPE_EMULATOR))
            {
                // emulator: no initialization needed
            }
            else
            {
                string message = "please check process type, it should be(trans|ei|daemon|control|host|emulator|report|query|ui)";
                throw new ApplicationException(message);
            }
        }

        #region Initialization Steps


        private void SetApplicationContextWorkflowManager()
        {
            try
            {
                // BizProcessManager에 IServiceLocator 설정
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
                // Autofac 전환 후 과도기 호환을 위해 null 허용
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        private void SetReloadableToApplicationControlManager(Executor executor)
        {
            if (_applicationControlManager == null) return;
            if (executor.UseService)
            {
                _applicationControlManager.ReloadableDirectory = executor.ServicePath;
            }
        }

        private void StartMsb(Executor executor)
        {
            if (executor.Msb.Equals("none"))
            {
                return;
            }

            // Autofac IEnumerable<IMsbControllable>로 모든 MSB 제어 가능 객체를 순회하여 시작
            foreach (IMsbControllable msbControllable in _msbControllables.Where(m => m != null))
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

        private Core.Application.Model.Application CreateOrUpdateApplication(Executor executor)
        {
            Core.Application.Model.Application application = null;

            if (_applicationManager == null)
            {
                logger.Warn("applicationManager is not defined — skipping CreateOrUpdateApplication");
                return null;
            }

            try
            {
                application = _applicationManager.GetApplication(executor.Id);

                if (application == null)
                {
                    DateTime date = DateTime.UtcNow;

                    application = new Core.Application.Model.Application();
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
                    defaultDestinationName = defaultDestinationName.Replace("@{site}", _configuration["Acs:Site:Name"]);
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
                    DateTime date = DateTime.UtcNow;

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
                    defaultDestinationName = defaultDestinationName.Replace("@{site}", _configuration["Acs:Site:Name"]);

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
            if (_applicationManager == null) return;
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
            //_controlServerManager.ScheduleUiTransport();
            //_controlServerManager.ScheduleUiApplicationManager();
            //_controlServerManager.ScheduleUiCommand();
        }

        private void InvokeStartWorkflow(Core.Application.Model.Application application, string messageName)
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

        private void StartHttpApiServer()
        {
            try
            {
                var httpServer = _scope.Resolve<HttpCommServer>();
                httpServer.httpServerListenIP = _configuration["Acs:Api:ListenIP"] ?? "any";
                httpServer.httpServerListenPort = _configuration["Acs:Api:ListenPort"] ?? "5100";
                httpServer.Start();
                logger.Info("HTTP API server started on port " + httpServer.httpServerListenPort);
            }
            catch (Exception e)
            {
                logger.Error("Failed to start HTTP API server", e);
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
