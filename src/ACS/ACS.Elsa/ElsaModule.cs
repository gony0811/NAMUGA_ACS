using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

                // Register custom Activities (scans ACS.Elsa assembly)
                elsa.AddActivitiesFrom<Activities.ReflectionActivityBase>();

                // ACS.Activity 어셈블리에서 Generated activities + workflows 등록
                RegisterActivitiesFromAssembly(elsa, "ACS.Activity");
                RegisterWorkflowsFromAssembly(elsa, "ACS.Activity");
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

        /// <summary>
        /// 외부 어셈블리에서 ReflectionActivityBase 기반 activity들을 Elsa에 등록.
        /// AddActivitiesFrom 제네릭 메서드를 리플렉션으로 호출.
        /// </summary>
        private static void RegisterActivitiesFromAssembly(object elsaFeature, string assemblyName)
        {
            try
            {
                var asm = Assembly.Load(assemblyName);
                var markerType = asm.GetExportedTypes()
                    .FirstOrDefault(t => !t.IsAbstract && typeof(Activities.ReflectionActivityBase).IsAssignableFrom(t));
                if (markerType == null) return;

                // elsa.AddActivitiesFrom<T>() 호출 — 확장 메서드이므로 모든 로드된 어셈블리에서 탐색
                var addMethod = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Array.Empty<Type>(); } })
                    .Where(t => t.Name == "ModuleExtensions" && t.Namespace == "Elsa.Extensions")
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    .FirstOrDefault(m => m.Name == "AddActivitiesFrom" && m.IsGenericMethod);

                if (addMethod != null)
                {
                    addMethod.MakeGenericMethod(markerType).Invoke(null, new object[] { elsaFeature });
                    logger.Info($"{assemblyName} loaded: generated activities registered.");
                }
            }
            catch (FileNotFoundException)
            {
                logger.Warn($"{assemblyName} assembly not found — generated activities will not be available.");
            }
        }

        /// <summary>
        /// 외부 어셈블리에서 WorkflowBase 기반 워크플로우들을 Elsa에 등록.
        /// elsa.AddWorkflowsFrom(assembly) 호출.
        /// </summary>
        private static void RegisterWorkflowsFromAssembly(object elsaFeature, string assemblyName)
        {
            try
            {
                var asm = Assembly.Load(assemblyName);

                // elsa.AddWorkflowsFrom(Assembly) 확장 메서드 찾기
                var addMethod = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Array.Empty<Type>(); } })
                    .Where(t => t.Name == "ModuleExtensions" && t.Namespace == "Elsa.Extensions")
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    .FirstOrDefault(m => m.Name == "AddWorkflowsFrom" && !m.IsGenericMethod
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType == typeof(Assembly));

                if (addMethod != null)
                {
                    addMethod.Invoke(null, new object[] { elsaFeature, asm });
                    logger.Info($"{assemblyName}: programmatic workflows registered.");
                }
                else
                {
                    logger.Warn($"AddWorkflowsFrom(Assembly) method not found — workflows from {assemblyName} not registered.");
                }
            }
            catch (FileNotFoundException)
            {
                logger.Warn($"{assemblyName} assembly not found — workflows will not be available.");
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to register workflows from {assemblyName}: {ex.Message}");
            }
        }
    }
}
