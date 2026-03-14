using System;
using System.Reflection;
using Autofac;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using ACS.Framework.Logging;

namespace ACS.Elsa.Activities
{
    /// <summary>
    /// Elsa Activity that wraps an existing BaseBizJob handler.
    /// Uses reflection to invoke IBizJob.ExecuteJob() to avoid
    /// circular dependency on ACS.Biz.
    /// </summary>
    [Activity(
        "ACS",
        "ACS BizJob",
        "Executes a legacy BizJob command by name. Resolves the job from BizFileRepository.")]
    public class BizJobActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(BizJobActivity));

        /// <summary>The ACS command name (e.g. "MOVECMD", "RAIL-VEHICLESTART").</summary>
        [Input(
            Description = "ACS command name (e.g. 'MOVECMD', 'RAIL-VEHICLESTART')",
            UIHint = InputUIHints.SingleLine)]
        public Input<string> CommandName { get; set; }

        /// <summary>Arguments passed to BaseBizJob.ExecuteJob().</summary>
        [Input(
            Description = "Arguments array passed to BaseBizJob.ExecuteJob()")]
        public Input<object[]> Arguments { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var commandName = CommandName.Get(context) ?? string.Empty;
            var args = Arguments.Get(context) ?? Array.Empty<object>();

            try
            {
                var bizJob = ResolveBizJob(context, commandName);
                if (bizJob == null)
                {
                    logger.Error($"BizJobActivity: Cannot resolve job for command '{commandName}'");
                    context.Set(Result, false);
                    return;
                }

                // Set LifetimeScope via reflection (IBizJob.LifetimeScope)
                var scope = context.GetService<ILifetimeScope>();
                var scopeProp = bizJob.GetType().GetProperty("LifetimeScope");
                if (scopeProp != null && scope != null)
                    scopeProp.SetValue(bizJob, scope);

                // Invoke Execute(object[] args) via reflection
                var executeMethod = bizJob.GetType().GetMethod("Execute", new[] { typeof(object[]) });
                if (executeMethod == null)
                {
                    logger.Error($"BizJobActivity: Execute method not found on {bizJob.GetType().Name}");
                    context.Set(Result, false);
                    return;
                }

                var result = (int)executeMethod.Invoke(bizJob, new object[] { args });
                bool success = result == 0; // 0 = SUCCESS in BaseBizJob convention

                context.Set(Result, success);
                logger.Info($"BizJobActivity: {commandName} completed with result={result} (success={success})");
            }
            catch (Exception ex)
            {
                logger.Error($"BizJobActivity: {commandName} failed: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }

        private object ResolveBizJob(ActivityExecutionContext context, string commandName)
        {
            // Resolve from BizFileRepository via command name
            var bizFileRepo = context.GetService<ACS.Workflow.BizFileRepository>();
            if (bizFileRepo != null && bizFileRepo.InstanceList.ContainsKey(commandName))
            {
                var tuple = bizFileRepo.InstanceList[commandName];
                return tuple.Item2; // The instantiated job object
            }

            return null;
        }
    }
}
