using System;
using Autofac;
using Autofac.Core;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using ACS.Elsa.Bridge;
using ACS.Core.Workflow;
using ACS.Core.Logging;

namespace ACS.Elsa
{
    /// <summary>
    /// Autofac module that integrates Elsa Workflows 3 into the ACS process.
    ///
    /// Registers:
    /// - Elsa runtime services (workflow engine, activity registry, SQLite persistence)
    /// - ElsaWorkflowManagerBridge as IWorkflowManager (replacing direct WorkflowManagerImpl)
    /// - BizJobActivity for wrapping existing BaseBizJob handlers
    ///
    /// The bridge reads elsa-migration.json to determine which commands
    /// route to Elsa vs legacy WorkflowManagerImpl.
    /// </summary>
    public class ElsaModule : Autofac.Module
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ElsaModule));

        protected override void Load(ContainerBuilder builder)
        {
            // 0. Autofac ↔ Elsa 브릿지 (Activity에서 Autofac 서비스 접근용)
            var autofacAccessor = new AutofacContainerAccessor();

            // 1. Build an IServiceCollection with Elsa services
            var services = new ServiceCollection();

            // AutofacContainerAccessor를 Elsa의 IServiceCollection에 등록
            services.AddSingleton(autofacAccessor);

            services.AddElsa(elsa =>
            {
                elsa.UseWorkflowRuntime(runtime =>
                {
                    // Use default in-memory runtime
                });

                // ACS.Elsa 어셈블리의 모든 워크플로우 등록 (ControlStartHeartBeatWorkflow, HostMoveCmdWorkflow 등)
                elsa.AddWorkflowsFrom<Workflows.ControlStartHeartBeatWorkflow>();
            });

            // 2. Build the ServiceProvider from Elsa's IServiceCollection
            var serviceProvider = services.BuildServiceProvider();

            // 3. Elsa의 격리된 IServiceProvider는 "named"로만 등록한다.
            //    주의: .As<IServiceProvider>()로 기본 등록하면 control(웹 호스트)에서
            //    Kestrel의 DiagnosticSource 팩토리(sp => sp.GetRequiredService<DiagnosticListener>())가
            //    이 격리 provider를 기본 IServiceProvider로 잡아 DiagnosticListener 해석에 실패한다
            //    (격리 provider에는 호스트 프레임워크 서비스가 없음). 콘솔 프로세스는 Kestrel이 없어 무관.
            builder.RegisterInstance(serviceProvider)
                .Named<IServiceProvider>("ElsaServiceProvider")
                .SingleInstance();

            // AutofacContainerAccessor를 Autofac에도 등록 (Executor에서 Container 설정용)
            builder.RegisterInstance(autofacAccessor)
                .AsSelf()
                .SingleInstance();

            // Elsa scope factory도 "named"로만 등록 — 호스트의 기본 IServiceScopeFactory를 덮어쓰지 않는다.
            // Bridge가 매 워크플로우 호출마다 이 scope factory로 새 scope를 만들어 IWorkflowRunner(scoped)를 해석한다.
            builder.Register(c => serviceProvider.GetRequiredService<IServiceScopeFactory>())
                .Named<IServiceScopeFactory>("ElsaScopeFactory")
                .SingleInstance();

            // 4. Legacy WorkflowManagerImpl (still needed for non-Elsa commands)
            builder.RegisterType<WorkflowManagerImpl>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

            // 5. BizProcessManager (still needed for legacy path)
            builder.RegisterType<BizProcessManager>()
                .AsSelf()
                .SingleInstance()
                .PropertiesAutowired();

            // 6. ElsaWorkflowManagerBridge as IWorkflowManager
            //    Routes to Elsa or legacy based on elsa-migration.json
            //    생성자의 IServiceScopeFactory에는 위에서 named로 등록한 Elsa scope factory를 명시 주입.
            builder.RegisterType<ElsaWorkflowManagerBridge>()
                .Named<IWorkflowManager>("elsaWorkflowManager")
                .As<IWorkflowManager>()
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(IServiceScopeFactory),
                    (pi, ctx) => ctx.ResolveNamed<IServiceScopeFactory>("ElsaScopeFactory")))
                .SingleInstance();

            logger.Info("ElsaModule loaded: Elsa Workflows 3 integrated with hybrid bridge.");
        }

    }
}
