using System;
using System.ComponentModel;
using System.Reflection;
using Autofac;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using ACS.Core.Logging;

namespace ACS.Elsa.Activities
{
    /// <summary>
    /// Generic Elsa Activity that invokes any ACS service method via reflection.
    ///
    /// This avoids circular project references (ACS.Elsa cannot reference ACS.Service directly).
    /// Service types are resolved at runtime from the Autofac container.
    ///
    /// Usage in Elsa Studio:
    ///   ServiceTypeName  = "ACS.Service.ResourceServiceEx, ACS.Service"
    ///   MethodName       = "SearchSuitableRechargeStation"
    ///   Argument         = (workflow variable — the VehicleMessageEx object)
    /// </summary>
    [Activity(
        "ACS",
        "ACS Service Method",
        "Calls an ACS service method by name. Resolves the service from DI and invokes via reflection.")]
    public class ServiceMethodActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ServiceMethodActivity));

        /// <summary>
        /// Assembly-qualified type name of the service.
        /// Example: "ACS.Service.ResourceServiceEx, ACS.Service"
        /// </summary>
        [Input(
            Description = "Assembly-qualified service type name (e.g. 'ACS.Service.ResourceServiceEx, ACS.Service')",
            UIHint = InputUIHints.SingleLine)]
        public Input<string> ServiceTypeName { get; set; }

        /// <summary>
        /// Name of the method to invoke on the service.
        /// Example: "SearchSuitableRechargeStation"
        /// </summary>
        [Input(
            Description = "Method name to invoke (e.g. 'SearchSuitableRechargeStation')",
            UIHint = InputUIHints.SingleLine)]
        public Input<string> MethodName { get; set; }

        /// <summary>
        /// The argument to pass to the service method (typically a message object).
        /// </summary>
        [Input(
            Description = "Argument to pass to the method (typically VehicleMessageEx or XmlDocument)")]
        public Input<object> Argument { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var serviceTypeName = ServiceTypeName.Get(context);
            var methodName = MethodName.Get(context);
            var argument = Argument.Get(context);

            try
            {
                // 1. Resolve service type by name
                var serviceType = System.Type.GetType(serviceTypeName);
                if (serviceType == null)
                {
                    logger.Error($"ServiceMethodActivity: Type '{serviceTypeName}' not found.");
                    context.Set(Result, false);
                    return;
                }

                // 2. Resolve service instance from Autofac
                var scope = context.GetService<ILifetimeScope>();
                object service = null;
                if (scope != null)
                    service = scope.Resolve(serviceType);

                if (service == null)
                {
                    logger.Error($"ServiceMethodActivity: Cannot resolve '{serviceType.Name}' from DI.");
                    context.Set(Result, false);
                    return;
                }

                // 3. Find and invoke method
                var method = serviceType.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (method == null)
                {
                    logger.Error($"ServiceMethodActivity: Method '{methodName}' not found on '{serviceType.Name}'.");
                    context.Set(Result, false);
                    return;
                }

                object result;
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                    result = method.Invoke(service, null);
                else
                    result = method.Invoke(service, new[] { argument });

                // 4. Interpret result
                bool success;
                if (method.ReturnType == typeof(bool))
                    success = (bool)result;
                else if (method.ReturnType == typeof(int))
                    success = (int)result > 0;
                else
                    success = true; // void methods → always success

                context.Set(Result, success);
                logger.Info($"ServiceMethodActivity: {serviceType.Name}.{methodName} → {success}");
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                logger.Error($"ServiceMethodActivity: {serviceTypeName}.{methodName} failed: {inner.Message}", inner);
                context.Set(Result, false);
            }
            catch (Exception ex)
            {
                logger.Error($"ServiceMethodActivity: {serviceTypeName}.{methodName} failed: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }
}
