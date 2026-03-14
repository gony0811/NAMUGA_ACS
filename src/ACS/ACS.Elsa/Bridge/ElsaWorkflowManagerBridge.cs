using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.Json;
using Autofac;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using ACS.Framework.Logging;
using ACS.Workflow;

namespace ACS.Elsa.Bridge
{
    /// <summary>
    /// Hybrid IWorkflowManager: routes commands to Elsa or legacy WorkflowManagerImpl
    /// based on elsa-migration.json configuration.
    ///
    /// Commands listed in "ElsaCommands" are dispatched to Elsa workflow engine.
    /// All other commands fall through to the original WorkflowManagerImpl.
    /// This enables gradual migration without breaking existing functionality.
    /// </summary>
    public class ElsaWorkflowManagerBridge : IWorkflowManager
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ElsaWorkflowManagerBridge));

        private readonly WorkflowManagerImpl _legacyManager;
        private readonly IWorkflowDispatcher _workflowDispatcher;
        private HashSet<string> _elsaCommands;
        private readonly string _configPath;

        public ElsaWorkflowManagerBridge(
            WorkflowManagerImpl legacyManager,
            IWorkflowDispatcher workflowDispatcher)
        {
            _legacyManager = legacyManager;
            _workflowDispatcher = workflowDispatcher;

            // Config file next to the running executable
            _configPath = Path.Combine(AppContext.BaseDirectory, "elsa-migration.json");
            LoadConfig();
        }

        private void LoadConfig()
        {
            _elsaCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(_configPath))
            {
                logger.Info($"elsa-migration.json not found at {_configPath} — all commands routed to legacy.");
                return;
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ElsaCommands", out var cmds) && cmds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in cmds.EnumerateArray())
                    {
                        var cmd = item.GetString();
                        if (!string.IsNullOrEmpty(cmd))
                            _elsaCommands.Add(cmd);
                    }
                }

                logger.Info($"Elsa migration config loaded: {_elsaCommands.Count} command(s) routed to Elsa.");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load elsa-migration.json: {ex.Message}", ex);
            }
        }

        private bool IsElsaCommand(string workflowName)
        {
            return _elsaCommands.Contains(workflowName);
        }

        private bool DispatchToElsa(string workflowName, object[] args)
        {
            try
            {
                var input = new Dictionary<string, object>
                {
                    ["CommandName"] = workflowName,
                    ["Arguments"] = args
                };

                var request = new DispatchWorkflowDefinitionRequest(workflowName)
                {
                    Input = input
                };

                var result = _workflowDispatcher.DispatchAsync(request)
                    .GetAwaiter().GetResult();

                logger.Info($"Elsa dispatch for '{workflowName}': success");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Elsa dispatch for '{workflowName}' failed: {ex.Message}", ex);
                return false;
            }
        }

        // --- IWorkflowManager implementation ---

        public bool Execute(string workflowName, object paramObject)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new[] { paramObject });
            return _legacyManager.Execute(workflowName, paramObject);
        }

        public bool Execute(string workflowName, object paramObject, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new[] { paramObject });
            return _legacyManager.Execute(workflowName, paramObject, isParallel);
        }

        public bool Execute(string workflowName, XmlDocument document)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new object[] { document });
            return _legacyManager.Execute(workflowName, document);
        }

        public bool Execute(string workflowName, XmlDocument document, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new object[] { document });
            return _legacyManager.Execute(workflowName, document, isParallel);
        }

        public bool Execute(string workflowName, object[] args)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, args);
            return _legacyManager.Execute(workflowName, args);
        }

        public bool Execute(string workflowName, object[] args, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, args);
            return _legacyManager.Execute(workflowName, args, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, XmlDocument document)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new object[] { document });
            return _legacyManager.Execute(transactionId, workflowName, document);
        }

        public bool Execute(string transactionId, string workflowName, XmlDocument document, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new object[] { document });
            return _legacyManager.Execute(transactionId, workflowName, document, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, object paramObject)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new[] { paramObject });
            return _legacyManager.Execute(transactionId, workflowName, paramObject);
        }

        public bool Execute(string transactionId, string workflowName, object paramObject, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, new[] { paramObject });
            return _legacyManager.Execute(transactionId, workflowName, paramObject, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, object[] args, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return DispatchToElsa(workflowName, args);
            return _legacyManager.Execute(transactionId, workflowName, args, isParallel);
        }

        public void Reload()
        {
            _legacyManager.Reload();
            LoadConfig(); // also reload Elsa migration config
        }

        public bool SkipWorkflow(string workflowName)
        {
            return _legacyManager.SkipWorkflow(workflowName);
        }

        public void Start()
        {
            _legacyManager.Start();
        }

        public void Stop()
        {
            _legacyManager.Stop();
        }
    }
}
