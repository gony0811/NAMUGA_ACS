using System;
using Autofac;
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

                // ACS.Elsa 어셈블리의 모든 Activity 등록 (ReflectionActivityBase 파생 + HeartBeatActivities + HostActivities 등)
                elsa.AddActivitiesFrom<Activities.ReflectionActivityBase>();

                // ACS.Elsa 어셈블리의 모든 워크플로우 등록 (ControlStartHeartBeatWorkflow, HostMoveCmdWorkflow 등)
                elsa.AddWorkflowsFrom<Workflows.ControlStartHeartBeatWorkflow>();
            });

            // 2. Build the ServiceProvider from Elsa's IServiceCollection
            var serviceProvider = services.BuildServiceProvider();

            // 3. Register Elsa services into Autofac by resolving from the ServiceProvider
            builder.RegisterInstance(serviceProvider)
                .As<IServiceProvider>()
                .Named<IServiceProvider>("ElsaServiceProvider")
                .SingleInstance();

            // AutofacContainerAccessor를 Autofac에도 등록 (Executor에서 Container 설정용)
            builder.RegisterInstance(autofacAccessor)
                .AsSelf()
                .SingleInstance();

            builder.Register(c => serviceProvider.GetRequiredService<IWorkflowRunner>())
                .As<IWorkflowRunner>()
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
            builder.RegisterType<ElsaWorkflowManagerBridge>()
                .As<IWorkflowManager>()
                .SingleInstance();

            logger.Info("ElsaModule loaded: Elsa Workflows 3 integrated with hybrid bridge.");
        }

    }
}
