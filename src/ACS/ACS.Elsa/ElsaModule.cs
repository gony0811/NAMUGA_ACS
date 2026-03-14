using System;
using System.IO;
using Autofac;
using Elsa.Extensions;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using ACS.Elsa.Bridge;
using ACS.Workflow;
using ACS.Framework.Logging;

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
    public class ElsaModule : Module
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ElsaModule));

        protected override void Load(ContainerBuilder builder)
        {
            // 1. Build an IServiceCollection with Elsa services
            var services = new ServiceCollection();

            services.AddElsa(elsa =>
            {
                elsa.UseWorkflowRuntime(runtime =>
                {
                    // Use default in-memory runtime
                });

                // Register custom Activities (scans entire ACS.Elsa assembly)
                elsa.AddActivitiesFrom<Activities.ReflectionActivityBase>();
            });

            // 2. Build the ServiceProvider from Elsa's IServiceCollection
            var serviceProvider = services.BuildServiceProvider();

            // 3. Register Elsa services into Autofac by resolving from the ServiceProvider
            builder.RegisterInstance(serviceProvider)
                .As<IServiceProvider>()
                .Named<IServiceProvider>("ElsaServiceProvider")
                .SingleInstance();

            builder.Register(c => serviceProvider.GetRequiredService<IWorkflowDispatcher>())
                .As<IWorkflowDispatcher>()
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
