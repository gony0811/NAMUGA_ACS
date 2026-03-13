using System;
using System.Collections.Specialized;
using System.Configuration;
using Autofac;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Logging;
using ACS.Framework.Message;
using ACS.Communication.Msb;
using ACS.Communication.Msb.RabbitMQ;
using ACS.Workflow;

namespace ACS.Application.Modules
{
    /// <summary>
    /// RabbitMQ MSB 인프라 등록 모듈.
    /// Spring XML의 *-interface.xml 및 *-control.xml 설정을 대체.
    /// </summary>
    public class MsbRabbitMQModule : Module
    {
        private readonly string _processType;

        public MsbRabbitMQModule(string processType)
        {
            _processType = processType;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var dest = (NameValueCollection)ConfigurationManager.GetSection("Destination");

            // ${...} placeholder 치환 (Spring PropertyPlaceholderConfigurer 대체)
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
                case "host":
                    RegisterTransMsb(builder, dest, serverConnectUrl, serverUserName, serverPassword, serverStationMode,
                        hostConnectUrl, hostUserName, hostPassword, hostStationMode, xpathOfMessageName);
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

            // Senders
            RegisterSender(builder, "TsAgentSender", "TsAgentSender", acsSenderDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);

            RegisterSender(builder, "UiAgentSender", "UiAgentSender", uiSenderDest, "UNICAST",
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

            // Listener
            RegisterWorkflowListener(builder, "CsListener", "CsListener", csListenerDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation, xpathOfMessageName);

            // Senders
            RegisterSender(builder, "CsSenderToServer", "CsSender", acsSenderDest, "UNICAST",
                serverUrl, serverUser, serverPass, serverStation);

            RegisterSender(builder, "CsSenderToUi", "CsSender", uiSenderDest, "MULTICAST",
                serverUrl, serverUser, serverPass, serverStation);
        }

        // --- Helper methods ---

        /// <summary>
        /// ${key} 형식의 placeholder를 동일 NameValueCollection 내 값으로 치환.
        /// Spring PropertyPlaceholderConfigurer 대체.
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
            var d = new ApplicationNameChannelDestination { Name = name ?? "" };
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
                listener.LogManager = c.Resolve<ILogManager>();
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
            .SingleInstance();
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
                listener.LogManager = c.Resolve<ILogManager>();
                listener.ApplicationControlManager = c.Resolve<IApplicationControlManager>();
                listener.MessageManager = c.Resolve<IMessageManagerEx>();
                listener.Destination = destination;
                listener.Name = "ApplicationControlAgentListener";
                listener.CastOption = "UNICAST";
                listener.DefaultTTL = 60000;
                listener.Init();
                return listener;
            })
            .Named<IMsbControllable>("ApplicationControlAgentListener")
            .As<IMsbControllable>()
            .SingleInstance();
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
                sender.LogManager = c.Resolve<ILogManager>();
                sender.DefaultDestination = defaultDestination;
                sender.Name = senderName;
                sender.CastOption = castOption;
                sender.DefaultTTL = 60000;
                sender.Init();
                return sender;
            })
            .Named<IMessageAgent>(registrationName)
            .As<IMessageAgent>()
            .SingleInstance();
        }
    }
}
