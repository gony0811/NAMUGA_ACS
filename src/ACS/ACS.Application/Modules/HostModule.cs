using System;
using Autofac;
using Microsoft.Extensions.Hosting;
using ACS.Framework.Base;
using ACS.Framework.Application;
using ACS.Framework.Resource;
using ACS.Framework.Material;
using ACS.Framework.Transfer;
using ACS.Framework.History;
using ACS.Framework.Cache;
using ACS.Framework.Path;
using ACS.Framework.Message;
using ACS.Framework.Host;
using ACS.Communication.Msb;

namespace ACS.Application.Modules
{
    /// <summary>
    /// host 프로세스(HS01_P) 전용 서비스 등록 모듈.
    /// TransModule에서 host에 필요한 서비스만 추출하고,
    /// TCP/IP 게이트웨이 + 브릿지 서비스를 추가.
    ///
    /// Host 프로세스는 외부 Host(MES) TCP/IP ↔ ACS RabbitMQ 간 브릿지 역할.
    /// - 수신: Host TCP → MOVECMD/MOVECANCEL/MOVEUPDATE → RabbitMQ → Trans
    /// - 송신: Trans → RabbitMQ → TRSJOBREPORT/TRSSTATEREPORT → Host TCP
    /// </summary>
    public class HostModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // === 공통 매니저 (TransModule과 동일 패턴) ===
            builder.RegisterType<ApplicationManagerImplement>()
                .As<IApplicationManager>()
                .SingleInstance()
                .PropertiesAutowired();

            RegisterManager(builder, "ACS.Manager.Resource.ResourceManagerExImplement, ACS.Manager",
                typeof(IResourceManagerEx), initOnActivated: true);

            RegisterManager(builder, "ACS.Manager.Material.MaterialManagerExImplement, ACS.Manager",
                typeof(IMaterialManagerEx));

            RegisterManager(builder, "ACS.Manager.Transfer.TransferManagerExImplement, ACS.Manager",
                typeof(ITransferManagerEx));

            RegisterManager(builder, "ACS.Manager.History.HistoryManagerExImplement, ACS.Manager",
                typeof(IHistoryManagerEx));

            RegisterManager(builder, "ACS.Manager.Application.ApplicationControlManagerExImplement, ACS.Manager",
                typeof(IApplicationControlManager));

            RegisterManager(builder, "ACS.Manager.Path.PathManagerExImplement, ACS.Manager",
                typeof(IPathManagerEx), initOnActivated: true);

            RegisterManager(builder, "ACS.Manager.CacheManagerExImplement, ACS.Manager",
                typeof(ICacheManagerEx));

            // MessageManager
            RegisterManager(builder, "ACS.Manager.MessageManagerExsImplement, ACS.Manager",
                typeof(IMessageManagerEx), initOnActivated: true);

            // HostMessageManager (Host 메시지 변환 — host 프로세스의 핵심)
            RegisterManager(builder, "ACS.Manager.Host.HostMessageManagerImplement, ACS.Manager",
                typeof(IHostMessageManager));

            // DuplicatedChecker (메시지 중복 체크)
            builder.RegisterType<ACS.Communication.Socket.Checker.DuplicatedCheckerImpl>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

            // MsbConverter (XML 변환)
            builder.RegisterType<ACS.Communication.Msb.Convert.Implement.MsbXmlConverterExImplement>()
                .As<IMsbConverter>()
                .SingleInstance()
                .PropertiesAutowired()
                .OnActivated(e => e.Instance.GetType().GetMethod("Init")?.Invoke(e.Instance, null));

            // Elsa Workflows 3 — hybrid bridge (Elsa + legacy WorkflowManagerImpl)
            builder.RegisterModule<ACS.Elsa.ElsaModule>();

            // === Host 전용: TCP 게이트웨이 + 브릿지 서비스 ===

            // TCP/IP 게이트웨이 (Host 듀얼 포트: 수신 3334, 송신 3333)
            builder.RegisterType<ACS.Communication.Host.HostTcpGateway>()
                .As<IHostTcpGateway>()
                .SingleInstance()
                .PropertiesAutowired();

            // 브릿지 BackgroundService (TCP ↔ RabbitMQ)
            builder.RegisterType<ACS.Application.Host.HostBridgeService>()
                .As<IHostedService>()
                .AsSelf()
                .SingleInstance();
        }

        /// <summary>
        /// 매니저 타입을 문자열로 resolve하여 등록 (순환 참조 방지).
        /// </summary>
        private void RegisterManager(ContainerBuilder builder, string typeName, Type serviceType,
            bool initOnActivated = false)
        {
            var implType = Type.GetType(typeName);
            if (implType == null)
            {
                System.Diagnostics.Debug.WriteLine($"[HostModule] Manager type not found: {typeName}");
                return;
            }

            var reg = builder.RegisterType(implType)
                .As(serviceType)
                .SingleInstance()
                .PropertiesAutowired();

            if (initOnActivated)
                reg.OnActivated(e => ((AbstractManager)e.Instance).Init());
        }
    }
}
