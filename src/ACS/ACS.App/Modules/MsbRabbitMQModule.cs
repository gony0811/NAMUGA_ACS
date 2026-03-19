using System;
using System.Collections.Specialized;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.Core.Application;
using ACS.Core.Application.Model;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Communication.Msb;
using ACS.Communication.Msb.RabbitMQ;
using ACS.Core.Workflow;

namespace ACS.App.Modules
{
    /// <summary>
    /// RabbitMQ MSB 인프라 등록 모듈.
    /// </summary>
    public class MsbRabbitMQModule : Module
    {
        private readonly string _processType;
        private readonly IConfiguration _configuration;

        public MsbRabbitMQModule(string processType, IConfiguration configuration)
        {
            _processType = processType;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override void Load(ContainerBuilder builder)
        {
            // IConfiguration의 "Destination" 섹션에서 NameValueCollection 빌드
            // 중첩된 JSON 구조를 점(.) 구분 소문자 키로 재귀 평탄화
            // 예: Destination:Server:Domain:ConnectUrl → server.domain.connecturl
            var dest = new NameValueCollection();
            var destSection = _configuration.GetSection("Destination");
            FlattenSection(destSection, "", dest);

            // 하위 호환성: ${server.domain} placeholder가 "VM/DEMO" 등 DomainValue를 참조하도록
            // (JSON 구조에서 Server:Domain은 섹션이므로 값이 없고, Server:DomainValue에 실제 값 존재)
            if (dest["server.domain"] == null && dest["server.domainvalue"] != null)
                dest["server.domain"] = dest["server.domainvalue"];
            if (dest["host.domain"] == null && dest["host.domainvalue"] != null)
                dest["host.domain"] = dest["host.domainvalue"];

            // ${...} placeholder 치환
            dest = ResolvePlaceholders(dest);

            string serverConnectUrl = dest["server.domain.connecturl"] ?? "localhost";
            string serverUserName = dest["server.domain.username"] ?? "guest";
            string serverPassword = dest["server.domain.password"] ?? "guest";
            string serverStationMode = dest["server.domain.stationmode"] ?? "INTER";
            string hostConnectUrl = dest["host.domain.connecturl"] ?? "localhost";
            string hostUserName = dest["host.domain.username"] ?? "guest";
            string hostPassword = dest["host.domain.password"] ?? "guest";
            string hostStationMode = dest["host.domain.stationmode"] ?? "INTER";
            string xpathOfMessageName = dest["server.ts.xpathofmessagename.default"] ?? "/MESSAGE/HEADER/MESSAGENAME";

            switch (_processType)
            {
                case "trans":
                case "query":
                case "report":
                case "ui":
                    RegisterTransMsb(builder, dest, serverConnectUrl, serverUserName, serverPassword, serverStationMode,
                        hostConnectUrl, hostUserName, hostPassword, hostStationMode, xpathOfMessageName);
                    break;
                case "host":
                    RegisterHostMsb(builder, dest, hostConnectUrl, hostUserName, hostPassword, hostStationMode, xpathOfMessageName);
                    break;
                case "ei":
                    RegisterEiMsb(builder, dest, serverConnectUrl, serverUserName, serverPassword, serverStationMode, xpathOfMessageName);
                    break;
                case "daemon":
                    RegisterDaemonMsb(builder, dest, serverConnectUrl, serverUserName, serverPassword, serverStationMode);
                    break;
                case "control":
                    RegisterControlMsb(builder, dest, serverConnectUrl, serverUserName, serverPassword, serverStationMode, xpathOfMessageName);
                    break;
            }
        }

        private void RegisterHostMsb(ContainerBuilder builder, NameValueCollection dest,
            string hostUrl, string hostUser, string hostPass, string hostStation,
            string xpathOfMessageName)
        {
            // Host XPath: /Msg/Command
            string hostXpath = dest["server.trans.xpathofmessagename.host"] ?? "/Msg/Command";

            // Default destination = TS Listener (Host가 보내는 곳)
            RegisterDefaultDestination(builder, dest["server.ts.host.listener.destination"]);

            // Sender dest: TS의 Host Listener 큐 (Host → TS)
            var senderDest = CreateChannelDestination(dest["server.ts.host.listener.destination"]);
            // Listener dest: TS의 Host Sender 큐 (TS → Host)
            var listenerDest = CreateChannelDestination(dest["server.ts.host.sender.destination"]);

            // Listener: TS가 보낸 메시지 수신
            RegisterWorkflowListener(builder, "HostListener", "HOSTLISTENER", listenerDest, "UNICAST",
                hostUrl, hostUser, hostPass, hostStation, hostXpath);

            // Sender: TS로 메시지 전송
            RegisterSender(builder, "HostAgentSender", "HOSTSENDER", senderDest, "UNICAST",
                hostUrl, hostUser, hostPass, hostStation);
        }

        private void RegisterTransMsb(ContainerBuilder builder, NameValueCollection dest,
            string serverUrl, string serverUser, string serverPass, string serverStation,
            string hostUrl, string hostUser, string hostPass, string hostStation,
            string xpathOfMessageName)
        {
            // ApplicationDefaultDestinationName
            RegisterDefaultDestination(builder, dest["server.ts.host.listener.destination"]);

            // ChannelDestinations
            var hostSenderDest = CreateChannelDestination(dest["server.ts.host.sender.destination"]);
            var hostListenerDest = CreateChannelDestination(dest["server.ts.host.listener.destination"]);
            var eiSenderDest = CreateChannelDestination(dest["server.ts.es.sender.destination"]);
            var eiListenerDest = CreateChannelDestination(dest["server.ts.es.listener.destination"]);
            var uiSenderDest = CreateChannelDestination(dest["server.ts.ui.sender.destination"]);
            var uiListenerDest = CreateChannelDestination(dest["server.ts.ui.listener.destination"]);
            var daemonListenerDest = CreateChannelDestination(dest["server.ts.daemon.listener.destination"]);
            var controlListenerDest = CreateChannelDestination(dest["server.control.ts.sender.destination"]);
            var appNameDest = CreateApplicationNameChannelDestination(dest["server.es.ts.listener.destination"]);
            var appControlDest = CreateApplicationNameChannelDestination(dest["server.ts.control.listener.destination"]);

            // Listeners (IMsbControllable)
            RegisterWorkflowListener(builder, "HostListener", "HOSTLISTENER", hostListenerDest, "UNICAST",
                hostUrl, hostUser, hostPass, hostStation, xpathOfMessageName);

            RegisterWorkflowListener(builder, "EsListener", "ESLISTENER", eiListenerDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            RegisterWorkflowListener(builder, "UiListener", "UILISTENER", uiListenerDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            RegisterWorkflowListener(builder, "DaemonListener", "DAEMONLISTENER", daemonListenerDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            RegisterWorkflowListener(builder, "ControlListener", "DAEMONLISTENER", controlListenerDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            // ApplicationControlAgentListener
            RegisterControlAgentListener(builder, appControlDest,
                serverUrl, serverUser, serverPass, serverStation);

            // Senders (IMessageAgent)
            RegisterSender(builder, "HostAgentSender", "HOSTSENDER", hostSenderDest, "UNICAST",
                hostUrl, hostUser, hostPass, hostStation);

            RegisterSender(builder, "EsAgentSender", "ESSENDER", appNameDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);

            RegisterSender(builder, "UiAgentSender", "UISENDER", uiSenderDest, "MULTICAST",
                serverUrl, serverUser, serverPass, serverStation);
        }

        private void RegisterEiMsb(ContainerBuilder builder, NameValueCollection dest,
            string serverUrl, string serverUser, string serverPass, string serverStation,
            string xpathOfMessageName)
        {
            RegisterDefaultDestination(builder, dest["server.es.ts.listener.destination"]);

            var appNameDest = CreateApplicationNameChannelDestination(dest["server.es.ts.listener.destination"]);
            var acsSenderDest = CreateChannelDestination(dest["server.es.ts.sender.destination"]);
            var appControlDest = CreateApplicationNameChannelDestination(dest["server.es.control.listener.destination"]);

            // Listener
            RegisterWorkflowListener(builder, "TransAgentListener", "TsAgentListener", appNameDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            // ApplicationControlAgentListener
            RegisterControlAgentListener(builder, appControlDest,
                serverUrl, serverUser, serverPass, serverStation);

            // Sender
            RegisterSender(builder, "TransAgentSender", "tsAgentSender", acsSenderDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);
        }

        private void RegisterDaemonMsb(ContainerBuilder builder, NameValueCollection dest,
            string serverUrl, string serverUser, string serverPass, string serverStation)
        {
            RegisterDefaultDestination(builder, dest["server.daemon.ts.sender.destination"]);

            var uiSenderDest = CreateChannelDestination(dest["server.daemon.ui.sender.destination"]);
            var acsSenderDest = CreateChannelDestination(dest["server.daemon.ts.sender.destination"]);
            var appControlDest = CreateApplicationNameChannelDestination(dest["server.daemon.control.listener.destination"]);

            // ApplicationControlAgentListener
            RegisterControlAgentListener(builder, appControlDest,
                serverUrl, serverUser, serverPass, serverStation);

            // Senders — TsAgentSender를 마지막에 등록하여 단일 IMessageAgent resolve 시 반환되도록 함
            RegisterSender(builder, "UiAgentSender", "UiAgentSender", uiSenderDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);

            RegisterSender(builder, "TsAgentSender", "TsAgentSender", acsSenderDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);
        }

        private void RegisterControlMsb(ContainerBuilder builder, NameValueCollection dest,
            string serverUrl, string serverUser, string serverPass, string serverStation,
            string xpathOfMessageName)
        {
            RegisterDefaultDestination(builder, dest["server.control.control.listener.destination"]);

            var acsSenderDest = CreateChannelDestination(dest["server.control.ts.sender.destination"]);
            var uiSenderDest = CreateChannelDestination(dest["server.control.ui.sender.destination"]);
            var csListenerDest = CreateApplicationNameChannelDestination(dest["server.control.control.listener.destination"]);

            // HeartBeat RPC 전용 destination (prefix만 사용, 실제 queue는 동적)
            var domainValue = dest["server.domainvalue"] ?? "VM/DEMO";
            var heartbeatDest = CreateChannelDestination(domainValue + "/CONTROL/AGENT");

            // Listener
            RegisterWorkflowListener(builder, "CsListener", "CsListener", csListenerDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            // Senders
            RegisterSender(builder, "CsSenderToServer", "CsSender", acsSenderDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);

            RegisterSender(builder, "CsSenderToUi", "CsSender", uiSenderDest, "MULTICAST",
                serverUrl, serverUser, serverPass, serverStation);

            // HeartBeat RPC sender — ISynchronousMessageAgent를 RPCCLIENT로 등록
            RegisterSender(builder, "HeartBeatRpcSender", "HeartBeatRpcSender", heartbeatDest, "RPCCLIENT",
                serverUrl, serverUser, serverPass, serverStation);
        }

        // --- Helper methods ---

        /// <summary>
        /// IConfiguration 섹션을 재귀적으로 탐색하여 점(.) 구분 소문자 키로 NameValueCollection에 추가.
        /// 예: Server:Domain:ConnectUrl → server.domain.connecturl = "localhost"
        /// </summary>
        private void FlattenSection(IConfigurationSection section, string prefix, NameValueCollection dest)
        {
            foreach (var child in section.GetChildren())
            {
                string key = string.IsNullOrEmpty(prefix)
                    ? child.Key.ToLowerInvariant()
                    : prefix + "." + child.Key.ToLowerInvariant();

                if (child.Value != null)
                {
                    // 리프 노드 — 값 저장
                    dest[key] = child.Value;
                }

                // 하위 섹션이 있으면 재귀 탐색 (리프여도 자식이 있을 수 있으므로 항상 탐색)
                FlattenSection(child, key, dest);
            }
        }

        /// <summary>
        /// ${key} 형식의 placeholder를 동일 NameValueCollection 내 값으로 치환.
        /// 예: "${server.domain}/CONTROL" → "VM/DEMO/CONTROL"
        /// </summary>
        private NameValueCollection ResolvePlaceholders(NameValueCollection source)
        {
            var resolved = new NameValueCollection();
            foreach (string key in source.AllKeys)
            {
                string value = source[key];
                if (value != null && value.Contains("${"))
                {
                    // ${xxx} 패턴을 찾아서 치환
                    int startIdx;
                    while ((startIdx = value.IndexOf("${")) >= 0)
                    {
                        int endIdx = value.IndexOf("}", startIdx);
                        if (endIdx < 0) break;

                        string placeholder = value.Substring(startIdx + 2, endIdx - startIdx - 2);
                        string replacement = source[placeholder] ?? "";
                        value = value.Substring(0, startIdx) + replacement + value.Substring(endIdx + 1);
                    }
                }
                resolved[key] = value;
            }
            return resolved;
        }

        private void RegisterDefaultDestination(ContainerBuilder builder, string destinationName)
        {
            builder.Register(c =>
            {
                var d = new ApplicationDefaultDestinationName { DestinationName = destinationName ?? "" };
                d.Init();
                return d;
            }).AsSelf().SingleInstance();
        }

        private ChannelDestination CreateChannelDestination(string name)
        {
            var d = new ChannelDestination(name ?? "");
            d.Init();
            return d;
        }

        private ApplicationNameChannelDestination CreateApplicationNameChannelDestination(string name)
        {
            // .NET 8에서는 ConfigurationManager.AppSettings가 작동하지 않으므로
            // IConfiguration에서 프로세스 이름을 가져와 @{application} 치환
            string processName = _configuration["Acs:Process:Name"] ?? "";
            string resolvedName = (name ?? "").Replace("@{application}", processName);
            var d = new ApplicationNameChannelDestination { Name = resolvedName };
            d.Init();
            return d;
        }

        private void RegisterWorkflowListener(ContainerBuilder builder, string registrationName, string listenerName,
            ChannelDestination destination, string castOption,
            string hostName, string userName, string password, string stationMode, string xpathOfMessageName)
        {
            builder.Register(c =>
            {
                var listener = new GenericWorkflowRabbitMQListener();
                listener.HostName = hostName;
                listener.UserName = userName;
                listener.Password = password;
                listener.StationMode = stationMode;
                listener.LogManager = c.ResolveOptional<ILogManager>();
                listener.WorkflowManager = c.Resolve<IWorkflowManager>();
                listener.XpathOfMessageName = xpathOfMessageName;
                listener.Name = listenerName;
                listener.Destination = destination;
                listener.CastOption = castOption;
                listener.DefaultTTL = 60000;
                listener.Init();
                return listener;
            })
            .Named<IMsbControllable>(registrationName)
            .As<IMsbControllable>()
            .SingleInstance()
            .OnlyIf(reg => reg.IsRegistered(new Autofac.Core.TypedService(typeof(IWorkflowManager))));
        }

        private void RegisterControlAgentListener(ContainerBuilder builder, ChannelDestination destination,
            string hostName, string userName, string password, string stationMode)
        {
            builder.Register(c =>
            {
                var listener = new ApplicationControlAgentRabbitMQListener();
                listener.HostName = hostName;
                listener.UserName = userName;
                listener.Password = password;
                listener.StationMode = stationMode;
                listener.LogManager = c.ResolveOptional<ILogManager>();
                listener.ApplicationControlManager = c.Resolve<IApplicationControlManager>();
                listener.MessageManager = c.ResolveOptional<IMessageManagerEx>();
                listener.Destination = destination;
                listener.Name = "ApplicationControlAgentListener";
                listener.CastOption = "RPCSERVER";
                listener.DefaultTTL = 60000;
                listener.Init();
                return listener;
            })
            .Named<IMsbControllable>("ApplicationControlAgentListener")
            .As<IMsbControllable>()
            .SingleInstance()
            .OnlyIf(reg => reg.IsRegistered(new Autofac.Core.TypedService(typeof(IApplicationControlManager))));
        }

        private void RegisterSender(ContainerBuilder builder, string registrationName, string senderName,
            ChannelDestination defaultDestination, string castOption,
            string hostName, string userName, string password, string stationMode)
        {
            builder.Register(c =>
            {
                var sender = new GenericRabbitMQSender();
                sender.HostName = hostName;
                sender.UserName = userName;
                sender.Password = password;
                sender.StationMode = stationMode;
                sender.LogManager = c.ResolveOptional<ILogManager>();
                sender.DefaultDestination = defaultDestination;
                sender.Name = senderName;
                sender.CastOption = castOption;
                sender.DefaultTTL = 60000;
                sender.Init();
                return sender;
            })
            .Named<IMessageAgent>(registrationName)
            .As<IMessageAgent>()
            .As<ISynchronousMessageAgent>()
            .SingleInstance();
        }
    }
}
