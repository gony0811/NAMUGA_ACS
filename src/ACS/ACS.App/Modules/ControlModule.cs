using System;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Hosting;
using ACS.App.Web.Realtime;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.Transfer;
using ACS.Core.History;
using ACS.Core.Message;
using ACS.Core.Cache;
using ACS.Core.Alarm;
using ACS.Control;

namespace ACS.App.Modules
{
    /// <summary>
    /// control 프로세스 전용 서비스 등록.
    /// config/{SITE}/Startup/acs/control/control-manager.xml을 대체.
    ///
    /// control 프로세스가 UI 백엔드(REST API + SignalR)도 겸하므로,
    /// 기존 UiModule이 등록하던 CacheManager와 실시간 구독자(PoseTelemetrySubscriber,
    /// HostCommSubscriber)를 함께 등록한다. REST 컨트롤러가 요구하는 IResourceManagerEx/
    /// ITransferManagerEx는 본 모듈이 이미 등록한다. (REST/SignalR 호스팅은 Program.cs의 웹 호스트가 담당.)
    /// </summary>
    public class ControlModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationManagerImplement>()
                .As<IApplicationManager>()
                .SingleInstance()
                .PropertiesAutowired();

            // ACS.Manager types resolved by name to avoid circular project reference
            var resourceMgrType = Type.GetType("ACS.Manager.Resource.ResourceManagerExImplement, ACS.Manager");
            if (resourceMgrType != null)
                builder.RegisterType(resourceMgrType)
                    .As<IResourceManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());

            var materialMgrType = Type.GetType("ACS.Manager.Material.MaterialManagerExImplement, ACS.Manager");
            if (materialMgrType != null)
                builder.RegisterType(materialMgrType)
                    .As<IMaterialManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // Spring XML에서는 TransferManagerExsImplement (ITransferManagerExs) 사용
            var transferMgrType = Type.GetType("ACS.Manager.TransferManagerExsImplement, ACS.Manager");
            if (transferMgrType != null)
                builder.RegisterType(transferMgrType)
                    .As<ITransferManagerEx>()
                    .As(Type.GetType("ACS.Core.Transfer.ITransferManagerExs, ACS.Core"))
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((ACS.Core.Base.AbstractManager)e.Instance).Init());

            var historyMgrType = Type.GetType("ACS.Manager.History.HistoryManagerExImplement, ACS.Manager");
            if (historyMgrType != null)
                builder.RegisterType(historyMgrType)
                    .As<IHistoryManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            var appControlMgrType = Type.GetType("ACS.Manager.Application.ApplicationControlManagerExImplement, ACS.Manager");
            if (appControlMgrType != null)
                builder.RegisterType(appControlMgrType)
                    .As<IApplicationControlManager>()
                    .SingleInstance()
                    .PropertiesAutowired();

            builder.RegisterType<ControlServerManagerImplement>()
                .As<IControlServerManager>()
                .SingleInstance()
                .PropertiesAutowired()
                .OnActivated(e =>
                {
                    var mgr = (ControlServerManagerImplement)e.Instance;
                    mgr.Init();
                    // Scheduling job types (protected Type — set via reflection)
                    void SetProtected(string name, object value)
                    {
                        var prop = typeof(ControlServerManagerImplement).GetProperty(name,
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        prop?.SetValue(mgr, value);
                    }
                    // Job Type은 ControlServerManagerImplement.Init()에서 typeof()로 설정됨.
                    // 이전에 Type.GetType("..., ACS.Control")로 덮어쓰던 코드 제거 —
                    // ACS.Control 어셈블리가 존재하지 않아(실제: ACS.App) null을 반환하여
                    // HeartBeat/Reschedule/SimpleHeartBeat 스케줄링이 모두 실패했음.
                    SetProtected("WindowRedirectFilePath", "log/server/control/start");
                    // HeartBeat 전송 대상 큐의 prefix 설정 (예: VM/DEMO/CONTROL/AGENT)
                    // ${server.domain} placeholder는 MsbRabbitMQModule에서 이미 치환되지 않으므로
                    // IConfiguration에서 직접 조합
                    var domainValue = mgr.Configuration?["Destination:Server:DomainValue"] ?? "VM/DEMO";
                    SetProtected("DestinationNamePrefix", domainValue + "/CONTROL/AGENT");
                    // Feature toggles
                    mgr.UseHeartBeat = true;
                    mgr.UseUiTransport = true;
                    mgr.UseUiCommand = true;
                    mgr.UseUiApplicationManager = false;
                    // HeartBeat settings
                    mgr.HeartBeatInterval = 20000;
                    mgr.HeartBeatStartDelay = 10000;
                    mgr.HeartBeatTimeout = 5000;
                    mgr.HeartBeatRetryCount = 3;
                    mgr.HeartBeatRetryTimeout = 10000;
                    // 기동 유예(ms): 워커가 control-agent 리스너를 띄우기까지(~30s 관측)의 시간을 감안.
                    // 너무 짧으면 부팅 중 워커를 hang으로 오판해 Kill→Start 루프 발생. 환경별 조정은
                    // appsettings의 Acs:Control:HeartBeatStartupGraceMs로 override 가능(기본 60000).
                    mgr.HeartBeatStartupGrace =
                        long.TryParse(mgr.Configuration?["Acs:Control:HeartBeatStartupGraceMs"], out var graceMs)
                            ? graceMs : 60000;
                    mgr.SimpleHeartBeatInterval = 5000;
                    mgr.SimpleHeartBeatStartDelay = 2000;
                    mgr.HeartBeatFailWhenProcessDown = 2;
                    mgr.HeartBeatFailWhenProcessHang = 2;
                    // UI intervals
                    mgr.UiCommandInterval = 1;
                    mgr.UiTransportInterval = 1;
                    mgr.UiTransportStartDelay = 10000;
                    mgr.UiApplicationManagerInterval = 3000;
                    mgr.UiApplicationManagerStartDelay = 10000;
                    // System settings
                    mgr.UseSystemKill = true;
                    mgr.UseSystemGetProcessId = true;
                    // 스크립트 경로는 appsettings.json의 Acs:Control:Scripts 섹션에서 로드.
                    // (예: "TS-START": "D:\\ACS\\deploy\\TS01_P\\TS01_P.exe")
                    // 키는 ControlServerManagerImplement.SCRIPT_*_START 상수와 일치해야 함.
                    // 미설정(또는 경로에 파일 없음) 시 ControlServerManagerImplement가 CS 실행 위치
                    // 기준 형제 폴더에서 <이름>.exe 를 convention 으로 탐색하므로 생략 가능
                    // (ResolveStartScriptByConvention 참고). Scripts 명시는 override 용도.
                    var scripts = new System.Collections.Hashtable();
                    var scriptsSection = mgr.Configuration?.GetSection("Acs:Control:Scripts");
                    if (scriptsSection != null)
                        foreach (var child in scriptsSection.GetChildren())
                            scripts[child.Key] = child.Value;
                    mgr.Scripts = scripts;
                });

            // MessageManager
            var messageMgrType = Type.GetType("ACS.Manager.MessageManagerExsImplement, ACS.Manager");
            if (messageMgrType != null)
                builder.RegisterType(messageMgrType)
                    .As<IMessageManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());

            // AlarmManager
            var alarmMgrType = Type.GetType("ACS.Manager.Alarm.AlarmManagerExImplement, ACS.Manager");
            if (alarmMgrType != null)
                builder.RegisterType(alarmMgrType)
                    .As<IAlarmManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // CacheManager — UI 백엔드 매니저들의 PropertiesAutowired 주입 대상.
            var cacheMgrType = Type.GetType("ACS.Manager.CacheManagerExImplement, ACS.Manager");
            if (cacheMgrType != null)
                builder.RegisterType(cacheMgrType)
                    .As<ICacheManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();

            // UI 백엔드 실시간 구독자 (기존 UiModule에서 이전).
            // 자체 RabbitMQ fanout 커넥션을 열어 SignalR로 브로드캐스트하며, 웹 호스트의
            // Generic Host가 IHostedService로 자동 기동한다.
            // Trans → UI(RabbitMQ fanout) → SignalR(VehicleHub) POSE 브로드캐스트.
            builder.RegisterType<PoseTelemetrySubscriber>()
                .As<IHostedService>()
                .SingleInstance();

            // Host(MES) TCP 통신 로그(/UI/HOSTCOMM fanout) → SignalR HostCommHub 브로드캐스트.
            builder.RegisterType<HostCommSubscriber>()
                .As<IHostedService>()
                .SingleInstance();

            // Elsa Workflows 3 — hybrid bridge (Elsa + legacy WorkflowManagerImpl)
            // Elsa Workflows 3
            builder.RegisterModule<ACS.Elsa.ElsaModule>();
        }
    }
}
