using System;
using System.Reflection;
using Autofac;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Framework.Logging;

namespace ACS.Elsa.Activities
{
    /// <summary>
    /// Base class for generated service method Activities.
    /// Each subclass specifies ServiceTypeName/MethodName as constants,
    /// and accepts a message argument via Input.
    /// Resolves services from Autofac at runtime using reflection.
    /// </summary>
    public abstract class ReflectionActivityBase : CodeActivity<bool>
    {
        protected static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        /// <summary>Assembly-qualified service type name.</summary>
        protected abstract string ServiceTypeAssemblyName { get; }

        /// <summary>Method to invoke.</summary>
        protected abstract string ServiceMethodName { get; }

        /// <summary>The message argument (VehicleMessageEx, TransferMessageEx, etc.)</summary>
        [Input(Description = "Message argument (VehicleMessageEx, TransferMessageEx, XmlDocument, etc.)")]
        public Input<object> Message { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var message = Message?.Get(context);

            try
            {
                var serviceType = System.Type.GetType(ServiceTypeAssemblyName);
                if (serviceType == null)
                {
                    logger.Error($"{GetType().Name}: Type '{ServiceTypeAssemblyName}' not found.");
                    context.Set(Result, false);
                    return;
                }

                var scope = context.GetService<ILifetimeScope>();
                var service = scope?.Resolve(serviceType);
                if (service == null)
                {
                    logger.Error($"{GetType().Name}: Cannot resolve '{serviceType.Name}'.");
                    context.Set(Result, false);
                    return;
                }

                var method = serviceType.GetMethod(ServiceMethodName,
                    BindingFlags.Instance | BindingFlags.Public);
                if (method == null)
                {
                    logger.Error($"{GetType().Name}: Method '{ServiceMethodName}' not found.");
                    context.Set(Result, false);
                    return;
                }

                object result;
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                    result = method.Invoke(service, null);
                else
                    result = method.Invoke(service, new[] { message });

                bool success;
                if (method.ReturnType == typeof(bool))
                    success = (bool)result;
                else if (method.ReturnType == typeof(int))
                    success = (int)result > 0;
                else
                    success = true;

                context.Set(Result, success);
            }
            catch (TargetInvocationException tie)
            {
                logger.Error($"{GetType().Name}: {tie.InnerException?.Message}", tie.InnerException);
                context.Set(Result, false);
            }
            catch (Exception ex)
            {
                logger.Error($"{GetType().Name}: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }
}
